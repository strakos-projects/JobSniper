using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobSniper.Plugins
{
    // Class for passing data from the web to our application
    public class JobDataEventArgs : EventArgs
    {
        public string Url { get; set; }
        public string JobText { get; set; }
        public string CompanyName { get; set; }
        public string JobTitle { get; set; }
    }

    public class BrowserBridge
    {
        private HttpListener _listener;
        private bool _isRunning = false;
        public readonly int _port;

        public event EventHandler<JobDataEventArgs>? OnDataReceived;

        public Func<string, object>? OnCheckUrl; // Nyní vrací object, který zaserilizujeme do JSONu
        public Action<string, string>? OnSaveEvaluation;
        public Action<string>? OnDeleteEvaluation; // Nový delegát pro smazání

        // NOVÉ: Delegát pro spuštění lokálního AI workflow (předává URL a text inzerátu)
        public Action<string, string>? OnLocalEvaluationRequested;

        public BrowserBridge(int port = 55055)
        {
            _port = port;
            _listener = new HttpListener();

            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/jobsniper/");
            _listener.Prefixes.Add($"http://localhost:{_port}/jobsniper/");
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener.Start();
                _isRunning = true;

                Task.Run(() => ListenLoop());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserBridge] Cannot start server on port {_port}. Error: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Thrown during standard listener shutdown, can be ignored
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserBridge] Error processing request: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.AppendHeader("Access-Control-Allow-Origin", "*");
            response.AppendHeader("Access-Control-Allow-Methods", "POST, OPTIONS");
            response.AppendHeader("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            if (request.HttpMethod == "POST")
            {
                try
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
                    string json = reader.ReadToEnd();
                    using JsonDocument doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    string absolutePath = request.Url?.AbsolutePath.TrimEnd('/') ?? "";

                    // ROUTA 1: Kontrola URL
                    if (absolutePath.EndsWith("/check-url"))
                    {
                        string url = root.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";

                        // Získáme komplexní odpověď (isMatch, hasEvaluation, evaluationText)
                        object result = OnCheckUrl?.Invoke(url) ?? new { isMatch = false };

                        string jsonResponse = JsonSerializer.Serialize(result);
                        SendJsonResponse(response, 200, jsonResponse);
                    }
                    // ROUTA 2: Uložení AI Hodnocení
                    else if (absolutePath.EndsWith("/save-eval"))
                    {
                        string url = root.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";
                        string evalText = root.TryGetProperty("evaluationText", out var evalEl) ? evalEl.GetString() ?? "" : "";

                        OnSaveEvaluation?.Invoke(url, evalText);
                        SendJsonResponse(response, 200, "{\"status\":\"success\"}");
                    }
                    // ROUTA 3: Smazání AI Hodnocení
                    else if (absolutePath.EndsWith("/delete-eval"))
                    {
                        string url = root.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";

                        OnDeleteEvaluation?.Invoke(url);
                        SendJsonResponse(response, 200, "{\"status\":\"success\"}");
                    }
                    // ROUTA 4 (NOVÁ): Spuštění lokálního AI (LM Studio)
                    else if (absolutePath.EndsWith("/local-eval"))
                    {
                        string url = root.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";
                        string text = root.TryGetProperty("text", out var textEl) ? textEl.GetString() ?? "" : "";

                        // Předáme WPF aplikaci, ať workflow spustí asynchronně na pozadí
                        OnLocalEvaluationRequested?.Invoke(url, text);

                        // Odpovíme prohlížeči hned, že proces začal (aby doplněk nečekal minutu na model)
                        SendJsonResponse(response, 200, "{\"status\":\"processing\", \"message\":\"Local AI evaluation started.\"}");
                    }
                    // ROUTA 5: Původní scrapování (Fallback)
                    else
                    {
                        var args = new JobDataEventArgs
                        {
                            Url = root.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "",
                            JobText = root.TryGetProperty("text", out var textEl) ? textEl.GetString() ?? "" : "",
                            CompanyName = root.TryGetProperty("company", out var compEl) ? compEl.GetString() ?? "" : "",
                            JobTitle = root.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : ""
                        };

                        OnDataReceived?.Invoke(this, args);
                        SendJsonResponse(response, 200, "{\"status\":\"success\"}");
                    }
                }
                catch (Exception ex)
                {
                    SendJsonResponse(response, 500, $"{{\"status\":\"error\", \"message\":\"{ex.Message}\"}}");
                }
            }
            else
            {
                response.StatusCode = 405;
                response.Close();
            }
        }
        private void SendJsonResponse(HttpListenerResponse response, int statusCode, string jsonBody)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            byte[] buffer = Encoding.UTF8.GetBytes(jsonBody);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}