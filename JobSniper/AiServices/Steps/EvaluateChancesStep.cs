using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class EvaluateChancesStep : IPipelineStep
    {
        public string StepName => "Evaluating candidate chances based on CV";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string requirementsList = string.Join(", ", context.ExtractedRequirements);

            // Využití Raw String Literals pro čistý formát a vynucený Chain of Thought
            string systemPrompt = """
            You are a strict, pragmatic career coach and HR expert. Your task is to evaluate the candidate's CV against the job requirements.

            CRITICAL RULE - THE "KILL SWITCH": 
            Before calculating any score, you MUST verify if the candidate meets all legally mandated or strict formal requirements (e.g., specific driver's licenses, firearms licenses / Zbrojní průkaz, formal education levels like high school/university). 
            Experience NEVER replaces a formal license.
            If a critical license or degree is required but missing in the CV, the candidate is a HARD NO. In this case, RawHrScore MUST be 0, StrategicScore MUST be 0, GoNoGo MUST be false, and Strategy MUST be 'Ignore'.

            CRITICAL STRATEGY DEFINITIONS:
            - 'Ignore': STRICTLY select this if there is a hard formal barrier (missing license, missing degree) OR if the role is purely bureaucratic/administrative, which completely clashes with the candidate's engineering profile. Waste of time.
            - 'DumbDown': Select this ONLY if the candidate is severely overqualified for a manual/security/lower-tier role AND actually meets all legal/formal requirements for it.
            - 'B2B_Pitch': Select this if the company needs senior IT/AI solutions, but might not hire a full-time employee.
            - 'Standard': Standard application process. Good match.

            Respond ONLY with a valid JSON block. The "PreAnalysis" field MUST be evaluated FIRST to trigger the Kill Switch if necessary:
            {
                "PreAnalysis": "(Identify any missing strict formal requirements. If a license like firearms/zbrojní průkaz is required but missing, explicitly state it here and trigger the kill switch.)",
                "RawHrScore": (int 0-100),
                "StrategicScore": (int 0-100),
                "OverqualifiedRisk": (int 0-10),
                "UnderqualifiedRisk": (int 0-10),
                "HiddenRole": "(string)",
                "RecommendedCvCategory": (int 1-4),
                "Strategy": "Standard" | "DumbDown" | "B2B_Pitch" | "Ignore",
                "StrategyReasoning": "(short explanation based on PreAnalysis)",
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