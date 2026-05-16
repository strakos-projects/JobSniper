using System.Collections.Generic;

namespace JobSniper.AiServices
{
    public class PipelineContext
    {
        public string JobUrl { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string MasterCvContent { get; set; } = string.Empty;

        // Chain of Thought intermediate results
        public List<string> ExtractedRequirements { get; set; } = new();
        public string FinalEvaluation { get; set; } = string.Empty;
        public bool IsHardRequirementFailed { get; set; } = false;
    }
}