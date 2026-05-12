using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class EvaluateChancesStep : IPipelineStep
    {
        public string StepName => "Evaluating candidate chances based on CV";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string requirementsList = string.Join(", ", context.ExtractedRequirements);

            string systemPrompt = "You are an honest career coach. Compare the candidate's CV against the job requirements. Be brief. Identify matching skills and missing skills. Provide a short final verdict on their chances (0-100%). Respond in the language of the job description (usually Czech or English).";
            string userPrompt = $"JOB REQUIREMENTS: {requirementsList}\n\nCANDIDATE CV:\n{context.MasterCvContent}";

            context.FinalEvaluation = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);
        }
    }
}