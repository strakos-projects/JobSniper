using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class EvaluateChancesStep : IPipelineStep
    {
        public string StepName => "Evaluating candidate chances based on CV";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string requirementsList = string.Join(", ", context.ExtractedRequirements);

            string systemPrompt = $$"""
            You are a highly cynical career coach evaluating a candidate. 
            
            CANDIDATE'S REALITY:
            {{context.CandidateProfileSummary}}

            YOU MUST STRICTLY FOLLOW THIS EVALUATION MATRIX:

            1. LEGAL BLOCKERS (KILL SWITCH):
            Is the candidate missing a strict legal/safety license specifically required by the job? -> RawHrScore=0, StrategicScore=0, Strategy='Ignore'.

            2. DOMAIN MISMATCH (AUTO-REJECT):
            Is this job in a completely unrelated field to the candidate's core experience (e.g., Candidate is a builder, job is Senior Banker)? -> RawHrScore=0-5, StrategicScore=0-5, Strategy='Ignore'. DO NOT invent transferable skills.

            3. CORPORATE REALITY VS. EDUCATION:
            Is this a strict corporate/bureaucratic role that legally or culturally demands a formal degree the candidate lacks? 
            -> RawHrScore=10-20. StrategicScore can be higher ONLY if their self-taught tech/hard skills match perfectly. Strategy='Standard' or 'B2B_Pitch'.

            4. OVERQUALIFIED / MANUAL ROLES:
            Is the candidate severely overqualified for this lower-tier or manual role based on their advanced skills?
            -> RawHrScore=50-80, StrategicScore=80-95. Strategy='DumbDown' (CV must be stripped of advanced skills).

            5. GOOD MATCH:
            Domains match, education aligns or is compensated by strong portfolio.
            -> RawHrScore=70-90, StrategicScore=85-100. Strategy='Standard'.

            Respond ONLY with a valid JSON block. Evaluate "PreAnalysis" first:
            {
                "PreAnalysis": "(Evaluate DOMAIN MATCH first based on Candidate's Reality vs Job. Analyze corporate strictness and formal education gaps.)",
                "RawHrScore": (int 0-100),
                "StrategicScore": (int 0-100),
                "OverqualifiedRisk": (int 0-10),
                "UnderqualifiedRisk": (int 0-10),
                "HiddenRole": "(string)",
                "RecommendedCvCategory": (int 1-4),
                "Strategy": "Standard" | "DumbDown" | "B2B_Pitch" | "Ignore",
                "StrategyReasoning": "(short explanation of the decision)",
                "GoNoGo": true/false,
                "RedFlags": ["flag1", "flag2"]
            }
            """;

            // FIX: Přidán kompletní inzerát, aby matice výše dokázala detekovat obor (Finance/Reality atd.)
            string userPrompt = $"""
            === ORIGINAL JOB ADVERTISEMENT ===
            {context.JobDescription}

            === EXTRACTED KEY REQUIREMENTS ===
            {requirementsList}

            === CANDIDATE CV ===
            {context.MasterCvContent}
            """;

            context.FinalEvaluation = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);
        }
    }
}