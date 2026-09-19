using System.Net.Http.Json;

namespace SecretSanta.Web.Localization;

public static class ApiErrorReader
{
    public static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
            return string.IsNullOrWhiteSpace(problem?.Code) ? StatusCode(response.StatusCode) : problem.Code;
        }
        catch (Exception) when (response.Content is not null)
        {
            return StatusCode(response.StatusCode);
        }
    }

    private static string StatusCode(System.Net.HttpStatusCode statusCode) => statusCode switch
    {
        System.Net.HttpStatusCode.Unauthorized => "unauthorized",
        System.Net.HttpStatusCode.Forbidden => "forbidden",
        _ => "error_unknown"
    };

    private sealed class ApiProblem
    {
        public string? Code { get; set; }
    }
}
