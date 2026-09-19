using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SecretSanta.Web.Localization;
using SecretSanta.Web.Models;
namespace SecretSanta.Web.Components.Pages;
public partial class MyGames {
 [Inject] private IHttpClientFactory ClientFactory{get;set;}=null!; [Inject] private IJSRuntime JS{get;set;}=null!; [Inject] private NavigationManager NavigationManager{get;set;}=null!;
 [Parameter,SupplyParameterFromQuery(Name="inviteToken")] public string? InviteToken{get;set;} [Parameter,SupplyParameterFromQuery(Name="create")] public bool CreateRequested{get;set;}
 private bool loading=true,isLoggedIn,loadFailed,showCreateGameModal,saving,noticeError; private int userId; private string token="",notice="",formError=""; private List<GameListDto> games=[]; private CreateModel createModel=new();
 protected override async Task OnAfterRenderAsync(bool first){if(!first)return;try{token=await JS.InvokeAsync<string?>("localStorage.getItem","authToken")??"";var id=await JS.InvokeAsync<string?>("localStorage.getItem","userId");isLoggedIn=!string.IsNullOrEmpty(token)&&int.TryParse(id,out userId);}catch{}if(isLoggedIn&&InviteToken is not null)await JoinAsync();if(isLoggedIn)await LoadGamesAsync();if(isLoggedIn&&CreateRequested)showCreateGameModal=true;loading=false;StateHasChanged();}
 private HttpClient Client(){var c=ClientFactory.CreateClient("api");c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);return c;}
 private async Task LoadGamesAsync(){loadFailed=false;try{games=await Client().GetFromJsonAsync<List<GameListDto>>("api/games/user")??[];}catch{loadFailed=true;}StateHasChanged();}
 private async Task JoinAsync(){try{var r=await Client().PostAsync($"api/games/join/{Uri.EscapeDataString(InviteToken!)}",null);if(r.IsSuccessStatusCode){notice=Localizer["JoinedGame"];noticeError=false;}else{notice=Localizer[await ApiErrorReader.ReadCodeAsync(r)];noticeError=true;}}catch{notice=Localizer["ConnectionError"];noticeError=true;}}
 private void OpenCreate(){showCreateGameModal=true;formError="";} private void CloseCreate()=>showCreateGameModal=false; private void HandleDialogKey(KeyboardEventArgs e){if(e.Key=="Escape")CloseCreate();}
 private async Task CreateGameAsync(){if(saving)return;saving=true;formError="";try{var r=await Client().PostAsJsonAsync("api/games",new CreateGameRequest{Name=createModel.Name.Trim(),GiftCost=createModel.GiftCost});if(!r.IsSuccessStatusCode){formError=Localizer[await ApiErrorReader.ReadCodeAsync(r)];return;}var created=await r.Content.ReadFromJsonAsync<GameCreated>();showCreateGameModal=false;if(created is not null)NavigationManager.NavigateTo($"/games/{created.GameId}");else await LoadGamesAsync();}catch{formError=Localizer["ConnectionError"];}finally{saving=false;}}
 private sealed class CreateModel{[Required,MaxLength(200)]public string Name{get;set;}="";[Range(0,int.MaxValue)]public int GiftCost{get;set;}=1000;} private sealed class GameCreated{public int GameId{get;set;}}
}
