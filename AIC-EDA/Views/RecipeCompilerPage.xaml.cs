using AIC_EDA.Models;
using AIC_EDA.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AIC_EDA.Views
{
    public sealed partial class RecipeCompilerPage : Page
    {
        public RecipeCompilerViewModel ViewModel { get; } = new();

        public RecipeCompilerPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Item item)
            {
                ViewModel.SelectedTarget = item;
            }
        }
    }
}
