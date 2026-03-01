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

namespace JobSniper
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<JobOffer> DatabaseOfJobs { get; set; } = new ObservableCollection<JobOffer>();
        public ObservableCollection<JobOffer> SessionDuplicates { get; set; } = new ObservableCollection<JobOffer>();

        private readonly string urlsFilePath = "urls.json";
        private readonly string jobsFilePath = "jobs.json";
        private readonly string blacklistFilePath = "blacklist.json";

        private List<ScrapeUrl> savedUrls = new List<ScrapeUrl>();
        private List<string> blacklistedCompanies = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            DataGridJobs.ItemsSource = DatabaseOfJobs;

            LoadUrls();
            LoadBlacklist();
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

            TxtCekaVTridicce.Text = inbox.ToString();
            TxtMojePrilezitosti.Text = opportunities.ToString();
            TxtZahozeno.Text = $"{trashedAuto} / {trashedManual}";

            BtnTridicka.Content = $"📥 Inbox ({inbox})";
            BtnPrilezitosti.Content = $"⭐ Opportunities ({opportunities})";
            BtnKos.Content = $"🗑️ Trash ({trashedManual + trashedAuto})";
            BtnDuplicity.Content = $"🔁 Session Duplicates ({SessionDuplicates.Count})";
        }

        private void SetView(Grid visibleGrid, Button activeButton, string title = "", int? filterStatus = null, bool showDuplicates = false)
        {
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
                            SessionDuplicates.Add(job);
                            dupCount++;
                        }
                        else
                        {
                            job.Url = cleanUrl;
                            if (blacklistedCompanies.Contains(job.Company)) job.Status = 3;
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

        private void LoadJobs()
        {
            if (File.Exists(jobsFilePath))
            {
                var loaded = JsonSerializer.Deserialize<List<JobOffer>>(File.ReadAllText(jobsFilePath)) ?? new List<JobOffer>();
                foreach (var j in loaded) DatabaseOfJobs.Add(j);
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
    }
}