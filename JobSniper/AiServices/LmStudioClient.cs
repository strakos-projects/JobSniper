using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobSniper.AiServices
{
    public class LmStudioClient : IAiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpointUrl;

        public LmStudioClient(string endpointUrl = "http://127.0.0.1:1234/v1/chat/completions")
        {
            _httpClient = new HttpClient();
            _endpointUrl = endpointUrl;
        }

        public async Task<string> GetCompletionAsync(string systemPrompt, string userPrompt)
        {
            var requestBody = new
            {
                model = "local-model", // LM Studio ignores this and uses the loaded one
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2 // Low temperature for analytical tasks
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_endpointUrl, content);

                // NOVÁ LOGIKA: Místo EnsureSuccessStatusCode si chyby zpracujeme sami
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();

                    // Pokusíme se vyčíst detail chyby z JSONu
                    string errorDetail = errorBody;
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(errorBody);
                        if (errorDoc.RootElement.TryGetProperty("error", out var errElement) &&
                            errElement.TryGetProperty("message", out var msgElement))
                        {
                            errorDetail = msgElement.GetString() ?? errorBody;
                        }
                    }
                    catch { /* Pokud to není JSON, necháme původní text */ }

                    // Vlastní, užitečné (helpful) chybové hlášky pro běžné problémy
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && errorDetail.Contains("context"))
                    {
                        throw new Exception($"LM Studio Context Size Exceeded!\n[FIX]: Open LM Studio -> Server Settings -> Increase 'Context Length' (n_ctx) to at least 8192, eject the model and reload it.\nOriginal error: {errorDetail}");
                    }

                    throw new Exception($"LM Studio API Error ({(int)response.StatusCode}): {errorDetail}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                return doc.RootElement
                          .GetProperty("choices")[0]
                          .GetProperty("message")
                          .GetProperty("content")
                          .GetString() ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Cannot connect to LM Studio. Is the Local Server running on {_endpointUrl}?\nDetails: {ex.Message}");
            }
        }
    }
}