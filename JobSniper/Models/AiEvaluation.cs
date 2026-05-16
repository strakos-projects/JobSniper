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
        Pitch_Potential,
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
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new AiEvaluation { FullCoachText = "[Chyba]: Prázdný vstup od AI." };
            }

            int startIndex = rawText.IndexOf('{');
            int endIndex = rawText.LastIndexOf('}');

            if (startIndex == -1 || endIndex == -1 || startIndex > endIndex)
            {
                return new AiEvaluation
                {
                    FullCoachText = $"[Kritická chyba parsování]: AI nevrátila žádný validní JSON blok.\n\nPůvodní text:\n{rawText}",
                    RedFlags = new List<string> { "SYSTEM ERROR: No JSON detected" }
                };
            }

            string jsonPart = rawText.Substring(startIndex, endIndex - startIndex + 1);
            string textPart = startIndex > 0 ? rawText.Substring(0, startIndex).Trim() : "";

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
                AllowTrailingCommas = true // Pomáhá, pokud AI udělá čárku navíc na konci pole
            };

            try
            {
                var evaluation = JsonSerializer.Deserialize<AiEvaluation>(jsonPart, options);
                if (evaluation != null)
                {
                    // Pokud je textPart prázdný, uložíme si pro debug celý raw JSON
                    evaluation.FullCoachText = string.IsNullOrWhiteSpace(textPart) ? rawText : textPart;
                }
                return evaluation;
            }
            catch (JsonException ex)
            {
                // SENIORSKÝ FALLBACK: Chyba propadne až do UI (do CRM)
                System.Diagnostics.Debug.WriteLine($"JSON Parse Error: {ex.Message}");

                return new AiEvaluation
                {
                    // Přesný důvod (např. chybějící Enum) uvidíš přímo v textu posudku
                    FullCoachText = $"[Chyba deserializace JSONu]:\n{ex.Message}\n\n[INFO pro vývojáře: Zkontroluj, zda AI nevygenerovala hodnotu, která chybí v Enumu (např. Strategy), nebo špatný datový typ.]\n\nPůvodní JSON:\n{jsonPart}",

                    // Umělý RedFlag tě v UI okamžitě praští do očí
                    RedFlags = new List<string> { "SYSTEM ERROR: AI JSON Parsing Failed" },
                    RawHrScore = 0,
                    StrategicScore = 0
                };
            }
        }

        public static AiEvaluation GetDemoPosudek()
        {
            // ... Tvoje existující demo metoda zůstává nezměněna ...
            return new AiEvaluation
            {
                RawHrScore = 15,
                StrategicScore = 85,
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