using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SecretSanta.ApiService.Controllers;

internal static class ControllerBaseExtensions
{
    public static bool TryGetCurrentUserId(this ControllerBase controller, out int userId)
    {
        var value = controller.User.FindFirstValue("uid");
        return int.TryParse(value, out userId) && userId > 0;
    }
}
