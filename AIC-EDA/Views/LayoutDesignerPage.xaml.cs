using AIC_EDA.Models;
using AIC_EDA.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Text;

namespace AIC_EDA.Views
{
    public sealed partial class LayoutDesignerPage : Page
    {
        public LayoutDesignerViewModel ViewModel { get; } = new();

        // Canvas state
        private double _gridCellPixels = 30.0;
        private double _zoomFactor = 1.0;
        private Point _panOffset = new(20, 20);
        private bool _isDragging = false;
        private bool _isPanning = false;
        private Point _lastPointerPos;
        private PlacedMachine? _dragMachine = null;
        private Point _dragStartPos;
        private int _dragStartGridX;
        private int _dragStartGridY;

        // Canvas dimensions in grid units
        private const int CanvasGridW = 60;
        private const int CanvasGridH = 40;

        // Category colors (same as LayoutPreviewPage)
        private static readonly Dictionary<MachineCategory, Color> CategoryColors = new()
        {
            [MachineCategory.Resource] = Color.FromArgb(0xFF, 0x1E, 0x90, 0xFF),
            [MachineCategory.Processing] = Color.FromArgb(0xFF, 0xFF, 0xA5, 0x00),
            [MachineCategory.Logistics] = Color.FromArgb(0xFF, 0x80, 0x80, 0x80),
            [MachineCategory.Power] = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00),
            [MachineCategory.Agriculture] = Color.FromArgb(0xFF, 0x32, 0xCD, 0x32),
        };

        private bool _isLoaded = false;

        public LayoutDesignerPage()
        {
            this.InitializeComponent();
            this.Loaded += LayoutDesignerPage_Loaded;
            this.KeyDown += LayoutDesignerPage_KeyDown;
            GridSizeCombo.SelectedIndex = 1; // 30px default
        }

        private void LayoutDesignerPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            UpdateCanvasSize();
            RedrawCanvas();

