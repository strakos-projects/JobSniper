using JobSniper.Models;
using JobSniper.Scrapers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JobSniper
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<JobOffer> DatabaseOfJobs { get; set; } = new ObservableCollection<JobOffer>();
        public ObservableCollection<JobOffer> SessionDuplicates { get; set; } = new ObservableCollection<JobOffer>();
        public ObservableCollection<CompanyProfile> CrmProfiles { get; set; } = new ObservableCollection<CompanyProfile>();
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
            DataGridJobs.ItemsSource = DatabaseOfJobs;

            LoadUrls();
            LoadBlacklist();
            LoadCrm();
            LoadJobs();

            UpdateDashboardCounters();
            ShowDashboard();
            LogToConsole("System ready and data loaded...");

            _ = StartScrapingEngineAsync();
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
        
            GridCrm.Visibility = Visibility.Collapsed;

        
            if (BtnCrmTab != null) BtnCrmTab.Background = Brushes.Transparent;
            GridDashboard.Visibility = Visibility.Collapsed;
            GridTridicka.Visibility = Visibility.Collapsed;
            GridSettings.Visibility = Visibility.Collapsed;
            visibleGrid.Visibility = Visibility.Visible;

            if (!string.IsNullOrEmpty(title)) TxtTridickaTitle.Text = title;

            BtnDashboard.Background = Brushes.Transparent;
            BtnTridicka.Background = Brushes.Transparent;
            BtnPrilezitosti.Background = Brushes.Transparent;
            BtnKos.Background = Brushes.Transparent;
            BtnDuplicity.Background = Brushes.Transparent;
            BtnSettings.Background = Brushes.Transparent;

            activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34495E"));

            if (showDuplicates)
            {
                DataGridJobs.ItemsSource = SessionDuplicates;
                ICollectionView view = CollectionViewSource.GetDefaultView(SessionDuplicates);
                view.Filter = null;
                view.Refresh();
            }
            else
            {
                DataGridJobs.ItemsSource = DatabaseOfJobs;
                ICollectionView view = CollectionViewSource.GetDefaultView(DatabaseOfJobs);
                if (filterStatus.HasValue)
                {
                    view.Filter = (item) =>
                    {
                        var job = item as JobOffer;
                        if (filterStatus == 2) return job.Status == 2 || job.Status == 3;
                        return job.Status == filterStatus;
                    };
                }
                else view.Filter = null;
                view.Refresh();
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
            var scraper = new ExampleScraper();

            foreach (var url in savedUrls)
            {
                if (!url.IsActive) continue;
                List<JobOffer> newJobs = await scraper.ScrapeUrlAsync(url.Url, LogToConsole);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    int addedCount = 0;
                    int dupCount = 0;

                    foreach (var job in newJobs)
                    {
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
                    p.Aliases.Any(a => string.Equals(a, job.Company, StringComparison.OrdinalIgnoreCase))
                );

                bool isNewProfile = false;

                if (profile == null)
                {
                    profile = new CompanyProfile();
                    profile.Aliases.Add(job.Company);
                    isNewProfile = true;
                }

                bool isBlacklisted = profile.Aliases.Any(a => blacklistedCompanies.Any(b => string.Equals(b, a, StringComparison.OrdinalIgnoreCase))) ||
                                     blacklistedCompanies.Any(b => string.Equals(b, job.Company, StringComparison.OrdinalIgnoreCase));

                var crmWindow = new CrmWindow(profile, job.Company, isBlacklisted) { Owner = this };

                if (crmWindow.ShowDialog() == true)
                {
                    if (isNewProfile)
                    {
                        CrmProfiles.Add(profile);
                    }

                    SaveCrm();

                    int affectedCount = 0;
                    foreach (var j in DatabaseOfJobs)
                    {
                        if (profile.Aliases.Any(a => string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase)) ||
                            string.Equals(j.Company, job.Company, StringComparison.OrdinalIgnoreCase))
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
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
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
            }
        }
        private void SaveCrm() => File.WriteAllText(crmFilePath, JsonSerializer.Serialize(CrmProfiles, new JsonSerializerOptions { WriteIndented = true }));

        // Pomocná funkce, která zjistí barvu firmy z CRM (hledá i v Aliasech)
        private int GetCompanyReputation(string companyName)
        {
            var profile = CrmProfiles.FirstOrDefault(p =>
                p.Aliases.Any(a => string.Equals(a, companyName, StringComparison.OrdinalIgnoreCase))
            );
            return profile != null ? profile.Reputation : 0;
        }
        private void LoadJobs()
        {
            if (File.Exists(jobsFilePath))
            {
                var loaded = JsonSerializer.Deserialize<List<JobOffer>>(File.ReadAllText(jobsFilePath)) ?? new List<JobOffer>();
                foreach (var j in loaded)
                {
                    // Přidáno: zkontrolujeme aktuální reputaci v CRM
                    j.CrmReputation = GetCompanyReputation(j.Company);
                    DatabaseOfJobs.Add(j);
                }
            }
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
            savedUrls.Add(new ScrapeUrl { Url = txtNewUrl.Text, PortalName = txtNewUrl.Text.Contains("jobs.cz") ? "jobs.cz" : "Unknown", IsActive = true });
            SaveUrls(); RefreshUrlList(); txtNewUrl.Text = "";
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
                CrmProfiles.Add(newProfile);
                SaveCrm();

                // Přepočítá barvy u inzerátů, kdyby od ní už v Třídičce nějaký byl
                foreach (var job in DatabaseOfJobs.Where(j => newProfile.Aliases.Any(a => string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase))))
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
                bool isBlacklisted = profile.Aliases.Any(a => blacklistedCompanies.Any(b => string.Equals(b, a, StringComparison.OrdinalIgnoreCase)));
                var crmWindow = new CrmWindow(profile, profile.PrimaryName, isBlacklisted) { Owner = this };
                if (crmWindow.ShowDialog() == true)
                {
                    SaveCrm();
                    CollectionViewSource.GetDefaultView(CrmProfiles).Refresh(); // Aktualizuje tabulku CRM

                    
                    foreach (var job in DatabaseOfJobs.Where(j => profile.Aliases.Any(a => string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase))))
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
                    foreach (var job in DatabaseOfJobs.Where(j => profile.Aliases.Any(a => string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase))))
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
                    foreach (var job in DatabaseOfJobs.Where(j => profile.Aliases.Any(a => string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase))))
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

                    // Inzerátům této firmy resetuje barvu na výchozí
                    foreach (var job in DatabaseOfJobs.Where(j => profile.Aliases.Any(a => string.Equals(a, j.Company, StringComparison.OrdinalIgnoreCase))))
                        job.CrmReputation = 0;

                    CollectionViewSource.GetDefaultView(DatabaseOfJobs).Refresh();
                    SaveJobs();
                }
            }
        }
    }

}