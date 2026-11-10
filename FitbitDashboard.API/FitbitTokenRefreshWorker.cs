using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FitbitDashboard.API;

public class FitbitTokenRefreshWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<FitbitTokenRefreshWorker> _logger;

    // Check token expiration status every 5 minutes
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public FitbitTokenRefreshWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory clientFactory,
        IConfiguration config,
        ILogger<FitbitTokenRefreshWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _clientFactory = clientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fitbit Token Refresh Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshTokensIfExpiredAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the token refresh check.");
            }

            // Wait 5 minutes before checking again
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task RefreshTokensIfExpiredAsync()
    {
        // 1. Create a temporary database scope
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 2. Fetch our local user token parameters
        var tokenRecord = await db.FitbitTokens.FirstOrDefaultAsync(t => t.Id == "local-user");
        if (tokenRecord == null) return;

        // 3. Check if token expires within the next 10 minutes (buffer time)
        if (DateTime.UtcNow.AddMinutes(10) < tokenRecord.ExpiresAt)
        {
            // Token is still perfectly valid, skip refreshing
            return;
        }

        _logger.LogInformation("Fitbit token is nearing expiration. Initiating background refresh handshake...");

        // 4. Set up the secure Basic Auth client header strings
        var client = _clientFactory.CreateClient();
        var clientId = _config["Fitbit:ClientId"];
        var clientSecret = _config["Fitbit:ClientSecret"];
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://fitbit.com");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // 5. Structure the payload parameters requested specifically for a token refresh exchange
        var formData = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", tokenRecord.RefreshToken }
        };
        request.Content = new FormUrlEncodedContent(formData);

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Fitbit API rejected refresh request: {err}");
            return;
        }

        // 6. Extract the updated tokens from the server response
        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        var root = jsonDoc.RootElement;

        tokenRecord.AccessToken = root.GetProperty("access_token").GetString()!;
        tokenRecord.RefreshToken = root.GetProperty("refresh_token").GetString()!;
        var expiresInSeconds = root.GetProperty("expires_in").GetInt32();
        tokenRecord.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);

        // 7. Save changes back to SQLite
        await db.SaveChangesAsync();
        _logger.LogInformation("Fitbit access token successfully refreshed and stored.");
    }
}

