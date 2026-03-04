using JobSniper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobSniper.Scrapers
{
    public interface IScraper
    {
        // Každý scraper se musí umět představit (to uvidíme v menu)
        string Name { get; }

        // Každý scraper musí mít tuto metodu
        Task<List<JobOffer>> ScrapeUrlAsync(string startUrl, Action<string> logMessage);
    }
}