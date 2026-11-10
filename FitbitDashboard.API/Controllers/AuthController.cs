using FitbitDesktop.API;
using Microsoft.AspNetCore.Mvc;
using MySportsTechApp.Shared;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MySportsTechApp.Api.Controllers;

[ApiController]
[Route("[controller]")] // Resolves to api/auth due to your custom convention
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _clientFactory;
    private readonly AppDbContext _db;

    public AuthController(IConfiguration config, IHttpClientFactory clientFactory, AppDbContext db)
    {
        _config = config;
        _clientFactory = clientFactory;
        _db = db;
    }

    // 1. Blazor will call this to get the login URL
    [HttpGet("fitbit-login-url")]
    public IActionResult GetFitbitLoginUrl()
    {
        var clientId = _config["Fitbit:ClientId"];
        var redirectUri = Uri.EscapeDataString(_config["Fitbit:RedirectUri"]!);

        // Requesting access to activity, heart rate, and sleep data
        var scope = "activity%20heartrate%20sleep";

        var url = $"https://fitbit.com/oath2/authorize?&client_id={clientId}&redirect_uri={redirectUri}&scope={scope}&expires_in=604800";

        return Ok(new { Url = url });
    }

    // 2. Fitbit redirects here with the temporary ?code=XYZ parameter
    [HttpGet("fitbit-callback")]
    public async Task<IActionResult> FitbitCallback([FromQuery] string code)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest("Authorization code missing.");

        var client = _clientFactory.CreateClient();

        // OAuth 2.0 requires Basic Auth header: Base64(ClientId:ClientSecret)
        var clientId = _config["Fitbit:ClientId"];
        var clientSecret = _config["Fitbit:ClientSecret"];
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://fitbit.com");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Form URL encoded body payload parameters required by Fitbit
        var formData = new Dictionary<string, string>
        {
            { "client_id", clientId! },
            { "grant_type", "authorization_code" },
            { "redirect_uri", _config["Fitbit:RedirectUri"]! },
            { "code", code }
        };
        request.Content = new FormUrlEncodedContent(formData);

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorReason = await response.Content.ReadAsStringAsync();
            return BadRequest($"Token exchange failed: {errorReason}");
        }

        var jsonString = await response.Content.ReadAsStringAsync();

        // Parse the token properties out of Fitbit's payload
        using var jsonDoc = JsonDocument.Parse(jsonString);
        var root = jsonDoc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.GetProperty("refresh_token").GetString()!;
        var expiresInSeconds = root.GetProperty("expires_in").GetInt32();

        // Look for our existing local-user profile row
        var existingToken = await _db.FitbitTokens.FindAsync("local-user");

        if (existingToken == null)
        {
            var newToken = new UserFitbitToken
            {
                Id = "local-user",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds)
            };
            _db.FitbitTokens.Add(newToken);
        }
        else
        {
            existingToken.AccessToken = accessToken;
            existingToken.RefreshToken = refreshToken;
            existingToken.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        }

        await _db.SaveChangesAsync();

#if DEBUG
        // Redirect back to your Blazor WASM frontend home page
        return Redirect("https://localhost:7170/dashboard");
#endif
    }
}

