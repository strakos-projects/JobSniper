using JobSniper;         // Reference na linkovaný CompanyProfile
using JobSniper.Models;  // Reference na linkovaný JobOffer
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SniperIntel
{
    public partial class MainWindow : Window
    {
        // Data v paměti
        private ObservableCollection<CompanyProfile> _crmProfiles = new ObservableCollection<CompanyProfile>();
        private List<JobOffer> _allJobs = new List<JobOffer>();

        // Cesty k souborům JobSniperu (předpokládá se složka Data vedle SniperIntel.exe)
        private readonly string crmFilePath = Path.Combine("Data", "crm_companies.json");
        private readonly string jobsFilePath = Path.Combine("Data", "jobs.json");

        public MainWindow()
        {
            InitializeComponent();
            LoadIntelData();
        }

        private void LoadIntelData()
        {
            // 1. Ochrana: Existuje vůbec složka Data?
            if (!Directory.Exists("Data") || !File.Exists(crmFilePath))
            {
                MessageBox.Show("Datové soubory JobSniperu nebyly nalezeny.\n\nUjisti se, že složka 'Data' (obsahující crm_companies.json a jobs.json) leží hned vedle spouštěcího souboru SniperIntel.exe.",
                                "Chybí Intel", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Načtení a seřazení CRM firem
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var loadedCrm = JsonSerializer.Deserialize<List<CompanyProfile>>(File.ReadAllText(crmFilePath), options) ?? new List<CompanyProfile>();

                // Hned při načtení seřadíme firmy logicky: Nejnovější interakce (nebo nejvyšší priorita) nahoru
                loadedCrm = loadedCrm.OrderByDescending(c => c.LastInteraction).ToList();
                _crmProfiles = new ObservableCollection<CompanyProfile>(loadedCrm);

                // 3. Načtení všech inzerátů z databáze (jako důkazy)
                if (File.Exists(jobsFilePath))
                {
                    _allJobs = JsonSerializer.Deserialize<List<JobOffer>>(File.ReadAllText(jobsFilePath), options) ?? new List<JobOffer>();
                }

                // 4. Připojení firem do levého Master panelu
                LstCompanies.ItemsSource = _crmProfiles;
                TxtTotalCompanies.Text = _crmProfiles.Count.ToString();

                // 5. Zajištění live filtrace (Live Search)
                ICollectionView view = CollectionViewSource.GetDefaultView(LstCompanies.ItemsSource);
                view.Filter = FilterCompanies;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritická chyba při načítání databáze:\n{ex.Message}", "Chyba čtení", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        //  VYHLEDÁVÁNÍ (Live Search)
        // ==========================================
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Překreslí filtrovaný seznam při každém napsaném znaku
            if (LstCompanies.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(LstCompanies.ItemsSource).Refresh();
            }
        }

        private bool FilterCompanies(object item)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
                return true;

            var profile = item as CompanyProfile;
            if (profile == null) return false;

            string query = TxtSearch.Text.ToLower().Trim();

            // 1. Hledáme v názvu firmy
            bool matchName = profile.PrimaryName != null && profile.PrimaryName.ToLower().Contains(query);

            // 2. Hledáme v aliasech
            bool matchAliases = profile.Aliases != null && profile.Aliases.Any(a => a.ToLower().Contains(query));

            // 3. NOVĚ: Hledáme kompletně v celém CRM záznamu (know-how, telefony, jména)
            bool matchHistory = profile.InteractionHistory != null && profile.InteractionHistory.ToLower().Contains(query);

            // Pokud se najde shoda alespoň v jedné z těchto věcí, firma zůstane v seznamu
            return matchName || matchAliases || matchHistory;
        }

        // ==========================================
        //  PŘEPÍNÁNÍ DETAILU FIRMY (The Dossier)
        // ==========================================
        private void LstCompanies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = LstCompanies.SelectedItem as CompanyProfile;

            if (selectedProfile == null)
            {
                // Zobrazení empty state (úvodní terč), pokud odznačíme nebo vyfiltrujeme aktuální výběr
                GridIntelDetail.Visibility = Visibility.Hidden;
                PanelEmptyState.Visibility = Visibility.Visible;
                return;
            }

            // Přepnutí UI
            PanelEmptyState.Visibility = Visibility.Hidden;
            GridIntelDetail.Visibility = Visibility.Visible;

            // 1. Osobní Intel (Hlavička a CRM)
            TxtDetailName.Text = selectedProfile.PrimaryName;
            TxtDetailReputation.Text = selectedProfile.Reputation.ToString();

            // Obarvení reputace (Zelená = dobrá, Červená = špatná)
            TxtDetailReputation.Foreground = selectedProfile.Reputation >= 0
                ? new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"))
                : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E74C3C"));

            TxtDetailAliases.Text = selectedProfile.Aliases != null && selectedProfile.Aliases.Count > 1
                ? string.Join(" / ", selectedProfile.Aliases.Skip(1)) // Vypíšeme aliasy kromě PrimaryName
                : "Žádné aliasy";

            TxtInteractionHistory.Text = string.IsNullOrWhiteSpace(selectedProfile.InteractionHistory)
                ? "Není zadán žádný Intel. Záznam je prázdný."
                : selectedProfile.InteractionHistory;

            // 2. Inzeráty (Důkazy k firmě)
            // Protože jsi v JobSniperu udělal solidní párování (AssignCrmData), můžeme se
            // bezpečně spolehnout na vlastnost CrmCompanyId u inzerátů a vyhnout se těžkým regexům.
            var companyJobs = _allJobs
                .Where(j => j.CrmCompanyId == selectedProfile.CrmId)
                .OrderByDescending(j => j.DateScraped) // Nejnovější inzeráty nahoře
                .ToList();

            DgCompanyJobs.ItemsSource = companyJobs;
        }
    }
}