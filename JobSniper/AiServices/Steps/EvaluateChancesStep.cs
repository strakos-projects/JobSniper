using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class EvaluateChancesStep : IPipelineStep
    {
        public string StepName => "Evaluating candidate chances based on CV";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string requirementsList = string.Join(", ", context.ExtractedRequirements);

            string systemPrompt = @"You are a strict, pragmatic career coach and HR expert. Compare the candidate's CV against the job requirements. 

CRITICAL STRATEGY DEFINITIONS:
- 'DumbDown': Select this ONLY if the candidate is severely overqualified for a manual/security/lower-tier role. It means the CV must be aggressively stripped of advanced IT/AI skills.
- 'B2B_Pitch': Select this if the company needs senior IT/AI solutions, but might not hire a full-time employee.
- 'Ignore': STRICTLY select this if there is a hard formal barrier. For example, if the job strictly requires a High School (SŠ) or University degree, but the candidate's formal education is lower. Also select this if the role is heavily bureaucratic, administrative, or legal (e.g., HOA/SVJ management), which completely clashes with the candidate's engineering/solopreneur profile. Do not try to force a fit. Waste of time.
- 'Standard': Standard application process. Good match.

Respond ONLY with a valid JSON block containing the evaluation, using this exact structure:
{
    ""RawHrScore"": (int 0-100),
    ""StrategicScore"": (int 0-100),
    ""OverqualifiedRisk"": (int 0-10),
    ""UnderqualifiedRisk"": (int 0-10),
    ""HiddenRole"": ""(string)"",
    ""RecommendedCvCategory"": (int 1-4),
    ""Strategy"": ""Standard"" | ""DumbDown"" | ""B2B_Pitch"" | ""Ignore"",
    ""StrategyReasoning"": ""(short explanation)"",
    ""GoNoGo"": true/false,
    ""RedFlags"": [""flag1"", ""flag2""]
}";
            string userPrompt = $"JOB REQUIREMENTS: {requirementsList}\n\nCANDIDATE CV:\n{context.MasterCvContent}";

            context.FinalEvaluation = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);
        }
    }
}