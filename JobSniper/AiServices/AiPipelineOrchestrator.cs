using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobSniper.AiServices
{
    public class AiPipelineOrchestrator
    {
        private readonly IAiClient _aiClient;
        private readonly List<IPipelineStep> _steps;

        public AiPipelineOrchestrator(IAiClient aiClient)
        {
            _aiClient = aiClient;
            _steps = new List<IPipelineStep>();
        }

        public void AddStep(IPipelineStep step)
        {
            _steps.Add(step);
        }

        // FIX: Přidán parametr candidateProfileSummary pro udržení cache napříč inzeráty
        public async Task<PipelineContext> RunPipelineAsync(string jobUrl, string jobDescription, string masterCvContent, string candidateProfileSummary = null)
        {
            var context = new PipelineContext
            {
                JobUrl = jobUrl,
                JobDescription = jobDescription,
                MasterCvContent = masterCvContent,
                CandidateProfileSummary = candidateProfileSummary // FIX: Dosazení existující cache
            };

            Console.WriteLine("[Pipeline] Starting AI Evaluation Pipeline...");

            foreach (var step in _steps)
            {
                if (context.IsHardRequirementFailed)
                {
                    Console.WriteLine($"[Pipeline] Interrupting execution. Step '{step.StepName}' was skipped due to Hard Requirements failure.");
                    break;
                }
                Console.WriteLine($"[Pipeline] Executing step: {step.StepName}");
                await step.ExecuteAsync(context, _aiClient);
            }

            Console.WriteLine("[Pipeline] Evaluation finished successfully.");
            return context;
        }
    }
}