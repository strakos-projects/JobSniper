using System;
using System.IO;
using System.Threading.Tasks;

namespace JobSniper.AiServices.Steps
{
    public class EvaluateChancesStep : IPipelineStep
    {
        public string StepName => "Evaluating candidate chances based on Reality Match";

        public async Task ExecuteAsync(PipelineContext context, IAiClient aiClient)
        {
            string requirementsList = context.ExtractedRequirements != null && context.ExtractedRequirements.Count > 0
                ? string.Join(", ", context.ExtractedRequirements)
                : "None extracted";

            string systemPrompt = """
            You are a ruthless, analytical HR ATS (Applicant Tracking System) simulator. Your goal is to score realistic chances, not to flatter the candidate.

            PROCESS (CHAIN OF THOUGHT):
            1. PreAnalysis: Objectively compare the Job Requirements vs Candidate Reality.
            2. Assign scores STRICTLY following this hierarchical rule-set (evaluate from top to bottom, stop at the first match):

            HIERARCHICAL EVALUATION RULES:
            RULE 1 (FATAL BLOCKER): If the job requires a specific physical license (e.g., Security Guard, Forklift) and it is missing from Candidate Reality -> RawHrScore=0, StrategicScore=0, Strategy='Ignore'.
            RULE 2 (TECH STACK MISMATCH): If it's a software/IT job, EXPLICITLY compare languages. If Job wants Python/Java/C++ and Candidate has C#/.NET -> RawHrScore=10-20, StrategicScore=30-45. Strategy='B2B_Pitch' or 'Ignore'. (Even if the candidate is a genius, traditional HR will block the wrong language).
            RULE 3 (APTITUDE ANOMALY): If Tech Stack MATCHES, but candidate lacks formal education/degree, AND they have verifiable complex projects matching the job's architecture -> RawHrScore=20-40, StrategicScore=60-80. Strategy='Pitch_Potential'.
            RULE 4 (EXACT MATCH): Domains, tech stack, and education all match perfectly -> RawHrScore=70-90, StrategicScore=85-100. Strategy='Standard'.
            
            EXPECTED OUTPUT FORMAT (Strict JSON ONLY):
            {
                "PreAnalysis": "Candidate has background in X. Job demands Y. Formal education is missing but demonstrated adaptability...",
                "RawHrScore": 15,
                "StrategicScore": 40,
                "OverqualifiedRisk": 2,
                "UnderqualifiedRisk": 8,
                "HiddenRole": "Junior Alternative",
                "RecommendedCvCategory": 3,
                "Strategy": "Pitch_Potential",
                "StrategyReasoning": "Lacks domain experience, but high cognitive capacity matches the employer's request for logical thinking.",
                "GoNoGo": false,
                "RedFlags": ["Missing formal degree"]
            }
            """;
            string userPrompt = $$"""
            === JOB REALITY ===
            {{context.JobRealitySummary}}

            === REQUIRED JOB TECH STACK ===
            {{context.JobTechStack}}

            === HARD REQUIREMENTS ===
            {{requirementsList}}

            === CANDIDATE REALITY (Including their Tech Stack) ===
            {{context.CandidateProfileSummary}}

            Respond purely with JSON. Start here:
            {
            """;

            // 1. Získání odpovědi
            string rawResponse = await aiClient.GetCompletionAsync(systemPrompt, userPrompt);
            context.FinalEvaluation = rawResponse;

            // ----------------------------------------------------------------------
            // DEBUG LOGOVÁNÍ - V PRODUKCI ZAKOMENTOVAT
            // ----------------------------------------------------------------------
            try
            {/*
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AiDebugLogs");
                Directory.CreateDirectory(logDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string logFile = Path.Combine(logDir, $"EvaluateChances_{timestamp}.txt");

                string logContent = $"""
                ======================================================
                SYSTEM PROMPT
                ======================================================
                {systemPrompt}

                ======================================================
                USER PROMPT
                ======================================================
                {userPrompt}

                ======================================================
                RAW AI RESPONSE
                ======================================================
                {rawResponse}
                """;

                await File.WriteAllTextAsync(logFile, logContent);
                Console.WriteLine($"[DEBUG] Prompt and response dumped to: {logFile}");
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG ERROR] Could not write debug log: {ex.Message}");
            }
            // ----------------------------------------------------------------------
        }
    }
}