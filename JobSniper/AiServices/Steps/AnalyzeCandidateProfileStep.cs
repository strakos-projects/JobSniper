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
            You are a highly analytical HR data extractor. Your task is to read a candidate's Master CV and extract their factual reality WITHOUT bias.

            CRITICAL INSTRUCTION - FORMAT YOUR OUTPUT EXACTLY LIKE THIS:
            1. DOMAINS OF EXPERIENCE: [List ALL domains, e.g., Physical Security (8 years), Software Development (3 years)]
            2. LICENSES & CERTIFICATES: [Extract ALL official licenses, e.g., Professional Security Guard License, First Aid, IT Certifications]
            3. FORMAL EDUCATION: [State the exact highest formal degree, e.g., Primary School / Základní]
            4. DOMINANT TECH STACK: [List primary programming languages, e.g., C#, .NET, Next.js. If none, write "None"]
            5. APTITUDE ANOMALY: [Briefly state if their demonstrated skills exceed their formal education]
            """;

            string userPrompt = $"""
            CANDIDATE CV:
            {context.MasterCvContent}
            """;

            context.CandidateProfileSummary = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);
        }
    }
}