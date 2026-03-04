using JobSniper.Models;
using System.Text.RegularExpressions;

using JobSniper.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JobSniper.Scrapers
{
    public class ExampleScraper : IScraper
    {
        public string Name => "ExampleScraper (Demo)";
        private readonly string[] examplePages = new[]
        {
@"<html>
<body>
<article class=""job""><a href=""/job/101"">Senior Developer</a><span class=""company"">Acme Corp</span><span class=""location"">Prague</span><span class=""salary"">80 000</span></article>
<article class=""job""><a href=""/job/102"">Junior Developer</a><span class=""company"">Startup Ltd</span><span class=""location"">Brno</span><span class=""salary"">30 000</span></article>
<article class=""job""><a href=""/job/103"">Product Manager</a><span class=""company"">MegaRetail</span><span class=""location"">Ostrava</span><span class=""salary"">65 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/201"">Backend Engineer</a><span class=""company"">Acme Corp</span><span class=""location"">Remote</span><span class=""salary"">70k–90k</span></article>
<article class=""job""><a href=""/job/202"">QA Tester</a><span class=""company"">GoodSoft</span><span class=""location"">Prague</span><span class=""salary"">35 000</span></article>
<article class=""job""><a href=""/job/203"">Data Analyst</a><span class=""company"">FinSolve</span><span class=""location"">Brno</span><span class=""salary"">45 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/301"">DevOps Engineer</a><span class=""company"">Cloudy</span><span class=""location"">Prague</span><span class=""salary"">85 000</span></article>
<article class=""job""><a href=""/job/302"">Frontend Developer</a><span class=""company"">DevHouse</span><span class=""location"">Brno</span><span class=""salary"">55 000</span></article>
<article class=""job""><a href=""/job/303"">AI Researcher</a><span class=""company"">AIForge</span><span class=""location"">Remote</span><span class=""salary"">120 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/401"">Systems Administrator</a><span class=""company"">NorthTech</span><span class=""location"">Liberec</span><span class=""salary"">40 000</span></article>
<article class=""job""><a href=""/job/402"">Cloud Architect</a><span class=""company"">GreenEnergy</span><span class=""location"">Prague</span><span class=""salary"">110 000</span></article>
<article class=""job""><a href=""/job/403"">Business Analyst</a><span class=""company"">DataNinjas</span><span class=""location"">Brno</span><span class=""salary"">60 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/501"">Support Engineer</a><span class=""company"">HelpDesk Pro</span><span class=""location"">Plzeò</span><span class=""salary"">28 000</span></article>
<article class=""job""><a href=""/job/502"">Mobile Developer</a><span class=""company"">AppWorks</span><span class=""location"">Prague</span><span class=""salary"">70 000</span></article>
<article class=""job""><a href=""/job/503"">Fullstack Developer</a><span class=""company"">Acme Corp</span><span class=""location"">Brno</span><span class=""salary"">75 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/601"">Machine Learning Engineer</a><span class=""company"">AIForge</span><span class=""location"">Prague</span><span class=""salary"">130 000</span></article>
<article class=""job""><a href=""/job/602"">Security Specialist</a><span class=""company"">SecureIT</span><span class=""location"">Brno</span><span class=""salary"">95 000</span></article>
<article class=""job""><a href=""/job/603"">Cloud Support</a><span class=""company"">Cloudy</span><span class=""location"">Remote</span><span class=""salary"">50 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/701"">Intern - Software</a><span class=""company"">Startup Ltd</span><span class=""location"">Brno</span><span class=""salary"">10 000</span></article>
<article class=""job""><a href=""/job/702"">QA Lead</a><span class=""company"">GoodSoft</span><span class=""location"">Prague</span><span class=""salary"">65 000</span></article>
<article class=""job""><a href=""/job/703"">Site Reliability Engineer</a><span class=""company"">DevHouse</span><span class=""location"">Ostrava</span><span class=""salary"">100 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/801"">Retail Manager</a><span class=""company"">MegaRetail</span><span class=""location"">Prague</span><span class=""salary"">45 000</span></article>
<article class=""job""><a href=""/job/802"">Sales Executive</a><span class=""company"">FinSolve</span><span class=""location"">Brno</span><span class=""salary"">55 000</span></article>
<article class=""job""><a href=""/job/803"">Marketing Specialist</a><span class=""company"">Brandify</span><span class=""location"">Prague</span><span class=""salary"">48 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/901"">Business Developer</a><span class=""company"">NorthTech</span><span class=""location"">Ostrava</span><span class=""salary"">70 000</span></article>
<article class=""job""><a href=""/job/902"">Data Engineer</a><span class=""company"">DataNinjas</span><span class=""location"">Prague</span><span class=""salary"">85 000</span></article>
<article class=""job""><a href=""/job/903"">Technical Writer</a><span class=""company"">DocuWorks</span><span class=""location"">Brno</span><span class=""salary"">38 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/1001"">Embedded Engineer</a><span class=""company"">Vandy Systems</span><span class=""location"">Prague</span><span class=""salary"">55 000</span></article>
<article class=""job""><a href=""/job/1002"">Factory Technician</a><span class=""company"">Vandy Motors</span><span class=""location"">Liberec</span><span class=""salary"">32 000</span></article>
<article class=""job""><a href=""/job/1003"">Operations Manager</a><span class=""company"">VANDY</span><span class=""location"">Brno</span><span class=""salary"">60 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/1101"">Contractor</a><span class=""company"">ShadyWorks</span><span class=""location"">Remote</span><span class=""salary"">TBD</span></article>
<article class=""job""><a href=""/job/1102"">Freelance Dev</a><span class=""company"">SpamCorp</span><span class=""location"">Remote</span><span class=""salary"">Negotiable</span></article>
<article class=""job""><a href=""/job/1103"">Legacy Support</a><span class=""company"">VandyCorp</span><span class=""location"">Prague</span><span class=""salary"">29 000</span></article>
</body>
</html>",
@"<html>
<body>
<article class=""job""><a href=""/job/1201"">AI Product Owner</a><span class=""company"">AIForge</span><span class=""location"">Prague</span><span class=""salary"">140 000</span></article>
<article class=""job""><a href=""/job/1202"">Cloud Engineer</a><span class=""company"">Cloudy</span><span class=""location"">Brno</span><span class=""salary"">90 000</span></article>
<article class=""job""><a href=""/job/1203"">Support Intern</a><span class=""company"">VANDY Ltd</span><span class=""location"">Plzeò</span><span class=""salary"">9 000</span></article>
</body>
</html>"
        };

        public ExampleScraper()
        {
        }

        public async Task<List<JobOffer>> ScrapeUrlAsync(string startUrl, Action<string> logMessage)
        {
            var results = new List<JobOffer>();
            for (int i = 0; i < examplePages.Length; i++)
            {
                string page = examplePages[i];
                logMessage?.Invoke($"[ExampleScraper] Processing example page {i + 1} for URL: {startUrl}");
                await Task.Delay(300);
                var parsed = ExtractJobsFromHtml(page);
                results.AddRange(parsed);
                logMessage?.Invoke($"[ExampleScraper] Page {i + 1} returned {parsed.Count} offers.");
                await Task.Delay(200);
            }
            logMessage?.Invoke($"[ExampleScraper] Finished. Total offers: {results.Count}");
            return results;
        }

        private List<JobOffer> ExtractJobsFromHtml(string html)
        {
            var list = new List<JobOffer>();
            var matches = Regex.Matches(html, @"<article[^>]*class=""job""[^>]*>.*?</article>", RegexOptions.Singleline);
            foreach (Match m in matches)
            {
                var fragment = m.Value;
                var titleMatch = Regex.Match(fragment, @"<a[^>]*href=""([^""]+)""[^>]*>([^<]+)</a>");
                var companyMatch = Regex.Match(fragment, @"<span[^>]*class=""company""[^>]*>([^<]+)</span>");
                var locationMatch = Regex.Match(fragment, @"<span[^>]*class=""location""[^>]*>([^<]+)</span>");
                var salaryMatch = Regex.Match(fragment, @"<span[^>]*class=""salary""[^>]*>([^<]+)</span>");

                var offer = new JobOffer
                {
                    Title = titleMatch.Success ? titleMatch.Groups[2].Value.Trim() : string.Empty,
                    Url = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : string.Empty,
                    Company = companyMatch.Success ? companyMatch.Groups[1].Value.Trim() : string.Empty,
                    Location = locationMatch.Success ? locationMatch.Groups[1].Value.Trim() : string.Empty,
                    Salary = salaryMatch.Success ? salaryMatch.Groups[1].Value.Trim() : string.Empty,
                    DateScraped = DateTime.Now,
                    LastSeen = DateTime.Now,
                    Status = 0
                };

                if (!string.IsNullOrEmpty(offer.Title))
                {
                    list.Add(offer);
                }
            }

            return list;
        }
    }
}