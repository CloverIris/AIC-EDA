using AIC_EDA.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AIC_EDA
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(NavView);
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            NavigateToPage("RecipeBrowser");
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                NavigateToPage(tag);
            }
        }

        private void NavigateToPage(string tag)
        {
            switch (tag)
            {
                case "RecipeBrowser":
                    ContentFrame.Navigate(typeof(RecipeBrowserPage));
                    break;
                case "RecipeCompiler":
                    ContentFrame.Navigate(typeof(RecipeCompilerPage));
                    break;
                case "LayoutPreview":
                    ContentFrame.Navigate(typeof(LayoutPreviewPage));
                    break;
                case "BlueprintExport":
                    ContentFrame.Navigate(typeof(BlueprintExportPage));
                    break;
            }
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            // Log or handle navigation failure
            e.Handled = true;
        }
    }
}
