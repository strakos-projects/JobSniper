using System.Collections.Generic;

namespace JobSniper.Models
{
    // Enum musí být stejný, aby deserializace z JSONu fungovala
    public enum ApplyStrategy
    {
        Standard,
        DumbDown,
        B2B_Pitch,
        Pitch_Potential,
        Ignore
    }

    public class AiEvaluation
    {
        public string EvaluatedJobDescription { get; set; }

        public int RawHrScore { get; set; }
        public int StrategicScore { get; set; }
        public string StrategyReasoning { get; set; }
        public bool GoNoGo { get; set; }

        public int OverqualifiedRisk { get; set; }
        public int UnderqualifiedRisk { get; set; }
        public string HiddenRole { get; set; }
        public int RecommendedCvCategory { get; set; }
        public ApplyStrategy Strategy { get; set; }
        public List<string> RedFlags { get; set; } = new List<string>();
        public string FullCoachText { get; set; }
    }
}