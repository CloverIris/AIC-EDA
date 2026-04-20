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
            this.SetTitleBar(null); // Use custom title bar area from MenuBar
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            NavigateToPage("RecipeBrowser");
            UpdateProjectInfo();
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                NavigateToPage(tag);
            }
        }

        public void NavigateToPage(string tag)
        {
            NavigateToPage(tag, null);
        }

        public void NavigateToPage(string tag, object? parameter)
        {
            switch (tag)
            {
                case "RecipeBrowser":
                    ContentFrame.Navigate(typeof(RecipeBrowserPage));
                    break;
                case "RecipeCompiler":
                    ContentFrame.Navigate(typeof(RecipeCompilerPage), parameter);
                    break;
                case "LayoutPreview":
                    ContentFrame.Navigate(typeof(LayoutPreviewPage));
                    break;
                case "LayoutDesigner":
                    ContentFrame.Navigate(typeof(LayoutDesignerPage));
                    break;
                case "BlueprintExport":
                    ContentFrame.Navigate(typeof(BlueprintExportPage));
                    break;
            }
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            e.Handled = true;
        }

        // ===== MENU HANDLERS =====
        private void MenuNewProject_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentGraph = null;
            ContentFrame.Navigate(typeof(RecipeCompilerPage));
            UpdateProjectInfo();
        }

        private void MenuOpenProject_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open file picker
        }

        private void MenuSaveBlueprint_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentGraph != null)
            {
                ContentFrame.Navigate(typeof(BlueprintExportPage));
            }
        }

        private void MenuExportJson_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentGraph != null)
            {
                ContentFrame.Navigate(typeof(BlueprintExportPage));
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void MenuUndo_Click(object sender, RoutedEventArgs e) { }
        private void MenuRedo_Click(object sender, RoutedEventArgs e) { }
        private void MenuPreferences_Click(object sender, RoutedEventArgs e) { }

        private void MenuNavigate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is string tag)
            {
                NavigateToPage(tag);
                // Sync NavigationView selection
                foreach (var navItem in NavView.MenuItems)
                {
                    if (navItem is NavigationViewItem nvi && nvi.Tag?.ToString() == tag)
                    {
                        NavView.SelectedItem = nvi;
                        break;
                    }
                }
            }
        }

        private void MenuToggleRightPanel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem toggle)
            {
                RightToolPanel.Visibility = toggle.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void MenuResetLayout_Click(object sender, RoutedEventArgs e)
        {
            RightToolPanel.Visibility = Visibility.Visible;
            ToggleRightPanelMenu.IsChecked = true;
        }

        private void MenuCompile_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(RecipeCompilerPage));
            NavView.SelectedItem = NavView.MenuItems[1];
        }

        private void MenuOptimize_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(LayoutPreviewPage));
            NavView.SelectedItem = NavView.MenuItems[2];
        }

        private void MenuRunSTA_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Run throughput STA
        }

        private void MenuRunDRC_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Run DRC validator
        }

        private void MenuReloadDB_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Reload recipe database
        }

        private void MenuDocs_Click(object sender, RoutedEventArgs e) { }
        private void MenuShortcuts_Click(object sender, RoutedEventArgs e) { }
        private void MenuAbout_Click(object sender, RoutedEventArgs e) { }

        // ===== TOOLBAR HANDLERS =====
        private void ToolbarZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.Content is LayoutPreviewPage page)
            {
                page.ZoomIn();
            }
        }

        private void ToolbarZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.Content is LayoutPreviewPage page)
            {
                page.ZoomOut();
            }
        }

        // ===== PROJECT INFO UPDATE =====
        public void UpdateProjectInfo()
        {
            var graph = App.CurrentGraph;
            if (graph != null)
            {
                TargetItemText.Text = graph.TargetItem;
                TargetRateText.Text = graph.TargetRate.ToString("F1");
                DeviceCountText.Text = graph.Nodes.Count.ToString();
                PowerText.Text = graph.TotalPowerConsumption.ToString("F1");
            }
            else
            {
                TargetItemText.Text = "-";
                TargetRateText.Text = "-";
                DeviceCountText.Text = "-";
                PowerText.Text = "-";
            }
        }
    }
}
