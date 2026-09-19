using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SecretSanta.Web.Models;
using SecretSanta.Web.Localization;

namespace SecretSanta.Web.Components.Pages;

public partial class MyGames
{
    [Inject]
    private IHttpClientFactory ClientFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "inviteToken")]
    public string? InviteToken { get; set; }

    private GameListDto? selectedGame;
    private bool isCreator;
    private List<UserGiftDto> myGifts = [];
    private ReceiverGiftDto assgGifts = new();
    private string newGiftName = string.Empty;
    private string newDeliveryMethod = string.Empty;
    private bool loading = true;
    private bool isLoggedIn;
    private int userId;
    private string token = string.Empty;
    private List<GameListDto> games = [];
    private bool showCreateGameModal;
    private string newGameName = string.Empty;
    private int giftCost = 1000;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var userIdValue = string.Empty;
        try
        {
            token = await JS.InvokeAsync<string>("localStorage.getItem", "authToken");
            userIdValue = await JS.InvokeAsync<string>("localStorage.getItem", "userId");
        }
        catch (JSException)
        {
            // The page remains in the signed-out state when browser storage is unavailable.
        }

        isLoggedIn = !string.IsNullOrEmpty(token) && int.TryParse(userIdValue, out userId);

        if (!string.IsNullOrEmpty(InviteToken))
        {
            if (isLoggedIn)
            {
                await JoinGameByInviteTokenAsync(InviteToken);
            }
            else
            {
                await ShowAlertAsync(Localizer["JoinRequiresSignIn"]);
            }
        }

        if (isLoggedIn)
        {
            await LoadGamesAsync();
        }

        loading = false;
        StateHasChanged();
    }

    private HttpClient CreateAuthorizedClient()
    {
        var client = ClientFactory.CreateClient("api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task LoadMyGiftsAsync(int gameId)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var gifts = await CreateAuthorizedClient()
            .GetFromJsonAsync<List<UserGiftDto>>($"api/Gifts/{gameId}/gifts");
        myGifts = gifts ?? [];
    }

    private async Task AddGiftAsync()
    {
        if (selectedGame is null ||
            string.IsNullOrWhiteSpace(newGiftName) ||
            string.IsNullOrWhiteSpace(newDeliveryMethod))
        {
            return;
        }

        var gift = new UserGiftDto
        {
            GiftName = newGiftName,
            DeliveryMethod = newDeliveryMethod
        };
        var response = await CreateAuthorizedClient()
            .PostAsJsonAsync($"api/Gifts/{selectedGame.GameId}/gifts", gift);

        if (!response.IsSuccessStatusCode)
        {
            await ShowResponseErrorAsync(response);
            return;
        }

        newGiftName = string.Empty;
        newDeliveryMethod = string.Empty;
        await LoadMyGiftsAsync(selectedGame.GameId);
    }

    private async Task DeleteGiftAsync(int giftId)
    {
        if (selectedGame is null)
        {
            return;
        }

        var response = await CreateAuthorizedClient()
            .DeleteAsync($"api/gifts/{selectedGame.GameId}/gifts/{giftId}");

        if (!response.IsSuccessStatusCode)
        {
            await ShowResponseErrorAsync(response);
            return;
        }

        myGifts.RemoveAll(gift => gift.UserGiftId == giftId);
    }

    private async Task LoadGamesAsync()
    {
        try
        {
            games = await CreateAuthorizedClient()
                .GetFromJsonAsync<List<GameListDto>>("api/games/user") ?? [];
        }
        catch (HttpRequestException)
        {
            games = [];
        }
    }

    private void ToggleCreateGameModal() => showCreateGameModal = !showCreateGameModal;

    private async Task CreateGameAsync()
    {
        if (string.IsNullOrWhiteSpace(newGameName))
        {
            return;
        }

        var request = new CreateGameRequest { Name = newGameName, GiftCost = giftCost };
        var response = await CreateAuthorizedClient().PostAsJsonAsync("api/games", request);

        if (!response.IsSuccessStatusCode)
        {
            await ShowResponseErrorAsync(response);
            return;
        }

        newGameName = string.Empty;
        showCreateGameModal = false;
        await LoadGamesAsync();
    }

    private async Task OpenGameModal(GameListDto game)
    {
        selectedGame = game;
        isCreator = game.CreatorId == userId;
        assgGifts = new ReceiverGiftDto();

        if (game.IsDrawn)
        {
            await LoadReceiverAssignmentAsync(game.GameId);
        }

        await LoadMyGiftsAsync(game.GameId);
    }

    private void CloseGameModal()
    {
        selectedGame = null;
        myGifts = [];
        assgGifts = new ReceiverGiftDto();
    }

    private async Task LoadReceiverAssignmentAsync(int gameId)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        try
        {
            assgGifts = await CreateAuthorizedClient()
                .GetFromJsonAsync<ReceiverGiftDto>($"api/Assignments/giver/{gameId}") ?? new ReceiverGiftDto();
        }
        catch (HttpRequestException)
        {
            assgGifts = new ReceiverGiftDto();
        }
    }

    private async Task StartDraw()
    {
        if (selectedGame is null)
        {
            return;
        }

        var response = await CreateAuthorizedClient()
            .PostAsync($"api/Assignments/draw/{selectedGame.GameId}", null);

        if (!response.IsSuccessStatusCode)
        {
            await ShowResponseErrorAsync(response);
            return;
        }

        selectedGame.IsDrawn = true;
        await LoadGamesAsync();
        await LoadReceiverAssignmentAsync(selectedGame.GameId);
    }

    private async Task InviteParticipants()
    {
        if (selectedGame is null)
        {
            return;
        }

        var response = await CreateAuthorizedClient()
            .PostAsync($"api/games/{selectedGame.GameId}/invite", null);
        if (!response.IsSuccessStatusCode)
        {
            await ShowResponseErrorAsync(response);
            return;
        }

        var result = await response.Content.ReadFromJsonAsync<InviteResult>();
        if (result is not null)
        {
            var inviteLink = NavigationManager.GetUriWithQueryParameter("inviteToken", result.InviteToken);
            await ShowAlertAsync(Localizer["InvitationLink", inviteLink]);
        }
    }

    private async Task JoinGameByInviteTokenAsync(string inviteToken)
    {
        try
        {
            var response = await CreateAuthorizedClient().PostAsync($"api/games/join/{inviteToken}", null);
            if (!response.IsSuccessStatusCode)
            {
                await ShowResponseErrorAsync(response);
            }
        }
        catch (HttpRequestException)
        {
            await ShowAlertAsync($"{Localizer["JoinGameError"]} {Localizer["ConnectionError"]}");
        }
    }

    private async Task ShowResponseErrorAsync(HttpResponseMessage response)
    {
        var code = await ApiErrorReader.ReadCodeAsync(response);
        await ShowAlertAsync(Localizer[code]);
    }

    private async Task ShowAlertAsync(string message)
    {
        try
        {
            await JS.InvokeVoidAsync("alert", message);
        }
        catch (JSException)
        {
            // An alert is best-effort and can fail after the circuit disconnects.
        }
    }
}
