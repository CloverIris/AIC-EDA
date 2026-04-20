using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        private void ClearLayout()
        {
            Layout.Clear();
            SelectedMachine = null;
            StatusText = "Layout cleared.";
            UpdateStats();
        }

        [RelayCommand]
        private void SetGridSize(int size)
        {
            Layout.GridSize = size;
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
