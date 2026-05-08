using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JobSniper.Models
{
    public class EvaluationRepository
    {
        private readonly string _filePath;
        private Dictionary<string, AiEvaluation> _evaluations;

        public EvaluationRepository(string filePath)
        {
            _filePath = filePath;
            _evaluations = new Dictionary<string, AiEvaluation>();
        }

        public void Load()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    _evaluations = JsonSerializer.Deserialize<Dictionary<string, AiEvaluation>>(json) ?? new Dictionary<string, AiEvaluation>();
                }
                catch
                {
                    _evaluations = new Dictionary<string, AiEvaluation>();
                }
            }
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(_evaluations, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public AiEvaluation GetEvaluation(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return null;
            return _evaluations.TryGetValue(jobId, out var eval) ? eval : null;
        }

        public void AddOrUpdateEvaluation(string jobId, string text)
        {
            if (string.IsNullOrEmpty(jobId)) return;

            if (!_evaluations.ContainsKey(jobId))
            {
                _evaluations[jobId] = new AiEvaluation();
            }

            // Můžeš sem později přidat i parsování skóre atd.
            _evaluations[jobId].FullCoachText = text;
            Save(); // Ukládáme POUZE tento menší JSON soubor
        }
    }
}