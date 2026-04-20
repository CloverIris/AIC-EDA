using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIC_EDA.Core;
using AIC_EDA.Models;
using AIC_EDA.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AIC_EDA.ViewModels
{
    public partial class RecipeCompilerViewModel : ObservableObject
    {
        private readonly RecipeDatabaseService _db;
        private readonly RecipeCompiler _compiler;
        private readonly FlowBalancer _balancer;
        private readonly SpatialPlanner _planner;

        [ObservableProperty]
        private ObservableCollection<Item> _finalProducts = new();

        [ObservableProperty]
        private Item? _selectedTarget;

        [ObservableProperty]
        private double _targetRate = 10;

        [ObservableProperty]
        private ProductionGraph? _compiledGraph;

        [ObservableProperty]
        private ObservableCollection<ProductionNodeViewModel> _nodes = new();

        [ObservableProperty]
        private ObservableCollection<BottleneckReport> _bottlenecks = new();

        [ObservableProperty]
        private string _statusMessage = "请选择目标产物并点击编译";

        [ObservableProperty]
        private bool _isCompiled;

        [ObservableProperty]
        private double _totalPower;

        [ObservableProperty]
        private int _totalMachines;

        [ObservableProperty]
        private ObservableCollection<RawMaterialRequirement> _rawMaterials = new();

        [ObservableProperty]
        private int _totalLayers;

        public RecipeCompilerViewModel()
        {
            _db = RecipeDatabaseService.Instance;
            if (!_db.IsLoaded)
            {
                _db.LoadDefaultData();
            }

            _compiler = new RecipeCompiler(_db);
            _balancer = new FlowBalancer();
            _planner = new SpatialPlanner();

            // 加载最终产物列表
            FinalProducts = new ObservableCollection<Item>(
                _db.Items.Where(i => i.Category == ItemCategory.FinalProduct).OrderBy(i => i.Name));
        }

        [RelayCommand]
        private void Compile()
        {
            if (SelectedTarget == null) return;

            try
            {
                StatusMessage = $"正在编译: {SelectedTarget.Name} @ {TargetRate}/min...";

                // 步骤1: 配方编译（反向构建生产树）
                var graph = _compiler.Compile(SelectedTarget.Id, TargetRate);

                // 步骤2: 流量平衡
                graph = _balancer.Balance(graph, SelectedTarget.Id, TargetRate);

                // 步骤3: 2D布局
                graph = _planner.AutoLayout2D(graph);

                CompiledGraph = graph;

                // 保存到全局
                App.CurrentGraph = graph;

                // 更新UI数据
                UpdateUI(graph);
                IsCompiled = true;
                StatusMessage = $"编译完成: 共{graph.Nodes.Count}个节点, {graph.Edges.Count}条连接";
            }
            catch (Exception ex)
            {
                StatusMessage = $"编译失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private void OptimizeLayout()
        {
            if (CompiledGraph == null) return;

            CompiledGraph = _planner.OptimizeLayout(CompiledGraph);
            UpdateUI(CompiledGraph);
            StatusMessage = "布局优化完成";
        }

        [RelayCommand]
        private void DetectBottlenecks()
        {
            if (CompiledGraph == null) return;

            var bneck = _balancer.DetectBottlenecks(CompiledGraph);
            Bottlenecks = new ObservableCollection<BottleneckReport>(bneck);
            StatusMessage = $"检测到 {bneck.Count} 个瓶颈";
        }

        private void UpdateUI(ProductionGraph graph)
        {
            // 节点视图模型
            var nodeVMs = graph.Nodes.Select(n => new ProductionNodeViewModel
            {
                Id = n.Id,
                DisplayName = n.DisplayName,
                MachineType = n.Recipe.Machine.GetDisplayName(),
                RecipeName = n.Recipe.Name,
                Count = n.Count,
                Layer = n.Layer,
                Position = n.Position?.ToString() ?? "未放置",
                PowerConsumption = n.TotalPowerConsumption,
                Outputs = string.Join(", ", n.Recipe.Outputs.Keys.Select(k => _db.GetItem(k)?.Name ?? k)),
                Inputs = string.Join(", ", n.Recipe.Inputs.Keys.Select(k => _db.GetItem(k)?.Name ?? k)),
            }).ToList();

            Nodes = new ObservableCollection<ProductionNodeViewModel>(nodeVMs);

            // 统计
            TotalPower = graph.TotalPowerConsumption;
            TotalMachines = graph.TotalMachineCount;
            TotalLayers = graph.Nodes.Count > 0 ? graph.Nodes.Max(n => n.Layer) + 1 : 0;

            // 原始资源
            var rawList = graph.RawMaterialRequirements.Select(r => new RawMaterialRequirement
            {
                ItemId = r.Key,
                ItemName = _db.GetItem(r.Key)?.Name ?? r.Key,
                RatePerMinute = r.Value
            }).ToList();
            RawMaterials = new ObservableCollection<RawMaterialRequirement>(rawList);
        }
    }

    public partial class ProductionNodeViewModel : ObservableObject
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string MachineType { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Layer { get; set; }
        public string Position { get; set; } = string.Empty;
        public double PowerConsumption { get; set; }
        public string Outputs { get; set; } = string.Empty;
        public string Inputs { get; set; } = string.Empty;
    }

    public partial class RawMaterialRequirement : ObservableObject
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public double RatePerMinute { get; set; }
    }
}
