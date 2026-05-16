using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class AnalyzeCandidateProfileStep : IPipelineStep
    {
        public string StepName => "Profiling Candidate (Generating Reality Check)";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            // Pokud už máme profil vygenerovaný (aby se to nevolalo u každého inzerátu znovu)
            if (!string.IsNullOrEmpty(context.CandidateProfileSummary))
                return;

            string systemPrompt = """
            You are a highly analytical HR profiler. Your task is to read a candidate's Master CV and summarize their "Reality" in exactly 3-4 brutal, objective sentences.
            
            You MUST extract:
            1. The candidate's primary domain(s) of actual work experience (e.g., IT, Retail, Security, Finance, Logistics).
            2. Their highest completed level of formal education (e.g., Primary school, High School, University degree).
            3. Any extreme anomalies (e.g., "Highly skilled self-taught programmer but formally only has primary education" OR "University degree in Law but only has experience as a barista").
            
            Do not flatter the candidate. Be completely objective. Output ONLY the summary text.
            """;

            string userPrompt = $"""
            CANDIDATE CV:
            {context.MasterCvContent}
            """;

            context.CandidateProfileSummary = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);
        }
    }
}