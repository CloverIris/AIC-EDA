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
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<PlayerAsset> _playerAssets = new();

        [ObservableProperty]
        private Item? _selectedAssetItem;

        [ObservableProperty]
        private double _assetQuantity = 1;

        [ObservableProperty]
        private AssetType _selectedAssetType = AssetType.Material;

        [ObservableProperty]
        private string _balancedLayoutResult = string.Empty;

        // Radar chart weights (0-100)
        [ObservableProperty]
        private double _weightResource = 50;

        [ObservableProperty]
        private double _weightProcessing = 50;

        [ObservableProperty]
        private double _weightLogistics = 50;

        [ObservableProperty]
        private double _weightPower = 50;

        [ObservableProperty]
        private double _weightAgriculture = 50;

        // Q2: Development tiers
        [ObservableProperty]
        private bool _tierPrimarySelected = true;

        [ObservableProperty]
        private bool _tierIntermediateSelected = false;

        [ObservableProperty]
        private bool _tierAdvancedSelected = false;

        [ObservableProperty]
        private ObservableCollection<CheckListItem> _checkListItems = new();

        [ObservableProperty]
        private ObservableCollection<RoadMapStage> _roadMapStages = new();

        [ObservableProperty]
        private double _checkListProgress = 0;

        // Q3: Multi-target
        [ObservableProperty]
        private ObservableCollection<TargetSelection> _targetSelections = new();

        [ObservableProperty]
        private ObservableCollection<SharedIntermediate> _sharedIntermediates = new();

        [ObservableProperty]
        private string _unifiedLayoutSummary = string.Empty;

        // Q4: Simulation
        [ObservableProperty]
        private bool _isSimulating = false;

        [ObservableProperty]
        private string _simulationStatus = "Ready";

        public List<Item> AllItems => RecipeDatabaseService.Instance.Items.ToList();
        public List<Item> FinalProducts => RecipeDatabaseService.Instance.Items.Where(i => i.Category == ItemCategory.FinalProduct).ToList();

        public DashboardViewModel()
        {
            InitializeTargetSelections();
        }

        private void InitializeTargetSelections()
        {
            TargetSelections.Clear();
            foreach (var item in FinalProducts)
            {
                TargetSelections.Add(new TargetSelection { Item = item, IsSelected = false, Rate = 60 });
            }
        }

        [RelayCommand]
        private void AddAsset()
        {
            if (SelectedAssetItem == null) return;
            var asset = new PlayerAsset
            {
                ItemId = SelectedAssetItem.Id,
                ItemName = SelectedAssetItem.Name,
                Type = SelectedAssetType,
                Quantity = AssetQuantity,
            };
            PlayerAssets.Add(asset);
        }

        [RelayCommand]
        private void RemoveAsset(PlayerAsset asset)
        {
            if (asset != null) PlayerAssets.Remove(asset);
        }

        [RelayCommand]
        private void ClearAssets()
        {
            PlayerAssets.Clear();
        }

        [RelayCommand]
        private void CalculateBalancedLayout()
        {
            var db = RecipeDatabaseService.Instance;
            var assets = PlayerAssets.ToList();
            if (assets.Count == 0)
            {
                BalancedLayoutResult = "No assets recorded. Add your current machines/materials first.";
                return;
            }

            // Simple analysis: count machines by category and estimate max throughput
            int machineCount = assets.Count(a => a.Type == AssetType.Machine);
            int materialCount = assets.Count(a => a.Type == AssetType.Material);
            double totalQty = assets.Sum(a => a.Quantity);

            BalancedLayoutResult = $"Assets: {machineCount} machines, {materialCount} materials (total qty: {totalQty:F0}).\n" +
                $"Weighted priority: Resource {WeightResource:F0}% | Processing {WeightProcessing:F0}% | " +
                $"Logistics {WeightLogistics:F0}% | Power {WeightPower:F0}% | Agriculture {WeightAgriculture:F0}%\n" +
                $"Recommend focusing on {(WeightResource > 60 ? "mining expansion" : WeightProcessing > 60 ? "processing lines" : WeightPower > 60 ? "power infrastructure" : "balanced growth")}.";
        }

        [RelayCommand]
        private void GenerateRoadMap()
        {
            CheckListItems.Clear();
            RoadMapStages.Clear();

            var db = RecipeDatabaseService.Instance;
            var selectedRecipes = new List<Recipe>();

            if (TierPrimarySelected)
            {
                selectedRecipes.AddRange(db.Recipes.Where(r =>
                    r.Machine == MachineType.MiningRig ||
                    r.Machine == MachineType.MiningRigMk2 ||
                    r.Machine == MachineType.HydraulicMiningRig ||
                    r.Machine == MachineType.RefiningUnit ||
                    r.Machine == MachineType.ShreddingUnit ||
                    r.Machine == MachineType.GrindingUnit));
            }
            if (TierIntermediateSelected)
            {
                selectedRecipes.AddRange(db.Recipes.Where(r =>
                    r.Machine == MachineType.FittingUnit ||
                    r.Machine == MachineType.GearingUnit ||
                    r.Machine == MachineType.MouldingUnit ||
                    r.Machine == MachineType.FillingUnit ||
                    r.Machine == MachineType.SeparatingUnit ||
                    r.Machine == MachineType.PackagingUnit));
            }
            if (TierAdvancedSelected)
            {
                selectedRecipes.AddRange(db.Recipes.Where(r =>
                    r.Machine == MachineType.ReactorCrucible ||
                    r.Id.StartsWith("make_") ||
                    r.Id.StartsWith("craft_gear_")));
            }

            // Deduplicate
            selectedRecipes = selectedRecipes.DistinctBy(r => r.Id).ToList();

            // Build checklist from all input materials
            var neededItems = new Dictionary<string, double>();
            foreach (var recipe in selectedRecipes)
            {
                foreach (var input in recipe.Inputs)
                {
                    if (neededItems.ContainsKey(input.Key))
                        neededItems[input.Key] += input.Value;
                    else
                        neededItems[input.Key] = input.Value;
                }
            }

            foreach (var kv in neededItems.OrderBy(kv => kv.Key))
            {
                var item = db.GetItem(kv.Key);
                CheckListItems.Add(new CheckListItem
                {
                    ItemId = kv.Key,
                    ItemName = item?.Name ?? kv.Key,
                    Quantity = kv.Value,
                    IsChecked = false,
                });
            }

            UpdateCheckListProgress();

            // Build roadmap stages
            if (TierPrimarySelected)
            {
                RoadMapStages.Add(new RoadMapStage
                {
                    Order = 1,
                    Title = "Phase 1: Raw Material Collection",
                    Description = "Establish mining outposts and basic extraction",
                    RequiredMachines = new() { "Mining Rig", "Fluid Pump" },
                    EstimatedPower = 30,
                    EstimatedArea = 200,
                });
                RoadMapStages.Add(new RoadMapStage
                {
                    Order = 2,
                    Title = "Phase 2: Primary Processing",
                    Description = "Build refining, shredding, and grinding lines",
                    RequiredMachines = new() { "Refining Unit", "Shredding Unit", "Grinding Unit" },
                    EstimatedPower = 80,
                    EstimatedArea = 400,
                });
            }
            if (TierIntermediateSelected)
            {
                RoadMapStages.Add(new RoadMapStage
                {
                    Order = 3,
                    Title = "Phase 3: Component Manufacturing",
                    Description = "Produce parts, components, bottles, and packaging",
                    RequiredMachines = new() { "Fitting Unit", "Gearing Unit", "Moulding Unit", "Packaging Unit" },
                    EstimatedPower = 150,
                    EstimatedArea = 600,
                });
            }
            if (TierAdvancedSelected)
            {
                RoadMapStages.Add(new RoadMapStage
                {
                    Order = 4,
                    Title = "Phase 4: Advanced Production",
                    Description = "Manufacture final products, batteries, explosives, and gear",
                    RequiredMachines = new() { "Reactor Crucible", "Gearing Unit" },
                    EstimatedPower = 300,
                    EstimatedArea = 1000,
                });
            }
        }

        [RelayCommand]
        private void ToggleCheckItem(CheckListItem item)
        {
            if (item != null)
            {
                item.IsChecked = !item.IsChecked;
                UpdateCheckListProgress();
            }
        }

        private void UpdateCheckListProgress()
        {
            if (CheckListItems.Count == 0)
            {
                CheckListProgress = 0;
                return;
            }
            CheckListProgress = (double)CheckListItems.Count(i => i.IsChecked) / CheckListItems.Count;
        }

        [RelayCommand]
        private void AnalyzeSharedIntermediates()
        {
            var selected = TargetSelections.Where(t => t.IsSelected).ToList();
            if (selected.Count < 2)
            {
                UnifiedLayoutSummary = "Select at least 2 target products to analyze shared intermediates.";
                SharedIntermediates.Clear();
                return;
            }

            var db = RecipeDatabaseService.Instance;
            var allInputs = new Dictionary<string, List<string>>();

            foreach (var target in selected)
            {
                var recipes = db.FindRecipesByOutput(target.Item.Id);
                foreach (var recipe in recipes)
                {
                    foreach (var input in recipe.Inputs.Keys)
                    {
                        if (!allInputs.ContainsKey(input))
                            allInputs[input] = new List<string>();
                        allInputs[input].Add(target.Item.Name);
                    }
                }
            }

            SharedIntermediates.Clear();
            foreach (var kv in allInputs.Where(x => x.Value.Count > 1).OrderByDescending(x => x.Value.Count))
            {
                var item = db.GetItem(kv.Key);
                SharedIntermediates.Add(new SharedIntermediate
                {
                    ItemId = kv.Key,
                    ItemName = item?.Name ?? kv.Key,
                    UsedByCount = kv.Value.Count,
                    UsedByProducts = string.Join(", ", kv.Value.Distinct()),
                });
            }

            int sharedCount = SharedIntermediates.Count;
            UnifiedLayoutSummary = $"Selected {selected.Count} targets with {sharedCount} shared intermediate(s). " +
                $"Merge production lines to save approximately {sharedCount * 15}% total machine count.";
        }

        [RelayCommand]
        private void ToggleSimulation()
        {
            IsSimulating = !IsSimulating;
            SimulationStatus = IsSimulating ? "Running simulation..." : "Paused";
        }
    }

    public partial class CheckListItem : ObservableObject
    {
        [ObservableProperty]
        private string _itemId = string.Empty;

        [ObservableProperty]
        private string _itemName = string.Empty;

        [ObservableProperty]
        private double _quantity;

        [ObservableProperty]
        private bool _isChecked;
    }

    public partial class TargetSelection : ObservableObject
    {
        [ObservableProperty]
        private Item _item = new();

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private double _rate = 60;
    }

    public partial class SharedIntermediate : ObservableObject
    {
        [ObservableProperty]
        private string _itemId = string.Empty;

        [ObservableProperty]
        private string _itemName = string.Empty;

        [ObservableProperty]
        private int _usedByCount;

        [ObservableProperty]
        private string _usedByProducts = string.Empty;
    }
}
