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

        public async Task<PipelineContext> RunPipelineAsync(string jobUrl, string jobDescription, string masterCvContent)
        {
            var context = new PipelineContext
            {
                JobUrl = jobUrl,
                JobDescription = jobDescription,
                MasterCvContent = masterCvContent
            };

            Console.WriteLine("[Pipeline] Starting AI Evaluation Pipeline...");

            foreach (var step in _steps)
            {
                Console.WriteLine($"[Pipeline] Executing step: {step.StepName}");
                await step.ExecuteAsync(context, _aiClient);
            }

            Console.WriteLine("[Pipeline] Evaluation finished successfully.");
            return context;
        }
    }
}