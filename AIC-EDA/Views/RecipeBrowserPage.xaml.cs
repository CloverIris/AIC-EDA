using AIC_EDA.Models;
using AIC_EDA.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIC_EDA.Views
{
    public sealed partial class RecipeBrowserPage : Page
    {
        public RecipeBrowserViewModel ViewModel { get; } = new();

        public RecipeBrowserPage()
        {
            this.InitializeComponent();
        }

        private void SetFilterChipActive(Button activeButton)
        {
            // Reset all chips to default style
            FilterAllButton.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SurfaceCardBrush"];
            FilterAllButton.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"];

            var parent = FilterAllButton.Parent as StackPanel;
            if (parent != null)
            {
                foreach (var child in parent.Children)
                {
                    if (child is Button btn)
                    {
                        btn.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SurfaceCardBrush"];
                        btn.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"];
                    }
                }
            }

            // Highlight active
            activeButton.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentYellowBrush"];
            activeButton.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SurfaceDarkBrush"];
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedCategory = null;
            SetFilterChipActive((Button)sender);
        }

        private void FilterRaw_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedCategory = ItemCategory.RawMaterial;
            SetFilterChipActive((Button)sender);
        }

        private void FilterIntermediate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedCategory = ItemCategory.Intermediate;
            SetFilterChipActive((Button)sender);
        }

        private void FilterFinal_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedCategory = ItemCategory.FinalProduct;
            SetFilterChipActive((Button)sender);
        }

        private void FilterFluid_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedCategory = ItemCategory.Fluid;
            SetFilterChipActive((Button)sender);
        }

        private void FilterSpecial_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedCategory = ItemCategory.Special;
            SetFilterChipActive((Button)sender);
        }

        private void CompileTarget_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedItem == null) return;

            // Navigate to RecipeCompiler with this item pre-selected
            // This requires MainWindow to expose a navigation method
            // For now, just open the compiler page
            if (App.MainWindow is MainWindow main)
            {
                main.NavigateToPage("RecipeCompiler");
            }
        }
    }
}
