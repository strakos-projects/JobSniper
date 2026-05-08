using JobSniper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JobSniper
{
    public partial class CrmWindow : Window
    {
        private CompanyProfile _profile;
        private EvaluationRepository _evalRepo;
        public bool IsBlacklisted => ChkIsBlacklisted.IsChecked == true;

        public CrmWindow(CompanyProfile profile, string primaryCompanyName, bool isBlacklisted, IEnumerable<JobOffer> companyJobs, EvaluationRepository evalRepo)
        {
            InitializeComponent();
            _profile = profile;
            _evalRepo = evalRepo;

            TxtCompanyName.Text = primaryCompanyName;
            TxtAliases.Text = string.Join(" ;;; ", _profile.Aliases);
            TxtHistory.Text = _profile.InteractionHistory;

            if (_profile.Reputation == 1) RbInfo.IsChecked = true;
            else if (_profile.Reputation == 2) RbWarning.IsChecked = true;
            else RbNeutral.IsChecked = true;

            ChkIsBlacklisted.IsChecked = isBlacklisted;
            CmbPotential.SelectedIndex = _profile.Potential;

            var sortedJobs = companyJobs.OrderByDescending(j => j.DateScraped).ToList();
            LstCompanyJobs.ItemsSource = sortedJobs;

            if (sortedJobs.Count > 0)
            {
                LstCompanyJobs.SelectedIndex = 0; // Automaticky vybere první inzerát
            }
            else
            {
                TxtNoEvalMsg.Text = "No job offers found for this company.";
            }
        }
        private void LstCompanyJobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstCompanyJobs.SelectedItem is JobOffer selectedJob)
            {
                // Zeptáme se "Lazy" repozitáře, jestli má pro toto JobId posudek
                var eval = _evalRepo.GetEvaluation(selectedJob.JobId);

                if (eval != null && !string.IsNullOrWhiteSpace(eval.FullCoachText))
                {
                    // Zapneme UI detaily
                    GridNoEval.Visibility = Visibility.Collapsed;
                    ScrollEvalDetails.Visibility = Visibility.Visible;
                    LoadAiEvaluationToUI(eval);
                }
                else
                {
                    // Vypneme UI detaily a zobrazíme zprávu
                    GridNoEval.Visibility = Visibility.Visible;
                    ScrollEvalDetails.Visibility = Visibility.Collapsed;
                    TxtNoEvalMsg.Text = "No AI evaluation generated for this role yet.";
                }
            }
        }

        private void LoadAiEvaluationToUI(AiEvaluation ai)
        {
            TxtAiScore.Text = $"{ai.MatchScore} %";
            PbOver.Value = ai.OverqualifiedRisk;
            PbUnder.Value = ai.UnderqualifiedRisk;
            TxtAiRole.Text = string.IsNullOrWhiteSpace(ai.HiddenRole) ? "Unknown" : ai.HiddenRole;
            TxtAiStrategy.Text = ai.Strategy.ToString();

            // Plný text od AI
            TxtAiCoach.Text = ai.FullCoachText;

            // Pokud nejsou žádné RedFlags, panel raději schováme, ať to vypadá čistě
            if (ai.RedFlags != null && ai.RedFlags.Any())
            {
                PanelRedFlags.Visibility = Visibility.Visible;
                ListAiRedFlags.ItemsSource = ai.RedFlags;
            }
            else
            {
                PanelRedFlags.Visibility = Visibility.Collapsed;
                ListAiRedFlags.ItemsSource = null;
            }
        }
        /*private void LoadAiEvaluationToUI_old(AiEvaluation ai)
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
        }*/
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