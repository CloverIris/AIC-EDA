using AIC_EDA.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AIC_EDA.Views
{
    public sealed partial class RecipeCompilerPage : Page
    {
        public RecipeCompilerViewModel ViewModel { get; } = new();

        public RecipeCompilerPage()
        {
            this.InitializeComponent();
        }
    }
}
