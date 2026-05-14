using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JobSniper.Models;

namespace JobSniper.Scrapers
{
    public interface IScraper
    {
        string Name { get; }

        // Původní metoda pro stažení seznamu inzerátů
        Task<List<JobOffer>> ScrapeUrlAsync(string url, Action<string> logAction, CancellationToken cancellationToken = default);

        // NOVINKA: Výchozí implementace (Default Interface Method)
        // Nemusíš to psát do ExampleScraper.cs ani do těch privátních!
        async Task<string> GetJobDescriptionAsync(string url, Action<string> logAction = null, CancellationToken cancellationToken = default)
        {
            logAction?.Invoke($"[Scraper] Fetching job description from: {url}");

            try
            {
                // HttpClient s AllowAutoRedirect = true zachytí kód 301/302 a stáhne finální HTML
                using var handler = new HttpClientHandler { AllowAutoRedirect = true };
                using var client = new HttpClient(handler);

                // Maskování za běžný prohlížeč
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Detekce přesměrování a logování v AJ
                if (response.RequestMessage?.RequestUri != null && response.RequestMessage.RequestUri.ToString() != url)
                {
                    logAction?.Invoke($"[Scraper Warning] Server redirected the request to: {response.RequestMessage.RequestUri}");
                }

                string html = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(html))
                {
                    logAction?.Invoke($"[Scraper Error] Downloaded HTML is empty.");
                    return string.Empty;
                }

                // Očištění HTML na čistý text
                string text = Regex.Replace(html, @"<(script|style)[^>]*>[\s\S]*?</\1>", " ", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, "<[^>]*>", " ");
                text = System.Net.WebUtility.HtmlDecode(text);
                text = Regex.Replace(text, @"\s+", " ").Trim();

                logAction?.Invoke($"[Scraper] Successfully extracted {text.Length} characters of raw text.");
                return text;
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"[Scraper Error] Failed to fetch job description: {ex.Message}");
                return string.Empty;
            }
        }
    }
}