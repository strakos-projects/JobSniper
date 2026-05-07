using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace JobSniper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /*protected override void OnStartup(StartupEventArgs e)
        {
            // Vytvoříme anglickou kulturu (např. americkou angličtinu)
            var culture = new CultureInfo("en-US"); // Můžeš použít i jen "en" pro obecnou angličtinu

            // Nastaví jazyk pro formátování (datum, měna, čísla)
            Thread.CurrentThread.CurrentCulture = culture;

            // Nastaví jazyk pro UI (vybírání .resx souborů)
            Thread.CurrentThread.CurrentUICulture = culture;

            // Pro moderní .NET aplikace je dobré nastavit i výchozí kulturu pro nově vznikající vlákna (např. async operace)
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Je nutné zavolat base metodu, aby aplikace normálně odstartovala
            base.OnStartup(e);
        } */
    }
}