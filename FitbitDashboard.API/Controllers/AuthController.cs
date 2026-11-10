using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;

namespace MySportsTechApp.Api.Controllers;

[ApiController]
[Route("[controller]")] // Resolves to api/auth due to your custom convention
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _clientFactory;

    public AuthController(IConfiguration config, IHttpClientFactory clientFactory)
    {
        _config = config;
        _clientFactory = clientFactory;
    }

    // 1. Blazor will call this to get the login URL
    [HttpGet("fitbit-login-url")]
    public IActionResult GetFitbitLoginUrl()
    {
        var clientId = _config["Fitbit:ClientId"];
        var redirectUri = Uri.EscapeDataString(_config["Fitbit:RedirectUri"]!);

        // Requesting access to activity, heart rate, and sleep data
        var scope = "activity%20heartrate%20sleep";

        var url = $"https://fitbit.com{clientId}&redirect_uri={redirectUri}&scope={scope}&expires_in=604800";

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

        // TODO: In production, store access_token & refresh_token securely in a Database!
        // For local development testing, we will just return them or redirect back to Blazor.

        // Redirect back to your Blazor WASM frontend home page
        return Redirect("https://localhost:7170/dashboard");
    }
}

