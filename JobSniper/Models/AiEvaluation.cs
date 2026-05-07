using System;
using System.Collections.Generic;

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