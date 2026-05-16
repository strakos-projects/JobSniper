using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class EvaluateHardRequirementsStep : IPipelineStep
    {
        public string StepName => "Hard Requirements Pre-Check";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string systemPrompt = """
            You are an uncompromising compliance officer. Your SOLE task is to find STRICT LEGAL OR SAFETY BLOCKERS.
            
            CRITICAL RULES:
            1. ONLY look for government-issued licenses (e.g., Zbrojní průkaz / Firearms license, specific driver's licenses, security clearances).
            2. DO NOT treat formal education (SŠ, VŠ, Maturita) or "years of experience" as hard blockers here. Education is a soft HR requirement, not a legal blocker.
            3. If a LEGAL/SAFETY license is required but missing, Passed MUST be false. Otherwise, Passed is true.
            
            Respond ONLY with a valid JSON matching this exact structure:
            {
                "ExtractedRequirements": ["item 1", "item 2"],
                "FoundInCV": ["item 1"],
                "MissingRequirements": ["item 2"],
                "Passed": true/false,
                "BlockReason": "Brief reason or null"
            }
            """;

            string userPrompt = $"""
            === JOB ADVERTISEMENT ===
            {context.JobDescription}

            === CANDIDATE CV ===
            {context.MasterCvContent}
            """;

            try
            {
                string jsonResponse = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                bool passed = doc.RootElement.GetProperty("Passed").GetBoolean();

                if (!passed)
                {
                    string reason = doc.RootElement.GetProperty("BlockReason").GetString();
                    Console.WriteLine($"[Hard Check] REJECTED: {reason}");
                    context.IsHardRequirementFailed = true;

                    context.FinalEvaluation = $$"""
                    {
                        "RawHrScore": 0,
                        "StrategicScore": 0,
                        "OverqualifiedRisk": 10,
                        "UnderqualifiedRisk": 10,
                        "HiddenRole": "Rejected (Hard Blocker)",
                        "RecommendedCvCategory": 1,
                        "Strategy": "Ignore",
                        "StrategyReasoning": "{{reason}}",
                        "GoNoGo": false,
                        "RedFlags": ["Critical legal requirement not met: {{reason}}"]
                    }
                    """;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hard Check] Warning: {ex.Message}");
            }
        }
    }
}