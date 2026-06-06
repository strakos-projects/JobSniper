using System;
using System.Windows;
using System.Windows.Controls;

namespace JobSniper
{
    public partial class ValidationWindow : Window
    {
        private readonly Func<string, string> _promptBuilder;

        // Vlastnost, kterou si PrivateAiWorkflow přečte po zavření okna
        public string FinalJobText { get; private set; }

        public ValidationWindow(string company, string title, string jobDescription, Func<string, string> promptBuilder)
        {
            InitializeComponent();

            TxtCompany.Text = string.IsNullOrWhiteSpace(company) ? "Neznámá" : company;
            TxtTitle.Text = string.IsNullOrWhiteSpace(title) ? "Neznámá" : title;
            TxtJobAd.Text = jobDescription;

            _promptBuilder = promptBuilder;
        }

        private void ExpPreview_Expanded(object sender, RoutedEventArgs e)
        {
            // Vygeneruje náhled, až když uživatel klikne na Expander
            TxtFullPrompt.Text = _promptBuilder(TxtJobAd.Text);
        }

        private void TxtJobAd_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Pokud uživatel edituje inzerát a má otevřený náhled, aktualizujeme ho živě
            if (ExpPreview != null && ExpPreview.IsExpanded)
            {
                TxtFullPrompt.Text = _promptBuilder(TxtJobAd.Text);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            // Uložíme zkontrolovaný inzerát a okno potvrdíme
            FinalJobText = TxtJobAd.Text;
            DialogResult = true;
            Close();
        }
    }
}