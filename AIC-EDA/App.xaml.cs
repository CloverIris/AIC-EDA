using AIC_EDA.Models;
using AIC_EDA.Views;
using Microsoft.UI.Xaml;
using System;
using Windows.Storage;

namespace AIC_EDA
{
    public partial class App : Application
    {
        public static ProductionGraph? CurrentGraph { get; set; }
        public static Window? MainWindow { get; private set; }
        public static Window? WelcomeWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
            this.RequestedTheme = ApplicationTheme.Dark;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Check if user wants to skip welcome screen
            var settings = ApplicationData.Current.LocalSettings;
            bool showWelcome = true;
            if (settings.Values.TryGetValue("ShowWelcomeScreen", out object? val) && val is bool b)
            {
                showWelcome = b;
            }

            if (showWelcome)
            {
                WelcomeWindow = new WelcomeWindow();
                WelcomeWindow.Activate();
            }
            else
            {
                OpenMainWindow();
            }
        }

        public static void OpenMainWindow()
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }
            MainWindow.Activate();
        }

        public static void CloseWelcomeWindow()
        {
            if (WelcomeWindow != null)
            {
                WelcomeWindow.Close();
                WelcomeWindow = null;
            }
        }
    }
}
