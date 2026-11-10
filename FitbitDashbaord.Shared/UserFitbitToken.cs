namespace MySportsTechApp.Shared;

public class UserFitbitToken
{
    // For local dev, we will use a fixed user ID string (e.g., "local-user")
    public string Id { get; set; } = "local-user";

    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    // Store when the token expires so your C# background service knows when to refresh it
    public DateTime ExpiresAt { get; set; }
}

