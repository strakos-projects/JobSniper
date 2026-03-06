using JobSniper.Models;
using JobSniper.Scrapers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JobSniper
{
    public partial class MainWindow : Window
    {
        private int? _currentFilterStatus = null;
        private bool _isShowingDuplicates = false;
        public ObservableCollection<JobOffer> DatabaseOfJobs { get; set; } = new ObservableCollection<JobOffer>();
        public ObservableCollection<JobOffer> SessionDuplicates { get; set; } = new ObservableCollection<JobOffer>();
        public ObservableCollection<CompanyProfile> CrmProfiles { get; set; } = new ObservableCollection<CompanyProfile>();


        private Dictionary<string, Type> _availableScrapers = new Dictionary<string, Type>();
        // Paměť pro rychlé hledání reputace
        private Dictionary<string, int> _companyReputationCache = new Dictionary<string, int>();

        private readonly string urlsFilePath = "urls.json";
        private readonly string jobsFilePath = "jobs.json";
        private readonly string blacklistFilePath = "blacklist.json";

        private List<ScrapeUrl> savedUrls = new List<ScrapeUrl>();
        private List<string> blacklistedCompanies = new List<string>();
        private readonly string crmFilePath = "crm_companies.json";
        private List<CompanyProfile> crmProfiles = new List<CompanyProfile>();
        public MainWindow()
        {
            InitializeComponent();
            var scraperTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IScraper).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            foreach (var type in scraperTypes)
            {
                var instance = (IScraper)Activator.CreateInstance(type);
                _availableScrapers[instance.Name] = type;
            }

            CmbScrapers.ItemsSource = _availableScrapers.Keys.ToList();
            if (CmbScrapers.Items.Count > 0) CmbScrapers.SelectedIndex = 0;

            ShowDashboard();

            // PŘIDÁNO: Načítání spustíme až ve chvíli, kdy už je okno fyzicky vidět na obrazovce
            this.Loaded += MainWindow_Loaded;
        }
        // 1. Zajišťuje, že Cache je neustále aktuální
        private void RefreshCrmCache()
        {
            _companyReputationCache.Clear();
            foreach (var profile in CrmProfiles)
            {
                foreach (var alias in profile.Aliases)
                {
                    string normAlias = NormalizeCompanyName(alias);
                    if (!string.IsNullOrEmpty(normAlias) && !_companyReputationCache.ContainsKey(normAlias))
                    {
                        _companyReputationCache[normAlias] = profile.Reputation;
                    }
                }
            }
        }

        // 2. Bleskové načítání na pozadí, které nezamrazí okno
        // 2. Bleskové načítání na pozadí, které nezamrazí okno
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GridDashboard.IsEnabled = false; // Zakážeme na vteřinu klikání
            LogToConsole("Začínám načítat databázi na pozadí...");

            // Třída Progress automaticky zajistí bezpečný přepis textu v UI vlákně z vlákna na pozadí
            var progress = new Progress<string>(status =>
            {
                Title = $"JobSniper - {status}";
            });

            // Dočasné lokální seznamy (aby UI vlákno nebylo rušeno)
            var tempUrls = new List<ScrapeUrl>();
            var tempBlacklist = new List<string>();
            var tempCrm = new List<CompanyProfile>();
            var tempJobs = new List<JobOffer>();

            // VŠECHNA PRÁCE SE TEĎ DĚJE NA POZADÍ (UI je plně responzivní)
            await Task.Run(() =>
            {
                var p = (IProgress<string>)progress;

                p.Report("Načítám nastavení a blacklist...");
                if (File.Exists(urlsFilePath)) tempUrls = JsonSerializer.Deserialize<List<ScrapeUrl>>(File.ReadAllText(urlsFilePath)) ?? new List<ScrapeUrl>();
                if (File.Exists(blacklistFilePath)) tempBlacklist = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(blacklistFilePath)) ?? new List<string>();

                p.Report("Načítám CRM databázi...");
                if (File.Exists(crmFilePath)) tempCrm = JsonSerializer.Deserialize<List<CompanyProfile>>(File.ReadAllText(crmFilePath)) ?? new List<CompanyProfile>();

                p.Report("Optimalizuji paměť...");
                _companyReputationCache.Clear();
                foreach (var profile in tempCrm)
                {
                    foreach (var alias in profile.Aliases)
                    {
                        string normAlias = NormalizeCompanyName(alias);
                        if (!string.IsNullOrEmpty(normAlias) && !_companyReputationCache.ContainsKey(normAlias))
                            _companyReputationCache[normAlias] = profile.Reputation;
                    }
                }

                if (File.Exists(jobsFilePath))
                {
                    p.Report("Čtu inzeráty z disku...");
                    tempJobs = JsonSerializer.Deserialize<List<JobOffer>>(File.ReadAllText(jobsFilePath)) ?? new List<JobOffer>();

                    int total = tempJobs.Count;
                    int current = 0;

                    foreach (var j in tempJobs)
                    {
                        current++;
                        // Abychom nezahltili UI vykreslováním, hlásíme progres jen každých 50 inzerátů nebo na konci
                        if (current % 50 == 0 || current == total)
                        {
                            p.Report($"Zpracovávám inzeráty: {current} z {total}");
                        }

                        string normCompany = NormalizeCompanyName(j.Company);
                        // Bleskové přiřazení z Cache
                        if (_companyReputationCache.TryGetValue(normCompany, out int rep))
                            j.CrmReputation = rep;
                        else
                        {
                            var profile = tempCrm.FirstOrDefault(pr => pr.Aliases.Any(a => IsCompanyMatch(a, j.Company)));
                            j.CrmReputation = profile != null ? profile.Reputation : 0;
                        }
                    }
                }
            });

            // 3. Po dokončení bezpečně nahrajeme data zpět do UI
            savedUrls = tempUrls;
            blacklistedCompanies = tempBlacklist;
            CrmProfiles = new ObservableCollection<CompanyProfile>(tempCrm);
            DatabaseOfJobs = new ObservableCollection<JobOffer>(tempJobs);

            RefreshUrlList();
            DataGridJobs.ItemsSource = DatabaseOfJobs;

            UpdateDashboardCounters();

            // Vrátíme UI do normálu
            GridDashboard.IsEnabled = true;
            Title = "JobSniper - Můj kariérní zen";
            LogToConsole("Systém připraven a data načtena...");

            _ = StartScrapingEngineAsync();
        }
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
        // 1. Normalizační metoda - Očistí název firmy od balastu
        private string NormalizeCompanyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";

            // Převod na malá písmena
            string normalized = name.ToLowerInvariant();
            normalized = RemoveDiacritics(normalized);
            normalized = normalized.Replace(".", "");
            // Nahrazení interpunkce (tečky, čárky, pomlčky, svislítka) za mezeru
            normalized = Regex.Replace(normalized, @"[.,\-\|]", " ");

            // Odstranění běžných právních forem (hledáme pouze celá slova, viz \b)
            string[] formsToRemove = { "s r o", "sro", "spol s r o", "spol", "a s", "as", "z s", "o p s", "z u", "inc", "llc", "corp", "corporation", "ltd", "limited", "gmbh", "sp z o o" };
            foreach (var form in formsToRemove)
            {
                normalized = Regex.Replace(normalized, $@"\b{form}\b", " ");
            }

            // Odstranění vícenásobných mezer a oříznutí krajů
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        // 2. Chytré porovnávání s využitím normalizace
        private bool IsCompanyMatch(string alias, string jobCompany)
        {
            if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(jobCompany)) return false;

            string normAlias = NormalizeCompanyName(alias);
            string normJobCompany = NormalizeCompanyName(jobCompany);

            if (string.IsNullOrEmpty(normAlias) || string.IsNullOrEmpty(normJobCompany)) return false;

            // A) Přesná shoda po očištění (např. "rockwell automation" == "rockwell automation")
            if (normAlias == normJobCompany) return true;

            // B) Částečná shoda - obalíme mezerami, aby např. "auto" nesouhlasilo s "automotive"
            if ($" {normJobCompany} ".Contains($" {normAlias} ")) return true;

            return false;
        }
        private void UpdateDashboardCounters()
        {
            int inbox = DatabaseOfJobs.Count(j => j.Status == 0);
            int opportunities = DatabaseOfJobs.Count(j => j.Status == 1);
            int trashedManual = DatabaseOfJobs.Count(j => j.Status == 2);
            int trashedAuto = DatabaseOfJobs.Count(j => j.Status == 3);
            int archive = DatabaseOfJobs.Count(j => j.Status == 4);

            TxtCekaVTridicce.Text = inbox.ToString();
            TxtMojePrilezitosti.Text = opportunities.ToString();
            TxtZahozeno.Text = $"{trashedAuto} / {trashedManual}";

            // Zde používáme C# zdroje s funkcí string.Format pro doplnění čísel do závorek
            BtnTridicka.Content = string.Format(Properties.Resources.Menu_Inbox_Format, inbox);
            BtnPrilezitosti.Content = string.Format(Properties.Resources.Menu_Opportunities_Format, opportunities);
            BtnKos.Content = string.Format(Properties.Resources.Menu_Trash_Format, trashedManual + trashedAuto);
            BtnDuplicity.Content = string.Format(Properties.Resources.Menu_Duplicates_Format, SessionDuplicates.Count);

            if (BtnArchiv != null)
                BtnArchiv.Content = string.Format(Properties.Resources.Menu_Archive_Format, archive);
        }

   
        private void SetView(Grid visibleGrid, Button activeButton, string title = "", int? filterStatus = null, bool showDuplicates = false)
        {
            GridDashboard.Visibility = Visibility.Collapsed;
            GridTridicka.Visibility = Visibility.Collapsed;
            GridSettings.Visibility = Visibility.Collapsed;
            GridCrm.Visibility = Visibility.Collapsed;

            visibleGrid.Visibility = Visibility.Visible;

            if (!string.IsNullOrEmpty(title)) TxtTridickaTitle.Text = title;

            BtnDashboard.Background = Brushes.Transparent;
            BtnTridicka.Background = Brushes.Transparent;
            BtnPrilezitosti.Background = Brushes.Transparent;
            BtnKos.Background = Brushes.Transparent;
            BtnDuplicity.Background = Brushes.Transparent;
            BtnSettings.Background = Brushes.Transparent;
            BtnCrmTab.Background = Brushes.Transparent;

            activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34495E"));


            _currentFilterStatus = filterStatus;
            _isShowingDuplicates = showDuplicates;

            if (TxtSearch != null) TxtSearch.Text = "";
            if (TxtCrmSearch != null) TxtCrmSearch.Text = "";

            ApplyFilters();
        }

        
        private void ApplyFilters()
        {
            var targetCollection = _isShowingDuplicates ? SessionDuplicates : DatabaseOfJobs;
            DataGridJobs.ItemsSource = targetCollection;

            ICollectionView view = CollectionViewSource.GetDefaultView(targetCollection);

            view.Filter = (item) => {
                var job = item as JobOffer;
                if (job == null) return false;

        
                bool statusMatch = true;
                if (!_isShowingDuplicates && _currentFilterStatus.HasValue)
                {
                    if (_currentFilterStatus == 2) statusMatch = (job.Status == 2 || job.Status == 3);
                    else statusMatch = (job.Status == _currentFilterStatus);
                }
                if (!statusMatch) return false;

        
                string query = TxtSearch.Text?.ToLower().Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    bool matchTitle = job.Title != null && job.Title.ToLower().Contains(query);
                    bool matchCompany = job.Company != null && job.Company.ToLower().Contains(query);
                    return matchTitle || matchCompany;
                }

                return true;
            };

            view.Refresh();
        }

        
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataGridJobs?.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(DataGridJobs.ItemsSource).Refresh();
            }
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();
        private void ShowDashboard() => SetView(GridDashboard, BtnDashboard);

        private void BtnTridicka_Click(object sender, RoutedEventArgs e) => SetView(GridTridicka, BtnTridicka, "📥 Inbox", 0);
        private void BtnPrilezitosti_Click(object sender, RoutedEventArgs e) => SetView(GridTridicka, BtnPrilezitosti, "⭐ Opportunities", 1);
        private void BtnKos_Click(object sender, RoutedEventArgs e) => SetView(GridTridicka, BtnKos, "🗑️ Filtered to Trash", 2);
        private void BtnDuplicity_Click(object sender, RoutedEventArgs e) => SetView(GridTridicka, BtnDuplicity, "🔁 Session Duplicates", null, true);
        private void BtnSettings_Click(object sender, RoutedEventArgs e) => SetView(GridSettings, BtnSettings);

        private async Task StartScrapingEngineAsync()
        {
            LogToConsole("Starting scraper engine...");
           // var scraper = new ExampleScraper();

            foreach (var url in savedUrls)
            {
                if (!url.IsActive) continue;
                /*IScraper scraper = url.PortalName switch
                {
                    "ExampleScraper (Demo)" => new ExampleScraper(),
                    "Jobs.cz (Ostrý)" => new JobsCzScraper(),
                    _ => new ExampleScraper() // Výchozí pojistka pro staré adresy z předchozí verze
                };*/
                IScraper scraper;
                if (_availableScrapers.ContainsKey(url.PortalName))
                {
                 
                    scraper = (IScraper)Activator.CreateInstance(_availableScrapers[url.PortalName]);
                }
                else
                {
                    scraper = (IScraper)Activator.CreateInstance(_availableScrapers.Values.First());
                    LogToConsole($"[Varování] Scraper '{url.PortalName}' nebyl nalezen. Používám záložní {scraper.Name}.");
                }

                LogToConsole($"[Engine] Using {scraper.Name} for {url.Url}");
                List<JobOffer> newJobs = await scraper.ScrapeUrlAsync(url.Url, LogToConsole);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    int addedCount = 0;
                    int dupCount = 0;

                    foreach (var job in newJobs)
                    {
                        job.PortalName = scraper.Name;
                        string cleanUrl = job.Url;
                        int qMarkIndex = cleanUrl.IndexOf('?');
                        if (qMarkIndex > 0) cleanUrl = cleanUrl.Substring(0, qMarkIndex);

                        var existingJob = DatabaseOfJobs.FirstOrDefault(j =>
                            (j.Url != null && j.Url.StartsWith(cleanUrl)) ||
                            (j.Title == job.Title && j.Company == job.Company)
                        );

                        if (existingJob != null)
                        {
                            existingJob.LastSeen = DateTime.Now;
                            if (existingJob.Status == 4)
                            {
                                existingJob.Status = 0;
                            }
                            job.Url = cleanUrl;
                            job.CrmReputation = GetCompanyReputation(job.Company);
                            SessionDuplicates.Add(job);
                            dupCount++;
                        }
                        else
                        {
                            job.Url = cleanUrl;
                            if (blacklistedCompanies.Contains(job.Company)) job.Status = 3;
                            job.CrmReputation = GetCompanyReputation(job.Company);
                            DatabaseOfJobs.Add(job);
                            addedCount++;
                        }
                    }
                    UpdateDashboardCounters();
                    SaveJobs();

                    if (addedCount > 0 || dupCount > 0)
                        LogToConsole($"[System] Saved {addedCount} new offers. Ignored {dupCount} duplicates.");
                });

                await Task.Delay(3000);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                ArchiveExpiredJobs();
            });

            LogToConsole("All URLs processed. Cycle finished.");
        }

        private void ArchiveExpiredJobs()
        {
            int archivedCount = 0;

            foreach (var job in DatabaseOfJobs.Where(j => j.Status == 0))
            {
                if ((DateTime.Now - job.LastSeen).TotalDays > 2)
                {
                    job.Status = 4;
                    archivedCount++;
                }
            }

            if (archivedCount > 0)
            {
                SaveJobs();
                UpdateDashboardCounters();
                LogToConsole($"[Maintenance] {archivedCount} old offers expired from Inbox and were moved to Archive.");
            }
        }
        private void BtnCrm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is JobOffer job)
            {
                if (SessionDuplicates.Contains(job))
                {
                    MessageBox.Show("Toto je pouze dočasný záznam relace. Otevřete CRM z Třídičky nebo Příležitostí.", "Informace", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var profile = CrmProfiles.FirstOrDefault(p =>
                    //p.Aliases.Any(a => string.Equals(a, job.Company, StringComparison.OrdinalIgnoreCase))
                    p.Aliases.Any(a => IsCompanyMatch(a, job.Company))
                );

                bool isNewProfile = false;

                if (profile == null)
                {
                    profile = new CompanyProfile();
                    profile.Aliases.Add(job.Company);
                    isNewProfile = true;
                }

                bool isBlacklisted = profile.Aliases.Any(a => blacklistedCompanies.Any(b => IsCompanyMatch(b, a) /*string.Equals(b, a, StringComparison.OrdinalIgnoreCase)*/)) ||
                                     blacklistedCompanies.Any(b => IsCompanyMatch(b, job.Company)/*tring.Equals(b, job.Company, StringComparison.OrdinalIgnoreCase)*/);

                var crmWindow = new CrmWindow(profile, job.Company, isBlacklisted) { Owner = this };

                if (crmWindow.ShowDialog() == true)
                {
                    if (isNewProfile)
                    {
                        CrmProfiles.Add(profile);
                    }

                    SaveCrm();
                    RefreshCrmCache();

                    int affectedCount = 0;
                    foreach (var j in DatabaseOfJobs)
                    {
                        if (profile.Aliases.Any(a => /*string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase)*/ IsCompanyMatch(a, j.Company)) ||
                            IsCompanyMatch(j.Company, job.Company)/*string.Equals(j.Company, job.Company, StringComparison.OrdinalIgnoreCase)*/)
                        {
                            j.CrmReputation = profile.Reputation;
                            affectedCount++;
                        }
                    }

                    CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                    SaveJobs();
                    HandleBlacklistChangeFromCrm(profile, crmWindow.IsBlacklisted);
                    LogToConsole($"[CRM] Profil firmy '{job.Company}' byl aktualizován. Změna se projevila u {affectedCount} inzerátů.");
                }
            }
        }
        private void BtnOpenWeb_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        MessageBox.Show("Tento inzerát neobsahuje žádný odkaz.", "Chybějící odkaz", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                    {
                        if (url.StartsWith("/"))
                        {
                            url = "https://example.com" + url;
                        }
                        else
                        {
                            url = "https://" + url;
                        }
                    }

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Odkaz se nepodařilo otevřít.\nURL: {url}\n\nChyba: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        private void BtnZajimave_Click(object sender, RoutedEventArgs e) => UpdateStatus(sender, 1);
        private void BtnSkryt_Click(object sender, RoutedEventArgs e) => UpdateStatus(sender, 2);

        private void UpdateStatus(object sender, int newStatus)
        {
            if (sender is Button btn && btn.Tag is JobOffer job)
            {
                if (SessionDuplicates.Contains(job))
                {
                    MessageBox.Show("This offer is a temporary session duplicate; state change is not allowed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                job.Status = newStatus;
                SaveJobs();
                CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                UpdateDashboardCounters();
            }
        }

        private void BtnBlokovatFirmu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is JobOffer clickedJob)
            {
                if (SessionDuplicates.Contains(clickedJob)) return;

                string company = clickedJob.Company;
                if (!blacklistedCompanies.Contains(company))
                {
                    blacklistedCompanies.Add(company);
                    SaveBlacklist();
                }

                int affected = 0;
                foreach (var job in DatabaseOfJobs)
                {
                    if (job.Company == company && (job.Status == 0 || job.Status == 1))
                    {
                        job.Status = 3;
                        affected++;
                    }
                }

                SaveJobs();
                CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                UpdateDashboardCounters();
                LogToConsole($"[Blacklist] Company '{company}' blocked. Cleaned {affected} offers.");
            }
        }

        private void BtnOdblokovatFirmu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is JobOffer clickedJob)
            {
                if (SessionDuplicates.Contains(clickedJob)) return;

                string company = clickedJob.Company;
                if (blacklistedCompanies.Contains(company))
                {
                    blacklistedCompanies.Remove(company);
                    SaveBlacklist();
                }

                int restored = 0;
                foreach (var job in DatabaseOfJobs)
                {
                    if (job.Company == company && job.Status == 3)
                    {
                        job.Status = 0;
                        restored++;
                    }
                }

                SaveJobs();
                CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                UpdateDashboardCounters();
                LogToConsole($"[Blacklist] Company '{company}' unblocked. Returned {restored} offers to Inbox.");
            }
        }

        private void LoadCrm()
        {
            if (File.Exists(crmFilePath))
            {
                var loaded = JsonSerializer.Deserialize<List<CompanyProfile>>(File.ReadAllText(crmFilePath)) ?? new List<CompanyProfile>();
                CrmProfiles = new ObservableCollection<CompanyProfile>(loaded);
                // ZRYCHLENÍ: Předpočítáme si normalizované aliasy a jejich reputaci
                _companyReputationCache.Clear();
                foreach (var profile in CrmProfiles)
                {
                    foreach (var alias in profile.Aliases)
                    {
                        string normAlias = NormalizeCompanyName(alias);
                        if (!string.IsNullOrEmpty(normAlias) && !_companyReputationCache.ContainsKey(normAlias))
                        {
                            _companyReputationCache[normAlias] = profile.Reputation;
                        }
                    }
                }
            }
        }
        private void SaveCrm() => File.WriteAllText(crmFilePath, JsonSerializer.Serialize(CrmProfiles, new JsonSerializerOptions { WriteIndented = true }));

        // Pomocná funkce, která zjistí barvu firmy z CRM (hledá i v Aliasech)
        private int GetCompanyReputation(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName)) return 0;

            string normCompany = NormalizeCompanyName(companyName);

            // 1. Zkusíme bleskové hledání v naší předpočítané paměti (přesná shoda očištěného názvu)
            if (_companyReputationCache.TryGetValue(normCompany, out int rep))
            {
                return rep;
            }
            var profile = CrmProfiles.FirstOrDefault(p =>
                // p.Aliases.Any(a => string.Equals(a, companyName, StringComparison.OrdinalIgnoreCase))
                p.Aliases.Any(a => IsCompanyMatch(a, companyName))

            );
            return profile != null ? profile.Reputation : 0;
        }
        private void LoadJobs()
        {
            if (File.Exists(jobsFilePath))
            {
                var loaded = JsonSerializer.Deserialize<List<JobOffer>>(File.ReadAllText(jobsFilePath)) ?? new List<JobOffer>();

                var tempJobs = new List<JobOffer>();
                foreach (var j in loaded)
                {
                    // Přidáno: zkontrolujeme aktuální reputaci v CRM
                    j.CrmReputation = GetCompanyReputation(j.Company);
                    // DatabaseOfJobs.Add(j);
                    tempJobs.Add(j);
                }
                DatabaseOfJobs = new ObservableCollection<JobOffer>(tempJobs);
            }
            else
            {
                DatabaseOfJobs = new ObservableCollection<JobOffer>();
            }
            DataGridJobs.ItemsSource = DatabaseOfJobs;
        }
        private void SaveJobs() => File.WriteAllText(jobsFilePath, JsonSerializer.Serialize(DatabaseOfJobs, new JsonSerializerOptions { WriteIndented = true }));

        private void LoadBlacklist()
        {
            if (File.Exists(blacklistFilePath)) blacklistedCompanies = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(blacklistFilePath)) ?? new List<string>();
        }
        private void SaveBlacklist() => File.WriteAllText(blacklistFilePath, JsonSerializer.Serialize(blacklistedCompanies, new JsonSerializerOptions { WriteIndented = true }));

        private void LoadUrls()
        {
            if (File.Exists(urlsFilePath)) savedUrls = JsonSerializer.Deserialize<List<ScrapeUrl>>(File.ReadAllText(urlsFilePath)) ?? new List<ScrapeUrl>();
            RefreshUrlList();
        }
        private void SaveUrls() => File.WriteAllText(urlsFilePath, JsonSerializer.Serialize(savedUrls, new JsonSerializerOptions { WriteIndented = true }));
        private void RefreshUrlList() { lstUrls.ItemsSource = null; lstUrls.ItemsSource = savedUrls; }

        private void BtnAddUrl_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNewUrl.Text)) return;

            string selectedScraper = CmbScrapers.SelectedItem as string ?? "ExampleScraper (Demo)";

            savedUrls.Add(new ScrapeUrl
            {
                Url = txtNewUrl.Text,
                PortalName = selectedScraper, // Zde ukládáme zvolený scraper!
                IsActive = true
            });

            SaveUrls();
            RefreshUrlList();
            txtNewUrl.Text = "";
        }
        private void BtnArchiv_Click(object sender, RoutedEventArgs e) => SetView(GridTridicka, BtnArchiv, "🗄️ Archive (Inactive offers)", 4);
        private void LogToConsole(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TxtConsoleLog.AppendText($"\n[{DateTime.Now:HH:mm:ss}] {message}");
                TxtConsoleLog.ScrollToEnd();
            });
        }
        // Otevření záložky CRM
        private void BtnCrmTab_Click(object sender, RoutedEventArgs e)
        {
            SetView(GridCrm, BtnCrmTab, "🏢 CRM - Databáze firem", null, false);
            DataGridCrm.ItemsSource = CrmProfiles; // Připojí data do tabulky
        }

        // Tlačítko: Přidat novou firmu (Ručně)
        private void BtnAddNewCompany_Click(object sender, RoutedEventArgs e)
        {
            var newProfile = new CompanyProfile();
            var crmWindow = new CrmWindow(newProfile, Properties.Resources.Crm_NewCompany, false) { Owner = this };

            if (crmWindow.ShowDialog() == true && newProfile.Aliases.Count > 0)
            {
                foreach (var newAlias in newProfile.Aliases)
                {
                    string normNewAlias = NormalizeCompanyName(newAlias);

                    // ... a zkusíme je najít v existujících CRM profilech
                    var existingProfile = CrmProfiles.FirstOrDefault(p => p.Aliases.Any(a => NormalizeCompanyName(a) == normNewAlias));

                    if (existingProfile != null)
                    {
                        MessageBox.Show(
                                string.Format(Properties.Resources.Msg_CompanyExistsText, newAlias, existingProfile.PrimaryName),
                                Properties.Resources.Msg_CompanyExistsTitle,
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                        return; // Okamžitě ukončíme metodu, firma se NEpřidá
                    }
                }

                CrmProfiles.Add(newProfile);
                SaveCrm();
                RefreshCrmCache();

                // Přepočítá barvy u inzerátů, kdyby od ní už v Třídičce nějaký byl
                foreach (var job in DatabaseOfJobs.Where(j => newProfile.Aliases.Any(a => IsCompanyMatch(a, j.Company)/*string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase)*/)))
                    job.CrmReputation = newProfile.Reputation;

                CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                SaveJobs();
                HandleBlacklistChangeFromCrm(newProfile, crmWindow.IsBlacklisted);
            }
        }

        // Tlačítko: Upravit firmu (V tabulce CRM)
        private void BtnEditCrm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CompanyProfile profile)
            {
                bool isBlacklisted = profile.Aliases.Any(a => blacklistedCompanies.Any(b => IsCompanyMatch(b, a)/* string.Equals(b, a, StringComparison.OrdinalIgnoreCase)*/));
                var crmWindow = new CrmWindow(profile, profile.PrimaryName, isBlacklisted) { Owner = this };
                if (crmWindow.ShowDialog() == true)
                {
                    SaveCrm();
                    RefreshCrmCache();
                    CollectionViewSource.GetDefaultView(CrmProfiles).Refresh(); // Aktualizuje tabulku CRM

                    
                    foreach (var job in DatabaseOfJobs.Where(j => profile.Aliases.Any(a => IsCompanyMatch(a, j.Company)/* string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase)*/)))
                        job.CrmReputation = profile.Reputation;

                    CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                    SaveJobs();
                    HandleBlacklistChangeFromCrm(profile, crmWindow.IsBlacklisted);
                }
            }
        }
        private void HandleBlacklistChangeFromCrm(CompanyProfile profile, bool shouldBeBlacklisted)
        {
            bool changed = false;

            if (shouldBeBlacklisted)
            {
                foreach (var alias in profile.Aliases)
                {
                    if (!blacklistedCompanies.Contains(alias))
                    {
                        blacklistedCompanies.Add(alias);
                        changed = true;
                    }
                }
                if (changed)
                {
                    foreach (var job in DatabaseOfJobs.Where(j => profile.Aliases.Any(a => IsCompanyMatch(a, j.Company) /*string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase)*/)))
                        if (job.Status == 0 || job.Status == 1) job.Status = 3;
                }
            }
            else
            {
                foreach (var alias in profile.Aliases)
                {
                    if (blacklistedCompanies.Contains(alias))
                    {
                        blacklistedCompanies.Remove(alias);
                        changed = true;
                    }
                }
                if (changed)
                {
                    foreach (var job in DatabaseOfJobs.Where(j => profile.Aliases.Any(a => IsCompanyMatch(a, j.Company) /*string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase)*/)))
                        if (job.Status == 3) job.Status = 0;
                }
            }

            if (changed)
            {
                SaveBlacklist();
                SaveJobs();
                CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                UpdateDashboardCounters();
            }
        }
        // Tlačítko: Smazat URL z Nastavení
        private void BtnDeleteUrl_Click(object sender, RoutedEventArgs e)
        {
            // Získáme konkrétní URL objekt z Tagu tlačítka
            if (sender is Button btn && btn.Tag is ScrapeUrl urlItem)
            {
                var result = MessageBox.Show($"Opravdu chcete odebrat sledování této URL?\n{urlItem.Url}", "Odebrat URL", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    savedUrls.Remove(urlItem); // Odstraníme ze seznamu
                    SaveUrls();                // Uložíme změny do urls.json
                    RefreshUrlList();          // Obnovíme zobrazení v ListBoxu
                }
            }
        }
        // Tlačítko: Smazat firmu (V tabulce CRM)
        private void BtnDeleteCrm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CompanyProfile profile)
            {
                var result = MessageBox.Show($"Opravdu chcete z CRM trvale smazat záznam firmy '{profile.PrimaryName}'?", "Potvrdit smazání", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    CrmProfiles.Remove(profile);
                    SaveCrm();
                    RefreshCrmCache();

                    // Inzerátům této firmy resetuje barvu na výchozí
                    foreach (var job in DatabaseOfJobs.Where(j => profile.Aliases.Any(a => IsCompanyMatch(a, j.Company) /*string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase)*/)))
                        job.CrmReputation = 0;

                    CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                    SaveJobs();
                }
            }
        }
        private void TxtCrmSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataGridCrm?.ItemsSource != null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(DataGridCrm.ItemsSource);
                string query = TxtCrmSearch.Text?.ToLower().Trim();

                if (string.IsNullOrEmpty(query))
                {
                    view.Filter = null; // Pokud je pole prázdné, zrušíme filtr
                }
                else
                {
                    view.Filter = (item) =>
                    {
                        var profile = item as CompanyProfile;
                        if (profile == null) return false;

                        // Hledáme v primárním názvu
                        bool matchName = profile.PrimaryName != null && profile.PrimaryName.ToLower().Contains(query);

                        // Hledáme ve všech aliasech
                        bool matchAliases = profile.Aliases != null && profile.Aliases.Any(a => a.ToLower().Contains(query));

                        // Hledáme v historii interakcí (zde jsou např. ta telefonní čísla)
                        bool matchHistory = profile.InteractionHistory != null && profile.InteractionHistory.ToLower().Contains(query);

                        return matchName || matchAliases || matchHistory;
                    };
                }
                view.Refresh();
            }
        }
    }

}