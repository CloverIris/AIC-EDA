using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIC_EDA.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "AIC-EDA 工业自动化布局系统";

        [ObservableProperty]
        private object? _selectedPage;

        [ObservableProperty]
        private string _statusText = "就绪";

        public List<NavItem> NavigationItems { get; } = new()
        {
            new NavItem { Icon = "\uE71D", Label = "配方浏览器", Tag = "RecipeBrowser" },
            new NavItem { Icon = "\uE7C3", Label = "Recipe Compiler", Tag = "RecipeCompiler" },
            new NavItem { Icon = "\uE7F4", Label = "布局预览", Tag = "LayoutPreview" },
            new NavItem { Icon = "\uE8A1", Label = "蓝图导出", Tag = "BlueprintExport" },
        };

        [RelayCommand]
        private void Navigate(object? parameter)
        {
            if (parameter is string pageTag)
            {
                SelectedPage = pageTag;
                StatusText = $"当前页面: {NavigationItems.FirstOrDefault(n => n.Tag == pageTag)?.Label ?? pageTag}";
            }
        }
    }

    public class NavItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
    }
}
