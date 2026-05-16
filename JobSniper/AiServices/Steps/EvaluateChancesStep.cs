using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class EvaluateChancesStep : IPipelineStep
    {
        public string StepName => "Evaluating candidate chances based on CV";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string requirementsList = string.Join(", ", context.ExtractedRequirements);

            string systemPrompt = """
            You are a pragmatic career coach evaluating an unconventional candidate. The candidate has advanced, self-taught IT/AI skills (C#, .NET, AI integrations) but lacks formal higher education (has 'Základní vzdělání') and has a background in security/parking management.

            RULE 1 - LEGAL BLOCKERS (KILL SWITCH):
            If missing a strict legal license (e.g., Zbrojní průkaz), RawHrScore MUST be 0, Strategy = 'Ignore'.

            RULE 2 - THE EDUCATION GAP (DO NOT KILL):
            If the job requires "SŠ" (High School) or "VŠ" (University), DO NOT automatically reject the candidate. 
            Instead, apply this logic:
            - Traditional HR will likely filter them out automatically -> Keep 'RawHrScore' low (e.g., 10-30%).
            - BUT if the candidate's actual IT/AI skills match the technical requirements perfectly, the 'StrategicScore' should be HIGH (e.g., 70-95%). 
            - In these cases, recommend 'B2B_Pitch' or 'Standard' strategy relying on their GitHub/Portfolio to bypass traditional HR.

            Respond ONLY with a valid JSON block. Evaluate "PreAnalysis" first:
            {
                "PreAnalysis": "(Identify legal blockers vs soft HR requirements like education. How will traditional HR react to the education gap?)",
                "RawHrScore": (int 0-100),
                "StrategicScore": (int 0-100),
                "OverqualifiedRisk": (int 0-10),
                "UnderqualifiedRisk": (int 0-10),
                "HiddenRole": "(string)",
                "RecommendedCvCategory": (int 1-4),
                "Strategy": "Standard" | "DumbDown" | "B2B_Pitch" | "Ignore",
                "StrategyReasoning": "(short explanation of the gap between Raw and Strategic score)",
                "GoNoGo": true/false,
                "RedFlags": ["flag1", "flag2"]
            }
            """;

            string userPrompt = $"""
            JOB REQUIREMENTS: 
            {requirementsList}

            CANDIDATE CV:
            {context.MasterCvContent}
            """;

            context.FinalEvaluation = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);
        }
    }
}