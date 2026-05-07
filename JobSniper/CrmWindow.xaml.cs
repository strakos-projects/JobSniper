using JobSniper.Models;
using System;
using System.Linq;
using System.Reflection;
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

            var aiMock = AiEvaluation.GetDemoPosudek();
            LoadAiEvaluationToUI(aiMock);
        }
        private void LoadAiEvaluationToUI(AiEvaluation ai)
        {
            TxtAiScore.Text = $"{ai.MatchScore} %";
            PbOver.Value = ai.OverqualifiedRisk;
            PbUnder.Value = ai.UnderqualifiedRisk;
            TxtAiRole.Text = ai.HiddenRole;
            TxtAiStrategy.Text = ai.Strategy.ToString();
            TxtAiCategory.Text = $"Kategorie {ai.RecommendedCvCategory}";
            TxtAiCoach.Text = ai.FullCoachText;

            // Nabindování listu s Red Flags
            ListAiRedFlags.ItemsSource = ai.RedFlags;
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