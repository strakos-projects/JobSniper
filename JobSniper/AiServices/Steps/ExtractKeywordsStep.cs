using System;
using System.Linq;
using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class ExtractKeywordsStep : IPipelineStep
    {
        public string StepName => "Extracting comprehensive requirements from job description";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            // Rozšířený prompt, který donutí model vytáhnout i měkké a byrokratické požadavky
            string systemPrompt = """
            You are a meticulous HR data extractor. Your task is to extract ALL explicit requirements from the job description, not just technical skills. 
            
            You MUST actively look for and extract:
            1. Technical skills (languages, frameworks, tools).
            2. Formal Education (e.g., University degree, SŠ/High School, formal IT diploma).
            3. Legal or specific licenses (e.g., Driver's license B, Firearms license / Zbrojní průkaz, security clearances).
            4. Seniority & Environment (e.g., corporate background, 5+ years of banking experience, leadership).
            
            Output ONLY a single comma-separated list containing all these extracted points. Do not use bullet points, categories, or conversational text.
            """;

            string userPrompt = $"""
            Job Description:
            {context.JobDescription}
            """;

            string response = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);

            // Zpracování zůstává stejné, rozsekáme to do Listu
            context.ExtractedRequirements = response.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(k => k.Trim())
                                                    .Where(k => !string.IsNullOrEmpty(k))
                                                    .ToList();
        }
    }
}