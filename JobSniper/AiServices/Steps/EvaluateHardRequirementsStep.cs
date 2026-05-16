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
            // Využití C# Raw String Literals pro naprosto čistý prompt bez escapování
            string systemPrompt = """
            You are an uncompromising HR auditor and compliance officer. Your SOLE task is to find "Hard Blockers" - strict, legally required licenses or certifications that the candidate is missing.
            
            CRITICAL RULE: Experience DOES NOT replace a license. If a firearms license (zbrojní průkaz), driving license, or specific security clearance is required, it MUST be explicitly written in the CV.
            
            You MUST follow this exact reasoning process in your JSON output:
            1. Extract all strict government or legal licenses required by the job ad.
            2. List relevant licenses found in the candidate's CV.
            3. Compare them. If a required license is missing, Passed MUST be false.
            
            Respond ONLY with a valid JSON matching this exact structure:
            {
                "ExtractedRequirements": ["item 1", "item 2"],
                "FoundInCV": ["item 1"],
                "MissingRequirements": ["item 2"],
                "Passed": false,
                "BlockReason": "Candidate is missing item 2"
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

                    // Fallback JSON pro CRM - opět čistě přes Raw String Literals
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
                        "RedFlags": ["Critical requirement not met: {{reason}}"]
                    }
                    """;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hard Check] Warning: Error while evaluating pre-check: {ex.Message}");
            }
        }
    }
}