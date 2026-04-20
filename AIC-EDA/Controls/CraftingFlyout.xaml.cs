using AIC_EDA.Models;
using AIC_EDA.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIC_EDA.Controls
{
    public sealed partial class CraftingFlyout : UserControl
    {
        public Item TargetItem { get; private set; } = new();
        private List<Recipe> _recipes = new();
        private Recipe? _selectedRecipe;

        public CraftingFlyout()
        {
            this.InitializeComponent();
        }

        public void LoadItem(Item item)
        {
            TargetItem = item;
            var db = RecipeDatabaseService.Instance;
            _recipes = db.GetProductionRecipes(item.Id);

            // Header
            HeaderIcon.Glyph = item.IconGlyph;
            HeaderIcon.Foreground = item.CategoryColorBrush;
            HeaderName.Text = item.Name;
            HeaderNameEN.Text = item.NameEN;
            HeaderCategory.Text = item.CategoryLabel;

            // Output
            OutputIcon.Glyph = item.IconGlyph;
            OutputIcon.Foreground = item.CategoryColorBrush;
            OutputName.Text = item.Name;

            if (_recipes.Count == 0)
            {
                // Raw material
                CraftingGridPanel.Visibility = Visibility.Collapsed;
                ChainTreePanel.Visibility = Visibility.Collapsed;
                NoRecipeText.Visibility = Visibility.Visible;
                RecipeSelectorPanel.Visibility = Visibility.Collapsed;
                return;
            }

            NoRecipeText.Visibility = Visibility.Collapsed;
            CraftingGridPanel.Visibility = Visibility.Visible;
            ChainTreePanel.Visibility = Visibility.Visible;

            // Recipe selector
            if (_recipes.Count > 1)
            {
                RecipeSelectorPanel.Visibility = Visibility.Visible;
                RecipeButtonsPanel.Children.Clear();
                for (int i = 0; i < _recipes.Count; i++)
                {
                    int idx = i;
                    var btn = new Button
                    {
                        Content = _recipes[i].Name,
                        Style = (Style)Application.Current.Resources["EndfieldOutlineButtonStyle"],
                        Padding = new Thickness(8, 4, 8, 4),
                        FontSize = 11,
                    };
                    if (i == 0)
                    {
                        btn.Background = (Brush)Application.Current.Resources["AccentYellowBrush"];
                        btn.Foreground = new SolidColorBrush(Colors.Black);
                    }
                    btn.Click += (s, e) => SelectRecipe(idx);
                    RecipeButtonsPanel.Children.Add(btn);
                }
            }
            else
            {
                RecipeSelectorPanel.Visibility = Visibility.Collapsed;
            }

            SelectRecipe(0);
        }

        private void SelectRecipe(int index)
        {
            if (index < 0 || index >= _recipes.Count) return;
            _selectedRecipe = _recipes[index];

            // Highlight selected button
            for (int i = 0; i < RecipeButtonsPanel.Children.Count; i++)
            {
                if (RecipeButtonsPanel.Children[i] is Button btn)
                {
                    if (i == index)
                    {
                        btn.Background = (Brush)Application.Current.Resources["AccentYellowBrush"];
                        btn.Foreground = new SolidColorBrush(Colors.Black);
                    }
                    else
                    {
                        btn.Background = (Brush)Application.Current.Resources["SurfaceCardBrush"];
                        btn.Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"];
                    }
                }
            }

            // Fill crafting grid
            FillCraftingGrid(_selectedRecipe);

            // Recipe details
            RecipeMachineText.Text = _selectedRecipe.Machine.GetDisplayName();
            RecipeDurationText.Text = $"{_selectedRecipe.Duration}s";
            RecipePowerText.Text = $"{_selectedRecipe.PowerConsumption}kW";

            // Build chain tree
            var db = RecipeDatabaseService.Instance;
            var chain = db.GetCraftingChain(TargetItem.Id, requiredRatePerMinute: 60, maxDepth: 5);
            BuildChainTree(chain);
        }

        private void FillCraftingGrid(Recipe recipe)
        {
            CraftingGrid.Children.Clear();
            var db = RecipeDatabaseService.Instance;
            int idx = 0;
            foreach (var inputKv in recipe.Inputs)
            {
                if (idx >= 9) break;
                int row = idx / 3;
                int col = idx % 3;
                var item = db.GetItem(inputKv.Key);
                if (item == null) continue;

                var cell = new Border
                {
                    Background = (Brush)Application.Current.Resources["SurfaceDarkBrush"],
                    BorderBrush = (Brush)Application.Current.Resources["BorderDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(2),
                };
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);

                var stack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                };
                stack.Children.Add(new FontIcon
                {
                    Glyph = item.IconGlyph,
                    FontSize = 20,
                    Foreground = item.CategoryColorBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                stack.Children.Add(new TextBlock
                {
                    Text = $"{inputKv.Value:F0}",
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    TextAlignment = TextAlignment.Center,
                });
                stack.Children.Add(new TextBlock
                {
                    Text = item.Name,
                    FontSize = 9,
                    Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 40,
                });

                cell.Child = stack;
                CraftingGrid.Children.Add(cell);
                idx++;
            }

            // Fill remaining cells with empty borders
            for (int i = idx; i < 9; i++)
            {
                int row = i / 3;
                int col = i % 3;
                var cell = new Border
                {
                    Background = (Brush)Application.Current.Resources["SurfaceDarkBrush"],
                    BorderBrush = (Brush)Application.Current.Resources["BorderDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(2),
                    Opacity = 0.4,
                };
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                CraftingGrid.Children.Add(cell);
            }
        }

        private void BuildChainTree(CraftingChainNode? root)
        {
            ChainTreeStack.Children.Clear();
            if (root == null) return;

            var flatList = new List<CraftingChainNode>();
            FlattenChain(root, flatList);

            // Skip root (it's shown in header)
            foreach (var node in flatList.Skip(1))
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(node.Depth * 16) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Connector line
                var line = new Border
                {
                    Background = (Brush)Application.Current.Resources["BorderDefaultBrush"],
                    Margin = new Thickness(0, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    CornerRadius = new CornerRadius(1),
                };
                Grid.SetColumn(line, 1);
                row.Children.Add(line);

                // Content
                var content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(0, 4, 0, 4),
                };
                Grid.SetColumn(content, 2);

                content.Children.Add(new FontIcon
                {
                    Glyph = node.Item.IconGlyph,
                    FontSize = 16,
                    Foreground = node.Item.CategoryColorBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                textStack.Children.Add(new TextBlock
                {
                    Text = node.Item.Name,
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                });

                string machineName = node.Recipe?.Machine.GetDisplayName() ?? "Raw";
                textStack.Children.Add(new TextBlock
                {
                    Text = $"{node.RequiredRatePerMinute:F1}/min  @ {machineName}",
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
                });

                content.Children.Add(textStack);
                row.Children.Add(content);
                ChainTreeStack.Children.Add(row);
            }
        }

        private void FlattenChain(CraftingChainNode node, List<CraftingChainNode> output)
        {
            output.Add(node);
            foreach (var input in node.Inputs)
            {
                FlattenChain(input, output);
            }
        }
    }
}
