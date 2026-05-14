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
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    };
                    _evaluations = JsonSerializer.Deserialize<Dictionary<string, AiEvaluation>>(json, options) ?? new Dictionary<string, AiEvaluation>();
                }
                catch
                {
                    _evaluations = new Dictionary<string, AiEvaluation>();
                }
            }
        }

        public void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            string json = JsonSerializer.Serialize(_evaluations, options);
            File.WriteAllText(_filePath, json);
        }

        public AiEvaluation GetEvaluation(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return null;
            return _evaluations.TryGetValue(jobId, out var eval) ? eval : null;
        }
        public void DeleteEvaluation(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return;

            if (_evaluations.ContainsKey(jobId))
            {
                _evaluations.Remove(jobId);
                Save(); 
            }
        }
        public void AddOrUpdateEvaluation(string jobId, string rawAiText, string jobDescription = null)
        {
            if (string.IsNullOrEmpty(jobId) || string.IsNullOrWhiteSpace(rawAiText)) return;
            var parsedEval = AiEvaluation.ParseFromAiOutput(rawAiText);

            if (parsedEval != null)
            {
                // Pokud jsme předali zdrojový text inzerátu, uložíme ho do objektu
                if (!string.IsNullOrEmpty(jobDescription))
                    parsedEval.EvaluatedJobDescription = jobDescription;

                _evaluations[jobId] = parsedEval;
            }
            else
            {
                if (!_evaluations.ContainsKey(jobId))
                {
                    _evaluations[jobId] = new AiEvaluation();
                }
                _evaluations[jobId].FullCoachText = rawAiText;
                if (!string.IsNullOrEmpty(jobDescription))
                    _evaluations[jobId].EvaluatedJobDescription = jobDescription;
            }
            Save();
        }
    }
}