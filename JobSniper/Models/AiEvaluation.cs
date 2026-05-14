using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobSniper.Models
{
    public enum ApplyStrategy
    {
        Standard,
        DumbDown,
        B2B_Pitch,
        Ignore
    }

    public class AiEvaluation
    {
        public string EvaluatedJobDescription { get; set; }

        // NOVÉ METRIKY
        public int RawHrScore { get; set; }
        public int StrategicScore { get; set; }
        public string StrategyReasoning { get; set; }
        public bool GoNoGo { get; set; }

        // PŮVODNÍ METRIKY
        public int OverqualifiedRisk { get; set; }
        public int UnderqualifiedRisk { get; set; }
        public string HiddenRole { get; set; }
        public int RecommendedCvCategory { get; set; }
        public ApplyStrategy Strategy { get; set; }
        public List<string> RedFlags { get; set; } = new List<string>();
        public string FullCoachText { get; set; }

        public static AiEvaluation ParseFromAiOutput(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return null;

            int startIndex = rawText.IndexOf('{');
            int endIndex = rawText.LastIndexOf('}');

            if (startIndex == -1 || endIndex == -1 || startIndex > endIndex)
            {
                return new AiEvaluation { FullCoachText = rawText };
            }

            string jsonPart = rawText.Substring(startIndex, endIndex - startIndex + 1);
            string textPart = rawText.Substring(0, startIndex).Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            try
            {
                var evaluation = JsonSerializer.Deserialize<AiEvaluation>(jsonPart, options);
                if (evaluation != null)
                {
                    evaluation.FullCoachText = textPart;
                }
                return evaluation;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Parse Error: {ex.Message}");
                return new AiEvaluation { FullCoachText = rawText };
            }
        }

        public static AiEvaluation GetDemoPosudek()
        {
            return new AiEvaluation
            {
                RawHrScore = 15, // HR uvidí chybějící VŠ a ostrahu
                StrategicScore = 85, // Po úpravě CV naprostý fit
                GoNoGo = true,
                StrategyReasoning = "Firma hledá technika s tahem na branku, zamlč složité C# architektury a prodej se jako analytický dispečer/technik.",
                OverqualifiedRisk = 9,
                UnderqualifiedRisk = 8,
                HiddenRole = "Troubleshooter / Maintenance",
                RecommendedCvCategory = 3,
                Strategy = ApplyStrategy.DumbDown,
                RedFlags = new List<string>
                {
                    "Likely a hard filter for a high school diploma or university degree in the ATS",
                    "Conservative corporate culture",
                    "The leap from an active security role to a managerial position is too big for traditional HR"
                },
                FullCoachText = "As a pragmatic HR manager, I won't sugarcoat it for you..."
            };
        }
    }
}