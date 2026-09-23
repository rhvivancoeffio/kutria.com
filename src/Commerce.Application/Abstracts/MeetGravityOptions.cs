namespace Commerce.Application.Abstracts;

public sealed class MeetGravityOptions
{
    public const string SectionName = "MeetGravity";

    public string BaseUrl { get; set; } =
        "https://gravity-sandbox-api-csa3exb8djeteucm.eastus-01.azurewebsites.net";

    public int PlatformType { get; set; } = 1;

    public string DefaultLanguage { get; set; } = "es-PE";

    public string DefaultTimeZone { get; set; } = "SA Pacific Standard Time";

    public string DefaultPhoneNumber { get; set; } = "+51000000000";

    /// <summary>Provider key (VTEX, Shopify, …) → MeetGravity providerId GUID.</summary>
    public Dictionary<string, string> ProviderIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