            // Listen for ViewModel property changes to redraw
            ViewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(ViewModel.Layout) ||
                    args.PropertyName == nameof(ViewModel.SelectedMachine))
                {
                    RedrawCanvas();
                    UpdatePropertiesPanel();
                }
            };
        }

        // ===== CANVAS SIZE =====
        private void UpdateCanvasSize()
        {
            double w = CanvasGridW * _gridCellPixels * _zoomFactor + _panOffset.X * 2;
            double h = CanvasGridH * _gridCellPixels * _zoomFactor + _panOffset.Y * 2;
            DesignCanvas.Width = w;
            DesignCanvas.Height = h;
            CanvasInfoText.Text = $"Grid: {_gridCellPixels:F0}px | Zoom: {_zoomFactor * 100:F0}%";
        }

        // ===== REDRAW =====
        private void RedrawCanvas()
        {
            if (!_isLoaded) return;
            DesignCanvas.Children.Clear();
            UpdateCanvasSize();

            DrawGrid();
            DrawMachines();
        }

        private void DrawGrid()
        {
            double cell = _gridCellPixels * _zoomFactor;
            double width = CanvasGridW * cell;
            double height = CanvasGridH * cell;

            // Background
            var bg = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x1A, 0x1A, 0x1A)),
            };
            Canvas.SetLeft(bg, _panOffset.X);
            Canvas.SetTop(bg, _panOffset.Y);
            DesignCanvas.Children.Add(bg);

            // Vertical lines
            for (int i = 0; i <= CanvasGridW; i++)
            {
                var line = new Line
                {
                    X1 = _panOffset.X + i * cell,
                    Y1 = _panOffset.Y,
                    X2 = _panOffset.X + i * cell,
                    Y2 = _panOffset.Y + height,
                    Stroke = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
                    StrokeThickness = (i % 5 == 0) ? 1.5 : 0.5,
                };
                DesignCanvas.Children.Add(line);
            }

            // Horizontal lines
            for (int i = 0; i <= CanvasGridH; i++)
            {
                var line = new Line
                {
                    X1 = _panOffset.X,
                    Y1 = _panOffset.Y + i * cell,
                    X2 = _panOffset.X + width,
                    Y2 = _panOffset.Y + i * cell,
                    Stroke = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
                    StrokeThickness = (i % 5 == 0) ? 1.5 : 0.5,
                };
                DesignCanvas.Children.Add(line);
            }

            // Border
            var border = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xD6, 0x00)),
                StrokeThickness = 2,
                Fill = null,
            };
            Canvas.SetLeft(border, _panOffset.X);
            Canvas.SetTop(border, _panOffset.Y);
            DesignCanvas.Children.Add(border);
        }

        private void DrawMachines()
        {
            double cell = _gridCellPixels * _zoomFactor;

            foreach (var machine in ViewModel.Layout.Machines)
            {
                bool isSelected = ViewModel.SelectedMachine?.Id == machine.Id;
                var color = CategoryColors.GetValueOrDefault(machine.Category, Colors.Gray);

                double x = _panOffset.X + machine.GridX * cell;
                double y = _panOffset.Y + machine.GridY * cell;
                double w = machine.GridWidth * cell;
                double h = machine.GridDepth * cell;

                // Glow/shadow for selected
                if (isSelected)
                {
                    var glow = new Rectangle
                    {
                        Width = w + 8,
                        Height = h + 8,
                        RadiusX = 4,
                        RadiusY = 4,
                        Fill = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xD6, 0x00)),
                    };
                    Canvas.SetLeft(glow, x - 4);
                    Canvas.SetTop(glow, y - 4);
                    DesignCanvas.Children.Add(glow);
                }

                // Main block
                var rect = new Rectangle
                {
                    Width = w,
                    Height = h,
                    RadiusX = 3,
                    RadiusY = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(0xCC, color.R, color.G, color.B)),
                    Stroke = isSelected
                        ? new SolidColorBrush(Colors.White)
                        : new SolidColorBrush(Color.FromArgb(0x80, color.R, color.G, color.B)),
                    StrokeThickness = isSelected ? 2 : 1,
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                DesignCanvas.Children.Add(rect);

                // Label
                if (w > 30 && h > 20)
                {
                    var tb = new TextBlock
                    {
                        Text = machine.DisplayName,
                        FontSize = Math.Max(8, cell * 0.3),
                        Foreground = new SolidColorBrush(Colors.White),
                        FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = w - 4,
                        TextAlignment = TextAlignment.Center,
                    };
                    Canvas.SetLeft(tb, x + 2);
                    Canvas.SetTop(tb, y + h / 2 - 7);
                    DesignCanvas.Children.Add(tb);
                }
            }
        }

        // ===== POINTER INTERACTION =====
        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pos = e.GetCurrentPoint(DesignCanvas).Position;
            var properties = e.GetCurrentPoint(DesignCanvas).Properties;

            if (properties.IsMiddleButtonPressed)
            {
                _isPanning = true;
                _lastPointerPos = pos;
                DesignCanvas.CapturePointer(e.Pointer);
                return;
            }

            int gridX = (int)Math.Floor((pos.X - _panOffset.X) / (_gridCellPixels * _zoomFactor));
            int gridY = (int)Math.Floor((pos.Y - _panOffset.Y) / (_gridCellPixels * _zoomFactor));

            // Check if clicking on an existing machine
            var existing = ViewModel.Layout.GetMachineAt(gridX, gridY);

            if (existing != null)
            {
                // Select existing machine
                ViewModel.SelectMachineCommand.Execute(existing.Id);
                _dragMachine = existing;
                _dragStartPos = pos;
                _dragStartGridX = existing.GridX;
                _dragStartGridY = existing.GridY;
                _isDragging = true;
                DesignCanvas.CapturePointer(e.Pointer);
            }
            else if (ViewModel.IsPlacingMode && ViewModel.PaletteSelection.HasValue)
            {
                // Place new machine
                if (gridX >= 0 && gridY >= 0 && gridX < CanvasGridW && gridY < CanvasGridH)
                {
                    ViewModel.PlaceMachineCommand.Execute(gridX * 10000 + gridY); // Encode as int
                }
            }
            else
            {
                // Deselect
                ViewModel.SelectedMachine = null;
                ViewModel.IsPlacingMode = false;
                ViewModel.PaletteSelection = null;
            }

            e.Handled = true;
        }

        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var pos = e.GetCurrentPoint(DesignCanvas).Position;

            if (_isPanning)
            {
                double dx = pos.X - _lastPointerPos.X;
                double dy = pos.Y - _lastPointerPos.Y;
                _panOffset = new Point(_panOffset.X + dx, _panOffset.Y + dy);
                _lastPointerPos = pos;
                RedrawCanvas();
                return;
            }

            if (_isDragging && _dragMachine != null)
            {
                double cell = _gridCellPixels * _zoomFactor;
                int gridX = (int)Math.Round((pos.X - _panOffset.X) / cell - (_dragMachine.GridWidth / 2.0));
                int gridY = (int)Math.Round((pos.Y - _panOffset.Y) / cell - (_dragMachine.GridDepth / 2.0));

                if (gridX != _dragMachine.GridX || gridY != _dragMachine.GridY)
                {
                    if (ViewModel.Layout.MoveMachine(_dragMachine.Id, gridX, gridY))
                    {
                        RedrawCanvas();
                    }
                }
                return;
            }

            // Update hover info
            int hoverGx = (int)Math.Floor((pos.X - _panOffset.X) / (_gridCellPixels * _zoomFactor));
            int hoverGy = (int)Math.Floor((pos.Y - _panOffset.Y) / (_gridCellPixels * _zoomFactor));
            var hoverMachine = ViewModel.Layout.GetMachineAt(hoverGx, hoverGy);
        }

        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
            _isPanning = false;
            _dragMachine = null;
            DesignCanvas.ReleasePointerCapture(e.Pointer);
        }

        private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(DesignCanvas).Properties.MouseWheelDelta;
            if (delta > 0)
                _zoomFactor = Math.Min(_zoomFactor * 1.1, 3.0);
            else
                _zoomFactor = Math.Max(_zoomFactor / 1.1, 0.3);

            RedrawCanvas();
            e.Handled = true;
        }

        // ===== KEYBOARD =====
        private void LayoutDesignerPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Delete:
                    ViewModel.DeleteSelectedCommand.Execute(null);
                    e.Handled = true;
                    break;
                case VirtualKey.R:
                    ViewModel.RotateSelectedCommand.Execute(null);
                    e.Handled = true;
                    break;
                case VirtualKey.Left:
                    ViewModel.MoveSelectedCommand.Execute(4900); // (-1, 0) encoded
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    ViewModel.MoveSelectedCommand.Execute(5100); // (1, 0) encoded
                    e.Handled = true;
                    break;
                case VirtualKey.Up:
                    ViewModel.MoveSelectedCommand.Execute(4999); // (0, -1) encoded
                    e.Handled = true;
                    break;
                case VirtualKey.Down:
                    ViewModel.MoveSelectedCommand.Execute(5001); // (0, 1) encoded
                    e.Handled = true;
                    break;
                case VirtualKey.Escape:
                    ViewModel.SelectedMachine = null;
                    ViewModel.IsPlacingMode = false;
                    ViewModel.PaletteSelection = null;
                    e.Handled = true;
                    break;
            }
        }

        // ===== UI EVENT HANDLERS =====
        private void PaletteMachine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MachineType type)
            {
                ViewModel.SelectPaletteMachineCommand.Execute(type);
            }
        }

        private void MachineTypeNameText_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.Tag is MachineType type)
            {
                tb.Text = type.GetDisplayName();
            }
        }

        private void GridSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridSizeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (int.TryParse(tag, out int size))
                {
                    _gridCellPixels = size;
                    RedrawCanvas();
                }
            }
        }

        private void ClearLayout_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearLayoutCommand.Execute(null);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Min(_zoomFactor * 1.2, 3.0);
            RedrawCanvas();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Max(_zoomFactor / 1.2, 0.3);
            RedrawCanvas();
        }

        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = 1.0;
            _panOffset = new Point(20, 20);
            RedrawCanvas();
        }

        // ===== PROPERTY PANEL =====
        private void UpdatePropertiesPanel()
        {
            var machine = ViewModel.SelectedMachine;
            if (machine == null)
            {
                NoSelectionPanel.Visibility = Visibility.Visible;
                PropertiesPanel.Visibility = Visibility.Collapsed;
                return;
            }

            NoSelectionPanel.Visibility = Visibility.Collapsed;
            PropertiesPanel.Visibility = Visibility.Visible;

            PropNameText.Text = machine.DisplayName;
            PropCategoryText.Text = machine.Category.ToString();
            PropPositionText.Text = $"({machine.GridX}, {machine.GridY})";
            PropSizeText.Text = $"{machine.GridWidth}x{machine.GridDepth}";
            PropRotationText.Text = $"{machine.Rotation} degrees";
            var spec = machine.Spec;
            PropPowerText.Text = spec?.PowerRadius > 0 ? $"{spec.PowerRadius:F0} m" : "N/A";
        }

        private void Rotate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RotateSelectedCommand.Execute(null);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteSelectedCommand.Execute(null);
        }

        private void MoveLeft_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MoveSelectedCommand.Execute(4900);
        }

        private void MoveRight_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MoveSelectedCommand.Execute(5100);
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MoveSelectedCommand.Execute(4999);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MoveSelectedCommand.Execute(5001);
        }
    }
}
