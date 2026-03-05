using System;
using System.Linq;
using System.Windows;

namespace JobSniper
{
    public partial class CrmWindow : Window
    {
        private CompanyProfile _profile;
        public bool IsBlacklisted => ChkIsBlacklisted.IsChecked == true;
        
        public CrmWindow(CompanyProfile profile, string primaryCompanyName, bool isBlacklisted)
        {
            InitializeComponent();
            _profile = profile;

            TxtCompanyName.Text = primaryCompanyName;
            TxtAliases.Text = string.Join(" ;;; ", _profile.Aliases);
            TxtHistory.Text = _profile.InteractionHistory;

            if (_profile.Reputation == 1) RbInfo.IsChecked = true;
            else if (_profile.Reputation == 2) RbWarning.IsChecked = true;
            else RbNeutral.IsChecked = true;

            ChkIsBlacklisted.IsChecked = isBlacklisted;
            CmbPotential.SelectedIndex = _profile.Potential;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Uložení aliasů (rozdělíme podle čárky, ořízneme mezery a vymažeme prázdné)
            _profile.Aliases = TxtAliases.Text
                 .Split(new string[] { ";;;" }, StringSplitOptions.None)
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
            _profile.Potential = CmbPotential.SelectedIndex >= 0 ? CmbPotential.SelectedIndex : 0;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}