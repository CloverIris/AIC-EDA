using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIC_EDA.Models;
using AIC_EDA.Services;
using System;
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

        [ObservableProperty]
        private ItemCategory? _selectedCategory = null;

        [ObservableProperty]
        private ObservableCollection<ItemCategory> _categories = new();

        public RecipeBrowserViewModel()
        {
            _db = RecipeDatabaseService.Instance;
            if (!_db.IsLoaded)
            {
                _db.LoadDefaultData();
            }

            Categories = new ObservableCollection<ItemCategory>(Enum.GetValues<ItemCategory>());
            LoadData();

            // Auto-select first item to populate recipe lists
            if (Items.Count > 0)
            {
                SelectedItem = Items[0];
            }
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
            else
            {
                ItemRecipes = new ObservableCollection<Recipe>();
                ItemUsages = new ObservableCollection<Recipe>();
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        partial void OnSelectedCategoryChanged(ItemCategory? value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _db.Items.AsEnumerable();

            if (SelectedCategory.HasValue)
            {
                query = query.Where(i => i.Category == SelectedCategory.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(i =>
                    i.Name.Contains(SearchText) ||
                    i.NameEN.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Items = new ObservableCollection<Item>(query.OrderBy(i => i.Category).ThenBy(i => i.Name));

            // Auto-select first filtered item
            if (Items.Count > 0)
            {
                SelectedItem = Items[0];
            }
        }
    }
}
