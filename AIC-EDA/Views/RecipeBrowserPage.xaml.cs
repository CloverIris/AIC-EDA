using AIC_EDA.ViewModels;
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
    }
}
