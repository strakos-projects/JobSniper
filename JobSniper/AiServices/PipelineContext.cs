using System.Collections.Generic;

namespace JobSniper.AiServices
{
    public static class JsonSanitizer
    {
        public static string CleanJsonOutput(string rawOutput)
        {
            if (string.IsNullOrWhiteSpace(rawOutput)) return "{}";

            int startIndex = rawOutput.IndexOf('{');
            if (startIndex == -1) return "{}";

            int braceCount = 0;
            for (int i = startIndex; i < rawOutput.Length; i++)
            {
                if (rawOutput[i] == '{') braceCount++;
                else if (rawOutput[i] == '}') braceCount--;

                if (braceCount == 0)
                {
                    return rawOutput.Substring(startIndex, i - startIndex + 1);
                }
            }

            return rawOutput.Substring(startIndex);
        }
    }
    public class PipelineContext
    {
        public string JobUrl { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string MasterCvContent { get; set; } = string.Empty;

        // Chain of Thought intermediate results
        public List<string> ExtractedRequirements { get; set; } = new();
        public string FinalEvaluation { get; set; } = string.Empty;
        public bool IsHardRequirementFailed { get; set; } = false;

        public string CandidateProfileSummary { get; set; }
        public string JobRealitySummary { get; set; }

        public string JobTechStack { get; set; }
    }
}