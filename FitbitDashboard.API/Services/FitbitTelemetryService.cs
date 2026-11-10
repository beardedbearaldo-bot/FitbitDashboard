using FitbitDashboard.Shared;
using Microsoft.EntityFrameworkCore;

namespace FitbitDashboard.API.Services;

public class FitbitTelemetryService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;

    public FitbitTelemetryService(AppDbContext db, HttpClient httpClient)
    {
        _db = db;
        _httpClient = httpClient;
    }

    public async Task<HeartRateAnalyticsResult?> GetIntradayHeartRateAsync(string date)
    {
        // 1. Fetch the active token credentials from SQLite
        var token = await _db.FitbitTokens.FirstOrDefaultAsync(t => t.Id == "local-user");
        if (token == null) return null;

        // 2. Setup the authorization headers safely
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        // 3. Request detail-level heart rate logs for a specific day from Fitbit
        var url = $"https://fitbit.com{date}/1d/1min.json";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadFromJsonAsync<FitbitHeartRateResponse>();
        var points = payload?.ActivitiesHeartIntraday?.Dataset;

        if (points == null || points.Count == 0) return null;

        // 4. Run calculations using your OriGym-based programming guidelines
        int maxHr = points.Max(p => p.Value);
        int avgHr = (int)points.Average(p => p.Value);

        // Define simple workout intensity zones based on standard maximum heart rate estimates
        // Zone 2 (Aerobic/Fat Max): 60-70% | Zone 3 (Tempo): 70-80% | Zone 4+ (Anaerobic Threshold): >80%
        int zone2Min = (int)(maxHr * 0.60);
        int zone3Min = (int)(maxHr * 0.70);
        int zone4Min = (int)(maxHr * 0.80);

        int z1Count = 0, z2Count = 0, z3Count = 0, z4Count = 0;

        foreach (var point in points)
        {
            if (point.Value >= zone4Min) z4Count++;
            else if (point.Value >= zone3Min) z3Count++;
            else if (point.Value >= zone2Min) z2Count++;
            else z1Count++;
        }

        var zones = new Dictionary<string, int>
        {
            { "Zone 1 (Recovery)", z1Count },
            { "Zone 2 (Aerobic Base)", z2Count },
            { "Zone 3 (Tempo / Lactate)", z3Count },
            { "Zone 4+ (Anaerobic / Peak)", z4Count }
        };

        return new HeartRateAnalyticsResult(points.Count, avgHr, maxHr, zones);
    }

    public async Task<AthleteRecoveryScore?> GetSleepRecoveryScoreAsync(string date)
    {
        // 1. Fetch credentials from SQLite database layer
        var token = await _db.FitbitTokens.FirstOrDefaultAsync(t => t.Id == "local-user");
        if (token == null) return null;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        // 2. Fetch v1.2 Sleep data from Fitbit
        var url = $"https://fitbit.com{date}.json";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadFromJsonAsync<FitbitSleepResponse>();
        var mainSleepLog = payload?.Sleep?.FirstOrDefault(s => s.Efficiency > 0); // Isolate the primary sleep cycle

        if (mainSleepLog == null) return null;

        // 3. Extract core tracking metrics
        int efficiency = mainSleepLog.Efficiency;
        int totalAsleep = mainSleepLog.MinutesAsleep;
        int deepMinutes = mainSleepLog.Levels?.Summary?.Deep?.Minutes ?? 0;
        int remMinutes = mainSleepLog.Levels?.Summary?.Rem?.Minutes ?? 0;

        // 4. Calculate Athlete Recovery Programming Metric
        string recommendation;
        string color;

        // Sports Science Heuristic: Evaluate nervous system and tissue structural regeneration status
        if (efficiency >= 90 && deepMinutes >= 60)
        {
            recommendation = "Optimal neurological and systemic recovery. High-intensity CNS training/Max overloading permitted today.";
            color = "Green";
        }
        else if (efficiency >= 80 || (deepMinutes + remMinutes) > 90)
        {
            recommendation = "Moderate baseline recovery. Proceed with standard scheduled program volume; closely monitor progressive fatigue.";
            color = "Amber";
        }
        else
        {
            recommendation = "Impaired regenerative index. Limit heavy mechanical loading; prioritize dynamic mobility or active recovery session.";
            color = "Red";
        }

        return new AthleteRecoveryScore(efficiency, totalAsleep, deepMinutes, remMinutes, recommendation, color);
    }
}

