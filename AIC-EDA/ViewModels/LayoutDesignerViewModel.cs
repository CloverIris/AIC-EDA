using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIC_EDA.Models;
using AIC_EDA.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AIC_EDA.ViewModels
{
    public partial class LayoutDesignerViewModel : ObservableObject
    {
        [ObservableProperty]
        private FactoryLayout _layout = new();

        [ObservableProperty]
        private PlacedMachine? _selectedMachine;

        [ObservableProperty]
        private MachineType? _paletteSelection;

        [ObservableProperty]
        private ObservableCollection<MachinePaletteGroup> _paletteGroups = new();

        [ObservableProperty]
        private string _statusText = "Select a machine from the palette and click on the canvas to place.";

        [ObservableProperty]
        private bool _isPlacingMode;

        [ObservableProperty]
        private bool _isConnectMode;

        [ObservableProperty]
        private PlacedMachine? _connectSource;

        [ObservableProperty]
        private int _machineCount;

        [ObservableProperty]
        private string _categorySummary = string.Empty;

        public LayoutDesignerViewModel()
        {
            InitializePalette();
            UpdateStats();
        }

        private void InitializePalette()
        {
            var groups = new List<MachinePaletteGroup>
            {
                new("Resource Extraction", MachineCategory.Resource, new()
                {
                    MachineType.MiningRig,
                    MachineType.MiningRigMk2,
                    MachineType.HydraulicMiningRig,
                    MachineType.FluidPump,
                }),
                new("Processing", MachineCategory.Processing, new()
                {
                    MachineType.RefiningUnit,
                    MachineType.ShreddingUnit,
                    MachineType.GrindingUnit,
                    MachineType.MouldingUnit,
                    MachineType.FittingUnit,
                    MachineType.GearingUnit,
                    MachineType.FillingUnit,
                    MachineType.PackagingUnit,
                    MachineType.SeparatingUnit,
                    MachineType.ReactorCrucible,
                    MachineType.ExpandedCrucible,
                    MachineType.PurificationUnit,
                }),
                new("Agriculture", MachineCategory.Agriculture, new()
                {
                    MachineType.PlantingUnit,
                    MachineType.SeedPickingUnit,
                    MachineType.PlantWaterer,
                }),
                new("Power", MachineCategory.Power, new()
                {
                    MachineType.ProtocolCore,
                    MachineType.SubPAC,
                    MachineType.PowerPylon,
                    MachineType.RelayTower,
                    MachineType.ThermalBank,
                }),
                new("Logistics", MachineCategory.Logistics, new()
                {
                    MachineType.ConveyorBelt,
                    MachineType.Splitter,
                    MachineType.Merger,
                    MachineType.ProtocolStash,
                }),
            };

            PaletteGroups = new ObservableCollection<MachinePaletteGroup>(groups);
        }

        [RelayCommand]
        private void SelectPaletteMachine(MachineType type)
        {
            PaletteSelection = type;
            IsPlacingMode = true;
            StatusText = $"Placing: {type.GetDisplayName()}. Click on the canvas.";
        }

        [RelayCommand]
        private void PlaceMachine(int encodedPos)
        {
            if (PaletteSelection == null) return;
            int gridX = encodedPos / 10000;
            int gridY = encodedPos % 10000;

            var machine = new PlacedMachine
            {
                MachineType = PaletteSelection.Value,
                GridX = gridX,
                GridY = gridY,
            };

            if (Layout.AddMachine(machine))
            {
                SelectedMachine = machine;
                StatusText = $"Placed {machine.DisplayName} at ({gridX}, {gridY})";
                UpdateStats();
            }
            else
            {
                StatusText = "Cannot place: collision or out of bounds.";
            }
        }

        [RelayCommand]
        private void SelectMachine(Guid id)
        {
            SelectedMachine = Layout.Machines.FirstOrDefault(m => m.Id == id);
            IsPlacingMode = false;
            PaletteSelection = null;
            if (SelectedMachine != null)
            {
                StatusText = $"Selected: {SelectedMachine.DisplayName} at ({SelectedMachine.GridX}, {SelectedMachine.GridY})";
            }
        }

        [RelayCommand]
        private void MoveSelected(int encodedDelta)
        {
            if (SelectedMachine == null) return;
            // Encoding: dx * 100 + dy + 5000 (offset to handle negatives)
            int decoded = encodedDelta - 5000;
            int deltaX = decoded / 100;
            int deltaY = decoded % 100;
            // Handle negative modulo
            if (deltaY > 50) deltaY -= 100;
            var newX = SelectedMachine.GridX + deltaX;
            var newY = SelectedMachine.GridY + deltaY;
            if (Layout.MoveMachine(SelectedMachine.Id, newX, newY))
            {
                StatusText = $"Moved to ({newX}, {newY})";
                OnPropertyChanged(nameof(Layout));
            }
            else
            {
                StatusText = "Cannot move: collision or out of bounds.";
            }
        }

        [RelayCommand]
        private void RotateSelected()
        {
            if (SelectedMachine == null) return;
            if (Layout.RotateMachine(SelectedMachine.Id))
            {
                StatusText = $"Rotated to {SelectedMachine.Rotation} degrees";
                OnPropertyChanged(nameof(Layout));
            }
            else
            {
                StatusText = "Cannot rotate.";
            }
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            if (SelectedMachine == null) return;
            var name = SelectedMachine.DisplayName;
            if (Layout.RemoveMachine(SelectedMachine.Id))
            {
                SelectedMachine = null;
                StatusText = $"Deleted {name}";
                UpdateStats();
            }
        }

        [RelayCommand]
        private void DuplicateSelected()
        {
            if (SelectedMachine == null) return;
            var original = SelectedMachine;
            var copy = new PlacedMachine
            {
                MachineType = original.MachineType,
                GridX = original.GridX + original.GridWidth,
                GridY = original.GridY,
                Rotation = original.Rotation,
                Label = original.Label,
                RecipeId = original.RecipeId,
            };
            if (Layout.AddMachine(copy))
            {
                SelectedMachine = copy;
                StatusText = $"Duplicated {copy.DisplayName}";
                UpdateStats();
                OnPropertyChanged(nameof(Layout));
            }
            else
            {
                // Try below
                copy.GridX = original.GridX;
                copy.GridY = original.GridY + original.GridDepth;
                if (Layout.AddMachine(copy))
                {
                    SelectedMachine = copy;
                    StatusText = $"Duplicated {copy.DisplayName}";
                    UpdateStats();
                    OnPropertyChanged(nameof(Layout));
                }
                else
                {
                    StatusText = "Cannot duplicate: no space available.";
                }
            }
        }

        [RelayCommand]
        private void ToggleConnectMode()
        {
            IsConnectMode = !IsConnectMode;
            ConnectSource = null;
            IsPlacingMode = false;
            PaletteSelection = null;
            StatusText = IsConnectMode ? "Connect Mode: Click source machine, then target machine." : "Connect mode disabled.";
        }

        [RelayCommand]
        private void SetConnectSource(Guid machineId)
        {
            if (!IsConnectMode) return;
            var machine = Layout.Machines.FirstOrDefault(m => m.Id == machineId);
            if (machine == null) return;
            ConnectSource = machine;
            StatusText = $"Selected source: {machine.DisplayName}. Click target machine.";
        }

        [RelayCommand]
        private void ConnectToTarget(Guid machineId)
        {
            if (!IsConnectMode || ConnectSource == null) return;
            if (ConnectSource.Id == machineId)
            {
                StatusText = "Cannot connect machine to itself.";
                return;
            }
            var conn = new MachineConnection
            {
                SourceId = ConnectSource.Id,
                TargetId = machineId,
            };
            if (Layout.AddConnection(conn))
            {
                var target = Layout.Machines.FirstOrDefault(m => m.Id == machineId);
                StatusText = $"Connected {ConnectSource.DisplayName} -> {target?.DisplayName ?? "?"}";
                ConnectSource = null;
                OnPropertyChanged(nameof(Layout));
            }
            else
            {
                StatusText = "Connection already exists.";
            }
        }

        [RelayCommand]
        private void ClearConnections()
        {
            Layout.Connections.Clear();
            ConnectSource = null;
            StatusText = "All connections cleared.";
            OnPropertyChanged(nameof(Layout));
        }

        [RelayCommand]
        private void ClearLayout()
        {
            Layout.Clear();
            SelectedMachine = null;
            ConnectSource = null;
            StatusText = "Layout cleared.";
            UpdateStats();
        }

        [RelayCommand]
        private void SetGridSize(int size)
        {
            Layout.GridSize = size;
            OnPropertyChanged(nameof(Layout));
        }

        [RelayCommand]
        private async Task SaveLayout()
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("JSON Layout", new List<string> { ".json" });
            savePicker.SuggestedFileName = $"{Layout.Name.Replace(" ", "_")}_layout";

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await LayoutSerializer.SaveToFileAsync(Layout, file);
                StatusText = $"Layout saved to {file.Name}";
            }
        }

        [RelayCommand]
        private async Task LoadLayout()
        {
            var openPicker = new FileOpenPicker();
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".json");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    var loaded = await LayoutSerializer.LoadFromFileAsync(file);
                    Layout = loaded;
                    SelectedMachine = null;
                    UpdateStats();
                    StatusText = $"Layout loaded from {file.Name}";
                    OnPropertyChanged(nameof(Layout));
                }
                catch (Exception ex)
                {
                    StatusText = $"Failed to load: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private void ImportFromGraph()
        {
            var graph = App.CurrentGraph;
            if (graph == null || graph.Nodes.Count == 0)
            {
                StatusText = "No compiled graph available. Go to RTL Synthesis first.";
                return;
            }

            Layout.Clear();
            var planner = new Core.SpatialPlanner();
            planner.AutoLayout2D(graph);

            foreach (var node in graph.Nodes)
            {
                if (node.Position.HasValue)
                {
                    var machine = new PlacedMachine
                    {
                        MachineType = node.Recipe.Machine,
                        GridX = (int)node.Position.Value.X,
                        GridY = (int)node.Position.Value.Z,
                        Rotation = node.Rotation,
                        RecipeId = node.Recipe.Id,
                        Label = node.DisplayName,
                    };
                    Layout.Machines.Add(machine);
                }
            }

            Layout.ModifiedAt = DateTime.Now;
            SelectedMachine = null;
            UpdateStats();
            StatusText = $"Imported {Layout.Machines.Count} machines from compiled graph.";
            OnPropertyChanged(nameof(Layout));
        }

        private void UpdateStats()
        {
            MachineCount = Layout.Machines.Count;
            var counts = Layout.GetCategoryCounts();
            CategorySummary = string.Join(" | ", counts.Select(c => $"{c.Key}: {c.Value}"));
        }
    }

    /// <summary>
    /// 机器工具栏分组
    /// </summary>
    public class MachinePaletteGroup
    {
        public string Name { get; }
        public MachineCategory Category { get; }
        public List<MachineType> Machines { get; }

        public MachinePaletteGroup(string name, MachineCategory category, List<MachineType> machines)
        {
            Name = name;
            Category = category;
            Machines = machines;
        }
    }
}
