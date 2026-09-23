using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Integrations;

public sealed class MeetGravityHeadlessClient(
    IHttpClientFactory httpClientFactory,
    IOptions<MeetGravityOptions> options,
    ILogger<MeetGravityHeadlessClient> logger) : IMeetGravityHeadlessClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task RegisterAsync(MeetGravityRegisterRequest request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var body = new
        {
            firstName = request.FirstName,
            lastName = request.LastName,
            email = request.Email,
            password = request.Password,
            confirmPassword = request.ConfirmPassword,
            name = request.Name,
            host = request.Host,
            platformType = request.PlatformType,
            phoneNumber = request.PhoneNumber,
            providerId = request.ProviderId,
            language = request.Language,
            timeZone = request.TimeZone
        };

        using var response = await client.PostAsJsonAsync("Users/registers", body, JsonOptions, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning(
            "MeetGravity register failed ({Status}): {Body}",
            (int)response.StatusCode,
            Truncate(error));

        // Caller may treat conflict / bad request as "already registered" and fall back to login.
        throw new MeetGravityApiException(
            $"MeetGravity register failed ({(int)response.StatusCode}).",
            (int)response.StatusCode,
            error);
    }

    public async Task<MeetGravityLoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync(
            "users/login",
            new { email, password },
            JsonOptions,
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "MeetGravity login failed ({Status}): {Body}",
                (int)response.StatusCode,
                Truncate(payload));
            throw new MeetGravityApiException(
                $"MeetGravity login failed ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                payload);
        }

        var login = JsonSerializer.Deserialize<LoginDto>(payload, JsonOptions)
            ?? throw new MeetGravityApiException("MeetGravity login returned empty body.", (int)response.StatusCode, payload);

        if (string.IsNullOrWhiteSpace(login.AccessToken))
            throw new MeetGravityApiException("MeetGravity login did not return access_token.", (int)response.StatusCode, payload);

        return new MeetGravityLoginResult(login.AccessToken, login.ExpiresIn, login.TokenType);
    }

    public async Task<IReadOnlyList<MeetGravityOrganization>> ListOrganizationsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "Organizations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "MeetGravity Organizations failed ({Status}): {Body}",
                (int)response.StatusCode,
                Truncate(payload));
            throw new MeetGravityApiException(
                $"MeetGravity Organizations failed ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                payload);
        }

        var list = JsonSerializer.Deserialize<List<OrganizationDto>>(payload, JsonOptions) ?? [];
        return list
            .Where(o => o.OrganizationId != Guid.Empty)
            .Select(o => new MeetGravityOrganization(o.OrganizationId, o.Name, o.ProviderId, o.ProviderName))
            .ToList();
    }

    public async Task<MeetGravityClientAppCredentials> CreateOrganizationClientAppAsync(
        string accessToken,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "ClientApps/organizations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("x-Org-Id", organizationId.ToString());
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { organizationId }, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "MeetGravity ClientApps/organizations failed ({Status}): {Body}",
                (int)response.StatusCode,
                Truncate(payload));
            throw new MeetGravityApiException(
                $"MeetGravity ClientApps/organizations failed ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                payload);
        }

        var creds = JsonSerializer.Deserialize<ClientAppDto>(payload, JsonOptions)
            ?? throw new MeetGravityApiException("MeetGravity ClientApps returned empty body.", (int)response.StatusCode, payload);

        if (string.IsNullOrWhiteSpace(creds.ClientId) || string.IsNullOrWhiteSpace(creds.ClientSecret))
            throw new MeetGravityApiException(
                "MeetGravity ClientApps did not return clientId/clientSecret.",
                (int)response.StatusCode,
                payload);

        return new MeetGravityClientAppCredentials(creds.ClientId, creds.ClientSecret);
    }

    public async Task<IReadOnlyList<MeetGravityAvailableProvider>> ListAvailableProvidersAsync(
        string accessToken,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "Providers/availables");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("x-Org-Id", organizationId.ToString());

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "MeetGravity Providers/availables failed ({Status}): {Body}",
                (int)response.StatusCode,
                Truncate(payload));
            throw new MeetGravityApiException(
                $"MeetGravity Providers/availables failed ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                payload);
        }

        var list = JsonSerializer.Deserialize<List<AvailableProviderDto>>(payload, JsonOptions) ?? [];
        return list
            .Where(p => p.ProviderId != Guid.Empty)
            .Select(p => new MeetGravityAvailableProvider(
                p.ProviderId,
                p.ProviderTenantId,
                p.Name,
                (p.ProviderSettings ?? [])
                    .Select(s => new MeetGravityProviderSetting(
                        s.ProviderSettingId,
                        s.Key ?? "",
                        s.Value,
                        s.Required,
                        s.Label))
                    .ToList()))
            .ToList();
    }

    public async Task UpdateProviderSettingsAsync(
        string accessToken,
        Guid organizationId,
        Guid providerId,
        MeetGravityUpdateProviderSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"Providers/{providerId}/settings");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.TryAddWithoutValidation("x-Org-Id", organizationId.ToString());
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    providerId = request.ProviderId,
                    providerTenantId = request.ProviderTenantId,
                    variables = request.Variables.Select(v => new
                    {
                        providerSettingId = v.ProviderSettingId,
                        key = v.Key,
                        value = v.Value
                    })
                },
                JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "MeetGravity Providers settings PUT failed ({Status}): {Body}",
                (int)response.StatusCode,
                Truncate(payload));
            throw new MeetGravityApiException(
                $"MeetGravity Providers settings PUT failed ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                payload);
        }
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("MeetGravity");
        var baseUrl = options.Value.BaseUrl?.Trim().TrimEnd('/')
            ?? throw new InvalidOperationException("MeetGravity:BaseUrl is required.");
        client.BaseAddress ??= new Uri(baseUrl + "/");
        return client;
    }

    private static string Truncate(string? value, int max = 500)
        => string.IsNullOrEmpty(value) ? "" : value.Length <= max ? value : value[..max] + "…";

    private sealed class LoginDto
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public string? ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private sealed class OrganizationDto
    {
        public Guid OrganizationId { get; set; }
        public string? Name { get; set; }
        public Guid? ProviderId { get; set; }
        public string? ProviderName { get; set; }
    }

    private sealed class ClientAppDto
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }

    private sealed class AvailableProviderDto
    {
        public Guid ProviderId { get; set; }
        public Guid ProviderTenantId { get; set; }
        public string? Name { get; set; }
        public List<ProviderSettingDto>? ProviderSettings { get; set; }
    }

    private sealed class ProviderSettingDto
    {
        public Guid ProviderSettingId { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
        public bool Required { get; set; }
        public string? Label { get; set; }
    }
}

public sealed class MeetGravityApiException(string message, int statusCode, string? responseBody)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
}
