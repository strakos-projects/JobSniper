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
        private readonly int _port;

        public event EventHandler<JobDataEventArgs>? OnDataReceived;

        public BrowserBridge(int port = 49152)
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
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string json = reader.ReadToEnd();

                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;

                            var args = new JobDataEventArgs
                            {
                                Url = root.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "",
                                JobText = root.TryGetProperty("text", out var textEl) ? textEl.GetString() ?? "" : "",
                                CompanyName = root.TryGetProperty("company", out var compEl) ? compEl.GetString() ?? "" : "",
                                JobTitle = root.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : ""
                            };

                            OnDataReceived?.Invoke(this, args);
                        }
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes("{\"status\":\"success\"}");
                    response.ContentType = "application/json";
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    response.StatusCode = 500;
                    byte[] buffer = Encoding.UTF8.GetBytes($"{{\"status\":\"error\", \"message\":\"{ex.Message}\"}}");
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                finally
                {
                    response.Close();
                }
            }
            else
            {
                response.StatusCode = 405; // Method Not Allowed
                response.Close();
            }
        }
    }
}