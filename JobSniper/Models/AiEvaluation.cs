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
        public int MatchScore { get; set; }
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
                // Žádný JSON nenalezen, vrátíme aspoň text
                return new AiEvaluation { FullCoachText = rawText };
            }

            // Rozdělení na JSON a textový posudek
            string jsonPart = rawText.Substring(startIndex, endIndex - startIndex + 1);
            string textPart = rawText.Substring(0, startIndex).Trim();

            // Nastavení parseru s klíčovým konvertorem pro Enum
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() } // TOTO ŘEŠÍ TVŮJ PROBLÉM
            };

            try
            {
                var evaluation = JsonSerializer.Deserialize<AiEvaluation>(jsonPart, options);
                if (evaluation != null)
                {
                    evaluation.FullCoachText = textPart; // Přibalíme si ten posudek
                }
                return evaluation;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Parse Error: {ex.Message}");
                // Fallback: vrátíme aspoň text, když JSON spadne
                return new AiEvaluation { FullCoachText = rawText };
            }
        }
        // Statická metoda, která nám nasimuluje to, co by jinak přišlo z webu
        public static AiEvaluation GetDemoPosudek()
        {
            return new AiEvaluation
            {
                MatchScore = 85,
                OverqualifiedRisk = 9,
                UnderqualifiedRisk = 8,
                HiddenRole = "Troubleshooter / Maintenance",
                RecommendedCvCategory = 3, // Facility
                Strategy = ApplyStrategy.B2B_Pitch,
                RedFlags = new List<string>
        {
            "Likely a hard filter for a high school diploma or university degree in the ATS",
            "Conservative corporate culture (joint-stock company)",
            "The leap from an active security role to a managerial position is too big for traditional HR"
        },
                FullCoachText = "As a pragmatic HR manager, I won't sugarcoat it for you. Your profile is absolutely fascinating – you are a prime example of a 'golden nugget' that the system cannot easily pigeonhole...\n\n(THE FULL AI TEXT WILL BE HERE)\n\nUnderqualified (on paper): You lack the required high school/university education and you don't have experience managing budgets.\n\nOverqualified (mentally): A person who programs asynchronous cores will go crazy from boredom after a month of dealing with clogged toilets."
            };
        }
    }
}