using System;
using System.Linq;
using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class ExtractKeywordsStep : IPipelineStep
    {
        public string StepName => "Extracting requirements from job description";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string systemPrompt = "You are a technical HR analyst. Extract the required technologies, languages, and skills from the provided job description. Output ONLY a comma-separated list. No other text.";
            string userPrompt = $"Job Description:\n{context.JobDescription}";

            string response = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);

            context.ExtractedRequirements = response.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(k => k.Trim())
                                                    .Where(k => !string.IsNullOrEmpty(k))
                                                    .ToList();
        }
    }
}