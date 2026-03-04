using System;
using System.Linq;
using System.Windows;

namespace JobSniper
{
    public partial class CrmWindow : Window
    {
        private CompanyProfile _profile;

        // Konstruktor přijímá profil firmy (existující nebo nově vytvořený) a hlavní název
        public CrmWindow(CompanyProfile profile, string primaryCompanyName)
        {
            InitializeComponent();
            _profile = profile;

            // Zobrazíme hlavní jméno firmy
            TxtCompanyName.Text = primaryCompanyName;

            // Načtení dat z profilu do UI
            TxtAliases.Text = string.Join(", ", _profile.Aliases);
            TxtHistory.Text = _profile.InteractionHistory;

            // Nastavení vybrané barvy (Radio buttonů)
            if (_profile.Reputation == 1) RbInfo.IsChecked = true;
            else if (_profile.Reputation == 2) RbWarning.IsChecked = true;
            else RbNeutral.IsChecked = true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Uložení aliasů (rozdělíme podle čárky, ořízneme mezery a vymažeme prázdné)
            _profile.Aliases = TxtAliases.Text
                .Split(',')
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a))
                .ToList();

            // Uložení historie
            _profile.InteractionHistory = TxtHistory.Text;
            _profile.LastInteraction = DateTime.Now;

            // Uložení reputace
            if (RbInfo.IsChecked == true) _profile.Reputation = 1;
            else if (RbWarning.IsChecked == true) _profile.Reputation = 2;
            else _profile.Reputation = 0;

            // Řekneme hlavnímu oknu, že se uložení povedlo a zavřeme okno
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}