using Microsoft.Extensions.Localization;

namespace SecretSanta.Web.Localization;

public static class UiText
{
    public static string ParticipantCount(IStringLocalizer<SharedResource> localizer, int count)
    {
        var key = count % 10 == 1 && count % 100 != 11
            ? "ParticipantCountOne"
            : count % 10 is >= 2 and <= 4 && count % 100 is not (>= 12 and <= 14)
                ? "ParticipantCountFew"
                : "ParticipantCountMany";
        return localizer[key, count];
    }
}
