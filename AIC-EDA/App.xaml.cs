using AIC_EDA.Models;
using Microsoft.UI.Xaml;

namespace AIC_EDA
{
    public partial class App : Application
    {
        public static ProductionGraph? CurrentGraph { get; set; }
        public static Window? MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
    }
}
