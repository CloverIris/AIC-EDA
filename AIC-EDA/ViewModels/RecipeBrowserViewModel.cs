using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIC_EDA.Models;
using AIC_EDA.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AIC_EDA.ViewModels
{
    public partial class RecipeBrowserViewModel : ObservableObject
    {
        private readonly RecipeDatabaseService _db;

        [ObservableProperty]
        private ObservableCollection<Item> _items = new();

        [ObservableProperty]
        private ObservableCollection<Recipe> _recipes = new();

        [ObservableProperty]
        private Item? _selectedItem;

        [ObservableProperty]
        private Recipe? _selectedRecipe;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Recipe> _itemRecipes = new();

        [ObservableProperty]
        private ObservableCollection<Recipe> _itemUsages = new();

        public RecipeBrowserViewModel()
        {
            _db = RecipeDatabaseService.Instance;
            if (!_db.IsLoaded)
            {
                _db.LoadDefaultData();
            }

            LoadData();
        }

        private void LoadData()
        {
            Items = new ObservableCollection<Item>(_db.Items.OrderBy(i => i.Category).ThenBy(i => i.Name));
            Recipes = new ObservableCollection<Recipe>(_db.Recipes.OrderBy(r => r.Machine.ToString()).ThenBy(r => r.Name));
        }

        partial void OnSelectedItemChanged(Item? value)
        {
            if (value != null)
            {
                ItemRecipes = new ObservableCollection<Recipe>(_db.FindRecipesByOutput(value.Id));
                ItemUsages = new ObservableCollection<Recipe>(_db.FindRecipesByInput(value.Id));
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                LoadData();
                return;
            }

            var filteredItems = _db.Items.Where(i =>
                i.Name.Contains(value) || i.NameEN.Contains(value, System.StringComparison.OrdinalIgnoreCase)).ToList();
            Items = new ObservableCollection<Item>(filteredItems);

            var filteredRecipes = _db.Recipes.Where(r =>
                r.Name.Contains(value) || r.Machine.GetDisplayName().Contains(value)).ToList();
            Recipes = new ObservableCollection<Recipe>(filteredRecipes);
        }
    }
}
