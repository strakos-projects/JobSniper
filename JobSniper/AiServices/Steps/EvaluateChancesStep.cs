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