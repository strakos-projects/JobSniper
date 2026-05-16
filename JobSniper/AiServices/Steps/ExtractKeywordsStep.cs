using System;
using System.Linq;
using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class ExtractKeywordsStep : IPipelineStep
    {
        public string StepName => "Extracting Job Reality & Hard Requirements";
        public string JobTechStack { get; set; }
        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string systemPrompt = """
            You are a meticulous HR analyst. Your task is to strip away all corporate marketing jargon from the job description and extract the BRUTAL REALITY of the job.
            
            Focus on:
            1. The core domain and actual daily reality of the job.
            2. Strict formal requirements (Education, Licenses, Years of experience).
            3. MINDSET & SOFT REQUIREMENTS: Explicitly note if the employer emphasizes traits like "logical thinking", "common sense", "willingness to learn", or "drive" OVER rigid past experience.
            
            FORMAT YOUR OUTPUT EXACTLY LIKE THIS:
            SUMMARY: [3-4 sentences]
            TECH_STACK: [Extract mandatory programming languages and frameworks, e.g., Python, React, Azure]
            REQUIREMENTS: [comma-separated list of hard requirements like Degree, Languages]
            """;
            string userPrompt = $"""
            === JOB DESCRIPTION ===
            {context.JobDescription}
            """;

            string response = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);

            // 1. Rozdělení na "vše před požadavky" a "požadavky"
            var reqParts = response.Split(new[] { "REQUIREMENTS:" }, StringSplitOptions.None);

            if (reqParts.Length > 0)
            {
                // 2. Rozdělení "všeho předtím" na Summary a Tech Stack
                var techStackParts = reqParts[0].Split(new[] { "TECH_STACK:" }, StringSplitOptions.None);

                if (techStackParts.Length > 1)
                {
                    // Ideální scénář: AI dodala obojí
                    context.JobRealitySummary = techStackParts[0].Replace("SUMMARY:", "").Trim();
                    context.JobTechStack = techStackParts[1].Trim();
                }
                else
                {
                    // Fallback: AI zapomněla vypsat 'TECH_STACK:'
                    context.JobRealitySummary = techStackParts[0].Replace("SUMMARY:", "").Trim();
                    context.JobTechStack = "Tech stack not explicitly extracted.";
                }
            }

            // 3. Záchrana a uložení REQUIREMENTS
            if (reqParts.Length > 1)
            {
                context.ExtractedRequirements = reqParts[1]
                    .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .ToList();
            }
            else
            {
                context.ExtractedRequirements = new System.Collections.Generic.List<string>();
            }
        }
    }
}