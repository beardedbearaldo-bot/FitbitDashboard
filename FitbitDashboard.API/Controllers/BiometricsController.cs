using FitbitDashboard.API.Services;
using FitbitDashboard.Shared;
using Microsoft.AspNetCore.Mvc;

namespace FitbitDashboard.API.Controllers;

[ApiController]
[Route("[controller]")]
public class BiometricsController : ControllerBase
{
    private readonly FitbitTelemetryService _telemetryService;

    public BiometricsController(FitbitTelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    [HttpGet("heartrate-summary")]
    public async Task<ActionResult<HeartRateAnalyticsResult>> GetHeartRateSummary([FromQuery] string? date)
    {
        // Default to today's date format (YYYY-MM-DD) if none specified
        var targetDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        var result = await _telemetryService.GetIntradayHeartRateAsync(targetDate);
        if (result == null) return NotFound("No biometric data found for this date layout or device is unauthorized.");

        return Ok(result);
    }

    [HttpGet("sleep-recovery")]
    public async Task<ActionResult<AthleteRecoveryScore>> GetSleepRecovery([FromQuery] string? date)
    {
        var targetDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        var result = await _telemetryService.GetSleepRecoveryScoreAsync(targetDate);
        if (result == null) return NotFound("Unable to compile an athlete recovery profile for the specified structural payload.");

        return Ok(result);
    }
}

