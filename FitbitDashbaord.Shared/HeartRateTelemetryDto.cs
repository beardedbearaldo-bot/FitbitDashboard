namespace FitbitDashboard.Shared
{
    public record HeartRateAnalyticsResult(
        int TotalMinutesTracked,
        int AverageHeartRate,
        int MaxHeartRate,
        Dictionary<string, int> ZoneDistributionMinutes
    );

    // Matching Fitbit's native time-series response schema layout
    public class FitbitHeartRateResponse
    {
        public List<HeartRateDatasetItem>? ActivitiesHeart { get; set; }
        public HeartRateIntradayDataset? ActivitiesHeartIntraday { get; set; }
    }

    public class HeartRateDatasetItem
    {
        public string? DateTime { get; set; }
        public object? Value { get; set; } // Can hold general summary data
    }

    public class HeartRateIntradayDataset
    {
        public List<IntradayDataPoint>? Dataset { get; set; }
    }

    public class IntradayDataPoint
    {
        public string Time { get; set; } = string.Empty;
        public int Value { get; set; } // The actual heart rate reading
    }
}
