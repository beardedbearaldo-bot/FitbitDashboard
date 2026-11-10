using System;
using System.Collections.Generic;
using System.Text;

namespace FitbitDashboard.Shared
{
    public record AthleteRecoveryScore(
        int SleepEfficiencyScore,
        int TotalMinutesAsleep,
        int DeepSleepMinutes,
        int RemSleepMinutes,
        string RecoveryRecommendation,
        string VisualIndicatorColor // e.g., "Green", "Amber", "Red"
    );

    // Matches Fitbit API v1.2 Sleep Log Response Schema
    public class FitbitSleepResponse
    {
        public List<SleepLogItem>? Sleep { get; set; }
    }

    public class SleepLogItem
    {
        public int Duration { get; set; } // Total duration in milliseconds
        public int Efficiency { get; set; } // Out of 100
        public int MinutesAsleep { get; set; }
        public int MinutesAwake { get; set; }
        public SleepLevels? Levels { get; set; }
    }

    public class SleepLevels
    {
        public SleepSummary? Summary { get; set; }
    }

    public class SleepSummary
    {
        public SleepStageDetail? Deep { get; set; }
        public SleepStageDetail? Rem { get; set; }
        public SleepStageDetail? Light { get; set; }
        public SleepStageDetail? Awake { get; set; }
    }

    public class SleepStageDetail
    {
        public int Minutes { get; set; }
    }
}
