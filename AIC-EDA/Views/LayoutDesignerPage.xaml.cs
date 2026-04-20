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

        // Isometric view state
        private bool _isIsometric = false;
        private bool _showAllPowerRadii = false;
        private const double IsoCellWidth = 1.0;  // Will be scaled by zoom
        private const double IsoCellHeight = 0.5; // Half width for 2:1 isometric

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
            double baseW = CanvasGridW * _gridCellPixels * _zoomFactor;
            double baseH = CanvasGridH * _gridCellPixels * _zoomFactor;
            double w = baseW + _panOffset.X * 2;
            double h = baseH + _panOffset.Y * 2;

            // Guard against NaN, Infinity, and negative values which crash WinUI
            if (!double.IsFinite(w) || w < 0) w = baseW > 0 ? baseW : CanvasGridW * _gridCellPixels;
            if (!double.IsFinite(h) || h < 0) h = baseH > 0 ? baseH : CanvasGridH * _gridCellPixels;

            // Clamp to reasonable max to prevent overflow
            const double maxDim = 100000;
            if (w > maxDim) w = maxDim;
            if (h > maxDim) h = maxDim;

            DesignCanvas.Width = w;
            DesignCanvas.Height = h;
            CanvasInfoText.Text = $"Grid: {_gridCellPixels:F0}px | Zoom: {_zoomFactor * 100:F0}%";
        }

        // ===== REDRAW =====
        private void RedrawCanvas()
        {
            if (!_isLoaded) return;
            try
            {
                DesignCanvas.Children.Clear();
                UpdateCanvasSize();

                // Dark background plate to separate grid from window background
                var bgPlate = new Rectangle
                {
                    Width = DesignCanvas.Width,
                    Height = DesignCanvas.Height,
                    Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x0A, 0x0A, 0x0A)),
                };
                DesignCanvas.Children.Add(bgPlate);

                DrawGrid();
                DrawMachines();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LayoutDesigner] Redraw error: {ex.Message}\n{ex.StackTrace}");
                StatusTextBlock.Text = $"Render error: {ex.Message}";
                FlashErrorWarning();
            }
        }

        /// <summary>
        /// Calculate bounding box of all placed machines plus padding.
        /// Returns (minX, minY, maxX, maxY) in grid coordinates.
        /// </summary>
        private (int minX, int minY, int maxX, int maxY) GetGridBounds(int padding = 8)
        {
            var machines = ViewModel.Layout.Machines;
            if (machines.Count == 0)
            {
                // Default small area when no machines placed
                return (0, 0, Math.Min(20, CanvasGridW), Math.Min(12, CanvasGridH));
            }

            int minX = machines.Min(m => m.GridX);
            int minY = machines.Min(m => m.GridY);
            int maxX = machines.Max(m => m.GridX + m.GridWidth);
            int maxY = machines.Max(m => m.GridY + m.GridDepth);

            minX = Math.Max(0, minX - padding);
            minY = Math.Max(0, minY - padding);
            maxX = Math.Min(CanvasGridW, maxX + padding);
            maxY = Math.Min(CanvasGridH, maxY + padding);

            return (minX, minY, maxX, maxY);
        }

        private async void FlashErrorWarning()
        {
            try
            {
                ErrorFlashBorder.BorderThickness = new Thickness(4);
                await System.Threading.Tasks.Task.Delay(200);
                ErrorFlashBorder.BorderThickness = new Thickness(0);
                await System.Threading.Tasks.Task.Delay(150);
                ErrorFlashBorder.BorderThickness = new Thickness(4);
                await System.Threading.Tasks.Task.Delay(200);
                ErrorFlashBorder.BorderThickness = new Thickness(0);
            }
            catch { /* Ignore flash errors */ }
        }

        private void DrawGrid()
        {
            if (_isIsometric)
                DrawGridIsometric();
            else
                DrawGridTopDown();
        }

        private void DrawGridTopDown()
        {
            double cell = _gridCellPixels * _zoomFactor;
            var (minX, minY, maxX, maxY) = GetGridBounds(padding: 6);

            double width = (maxX - minX) * cell;
            double height = (maxY - minY) * cell;
            double originX = _panOffset.X + minX * cell;
            double originY = _panOffset.Y + minY * cell;

            // Subtle background fill for active area
            var bg = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = new SolidColorBrush(Color.FromArgb(0x18, 0x1A, 0x1A, 0x1A)),
            };
            Canvas.SetLeft(bg, originX);
            Canvas.SetTop(bg, originY);
            DesignCanvas.Children.Add(bg);

            // Vertical lines (dashed, dim)
            for (int i = minX; i <= maxX; i++)
            {
                var line = new Line
                {
                    X1 = _panOffset.X + i * cell,
                    Y1 = originY,
                    X2 = _panOffset.X + i * cell,
                    Y2 = originY + height,
                    Stroke = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)),
                    StrokeThickness = (i % 5 == 0) ? 1.0 : 0.5,
                    StrokeDashArray = new DoubleCollection { 2, 4 },
                };
                DesignCanvas.Children.Add(line);
            }

            // Horizontal lines
            for (int i = minY; i <= maxY; i++)
            {
                var line = new Line
                {
                    X1 = originX,
                    Y1 = _panOffset.Y + i * cell,
                    X2 = originX + width,
                    Y2 = _panOffset.Y + i * cell,
                    Stroke = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)),
                    StrokeThickness = (i % 5 == 0) ? 1.0 : 0.5,
                    StrokeDashArray = new DoubleCollection { 2, 4 },
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
            Canvas.SetLeft(border, originX);
            Canvas.SetTop(border, originY);
            DesignCanvas.Children.Add(border);
        }

        private void DrawGridIsometric()
        {
            double cell = _gridCellPixels * _zoomFactor;
            double isoW = cell;
            double isoH = cell * 0.5;

            var (minGx, minGy, maxGx, maxGy) = GetGridBounds(padding: 8);

            // Use global center (same as machine drawing) so grid aligns with placed machines
            double centerX = _panOffset.X + CanvasGridW * isoW * 0.5;
            double originY = _panOffset.Y;

            // Dim grid brushes
            var gridBrush = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
            var majorGridBrush = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));

            // Limit diagonal line indices to machine area to avoid cage effect
            int iMin1 = minGx - maxGy;
            int iMax1 = maxGx - minGy;
            int iMin2 = minGx + minGy;
            int iMax2 = maxGx + maxGy;

            // Diagonal lines (\\ direction) — x - y = constant
            for (int i = iMin1; i <= iMax1; i++)
            {
                double x1 = centerX + i * isoW * 0.5;
                double y1 = originY + Math.Max(0, -i) * isoH * 0.5;
                double x2 = centerX + (i + CanvasGridH) * isoW * 0.5;
                double y2 = originY + CanvasGridH * isoH * 0.5 + Math.Min(0, -i) * isoH * 0.5;

                var line = new Line
                {
                    X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                    Stroke = (i % 5 == 0) ? majorGridBrush : gridBrush,
                    StrokeThickness = (i % 5 == 0) ? 1.0 : 0.5,
                    StrokeDashArray = new DoubleCollection { 2, 4 },
                };
                DesignCanvas.Children.Add(line);
            }

            // Diagonal lines (// direction) — x + y = constant
            for (int i = iMin2; i <= iMax2; i++)
            {
                double x1 = centerX + (i - CanvasGridH) * isoW * 0.5;
                double y1 = originY + Math.Max(0, CanvasGridH - i) * isoH * 0.5;
                double x2 = centerX + i * isoW * 0.5;
                double y2 = originY + Math.Min(CanvasGridH, i) * isoH * 0.5;

                var line = new Line
                {
                    X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                    Stroke = (i % 5 == 0) ? majorGridBrush : gridBrush,
                    StrokeThickness = (i % 5 == 0) ? 1.0 : 0.5,
                    StrokeDashArray = new DoubleCollection { 2, 4 },
                };
                DesignCanvas.Children.Add(line);
            }

            // Border diamond (around machine area)
            double borderLeftX = centerX + (minGx - maxGy) * isoW * 0.5;
            double borderRightX = centerX + (maxGx - minGy) * isoW * 0.5;
            double borderTopY = originY + (minGx + minGy) * isoH * 0.5;
            double borderBottomY = originY + (maxGx + maxGy) * isoH * 0.5;
            double borderMidX = (borderLeftX + borderRightX) * 0.5;

            var borderPoints = new PointCollection
            {
                new Point(borderMidX, borderTopY),
                new Point(borderRightX, (borderTopY + borderBottomY) * 0.5),
                new Point(borderMidX, borderBottomY),
                new Point(borderLeftX, (borderTopY + borderBottomY) * 0.5),
            };
            var border = new Polygon
            {
                Points = borderPoints,
                Stroke = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xD6, 0x00)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(0x10, 0x1A, 0x1A, 0x1A)),
            };
            DesignCanvas.Children.Add(border);
        }

        private void DrawMachines()
        {
            if (_isIsometric)
            {
                DrawConnectionsIsometric();
                DrawMachinesIsometric();
            }
            else
            {
                DrawConnectionsTopDown();
                DrawMachinesTopDown();
            }
        }

        private void DrawConnectionsTopDown()
        {
            double cell = _gridCellPixels * _zoomFactor;
            foreach (var conn in ViewModel.Layout.Connections)
            {
                var source = ViewModel.Layout.Machines.FirstOrDefault(m => m.Id == conn.SourceId);
                var target = ViewModel.Layout.Machines.FirstOrDefault(m => m.Id == conn.TargetId);
                if (source == null || target == null) continue;

                double x1 = _panOffset.X + (source.GridX + source.GridWidth / 2.0) * cell;
                double y1 = _panOffset.Y + (source.GridY + source.GridDepth / 2.0) * cell;
                double x2 = _panOffset.X + (target.GridX + target.GridWidth / 2.0) * cell;
                double y2 = _panOffset.Y + (target.GridY + target.GridDepth / 2.0) * cell;

                var color = conn.Type == ConnectionType.Pipe
                    ? Color.FromArgb(0xAA, 0x1E, 0x90, 0xFF)
                    : Color.FromArgb(0xAA, 0xFF, 0xD6, 0x00);

                // Connection line
                var line = new Line
                {
                    X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                };
                DesignCanvas.Children.Add(line);

                // Arrowhead at target
                double angle = Math.Atan2(y2 - y1, x2 - x1);
                double arrowLen = 8;
                double arrowAngle = 0.5;
                var arrow = new Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(x2, y2),
                        new Point(x2 - arrowLen * Math.Cos(angle - arrowAngle), y2 - arrowLen * Math.Sin(angle - arrowAngle)),
                        new Point(x2 - arrowLen * Math.Cos(angle + arrowAngle), y2 - arrowLen * Math.Sin(angle + arrowAngle)),
                    },
                    Fill = new SolidColorBrush(color),
                };
                DesignCanvas.Children.Add(arrow);
            }

            // Highlight connect source
            if (ViewModel.IsConnectMode && ViewModel.ConnectSource != null)
            {
                var src = ViewModel.ConnectSource;
                double cx = _panOffset.X + (src.GridX + src.GridWidth / 2.0) * cell;
                double cy = _panOffset.Y + (src.GridY + src.GridDepth / 2.0) * cell;
                var pulse = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Stroke = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xD6, 0x00)),
                    StrokeThickness = 2,
                    Fill = null,
                };
                Canvas.SetLeft(pulse, cx - 10);
                Canvas.SetTop(pulse, cy - 10);
                DesignCanvas.Children.Add(pulse);
            }
        }

        private void DrawConnectionsIsometric()
        {
            double cell = _gridCellPixels * _zoomFactor;
            double isoW = cell;
            double isoH = cell * 0.5;
            double centerX = _panOffset.X + CanvasGridW * isoW * 0.5;

            foreach (var conn in ViewModel.Layout.Connections)
            {
                var source = ViewModel.Layout.Machines.FirstOrDefault(m => m.Id == conn.SourceId);
                var target = ViewModel.Layout.Machines.FirstOrDefault(m => m.Id == conn.TargetId);
                if (source == null || target == null) continue;

                double baseIsoX1 = (source.GridX - source.GridY) * isoW * 0.5;
                double baseIsoY1 = (source.GridX + source.GridY) * isoH * 0.5;
                double baseIsoX2 = (target.GridX - target.GridY) * isoW * 0.5;
                double baseIsoY2 = (target.GridX + target.GridY) * isoH * 0.5;

                double x1 = centerX + baseIsoX1;
                double y1 = _panOffset.Y + baseIsoY1;
                double x2 = centerX + baseIsoX2;
                double y2 = _panOffset.Y + baseIsoY2;

                var color = conn.Type == ConnectionType.Pipe
                    ? Color.FromArgb(0xAA, 0x1E, 0x90, 0xFF)
                    : Color.FromArgb(0xAA, 0xFF, 0xD6, 0x00);

                var line = new Line
                {
                    X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                };
                DesignCanvas.Children.Add(line);
            }
        }

        private void DrawMachinesTopDown()
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

                // Power radius visualization for selected machine or all if toggled
                if (isSelected || _showAllPowerRadii)
                {
                    DrawPowerRadius(machine);
                }
            }
        }

        private void DrawMachinesIsometric()
        {
            double cell = _gridCellPixels * _zoomFactor;
            double isoW = cell;        // width of a diamond tile
            double isoH = cell * 0.5;  // height of a diamond tile (2:1 ratio)
            double blockH = cell * 0.4; // pseudo-height of the block

            // Sort machines by (gridX + gridY) for proper depth ordering (painter's algorithm)
            var sortedMachines = ViewModel.Layout.Machines.OrderBy(m => m.GridX + m.GridY).ToList();

            foreach (var machine in sortedMachines)
            {
                bool isSelected = ViewModel.SelectedMachine?.Id == machine.Id;
                var color = CategoryColors.GetValueOrDefault(machine.Category, Colors.Gray);

                // Isometric base position (bottom center of the block footprint)
                double baseIsoX = (machine.GridX - machine.GridY) * isoW * 0.5;
                double baseIsoY = (machine.GridX + machine.GridY) * isoH * 0.5;

                double footprintW = machine.GridWidth * isoW;
                double footprintH = machine.GridDepth * isoH;

                // Screen position
                double screenX = _panOffset.X + baseIsoX + CanvasGridW * isoW * 0.5;
                double screenY = _panOffset.Y + baseIsoY;

                // Color variants for faces
                var topColor = Color.FromArgb(0xEE,
                    (byte)Math.Min(255, color.R + 30),
                    (byte)Math.Min(255, color.G + 30),
                    (byte)Math.Min(255, color.B + 30));
                var leftColor = Color.FromArgb(0xDD,
                    (byte)Math.Max(0, color.R - 20),
                    (byte)Math.Max(0, color.G - 20),
                    (byte)Math.Max(0, color.B - 20));
                var rightColor = Color.FromArgb(0xCC,
                    (byte)Math.Max(0, color.R - 40),
                    (byte)Math.Max(0, color.G - 40),
                    (byte)Math.Max(0, color.B - 40));

                // Selected highlight glow
                if (isSelected)
                {
                    var glowPoints = new PointCollection
                    {
                        new Point(screenX, screenY - blockH - footprintH * 0.5 - 4),
                        new Point(screenX + footprintW * 0.5 + 4, screenY - blockH - 4),
                        new Point(screenX, screenY - blockH + footprintH * 0.5 + 4),
                        new Point(screenX - footprintW * 0.5 - 4, screenY - blockH - 4),
                    };
                    var glow = new Polygon
                    {
                        Points = glowPoints,
                        Fill = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xD6, 0x00)),
                    };
                    DesignCanvas.Children.Add(glow);
                }

                // Left face
                var leftFace = new Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(screenX - footprintW * 0.5, screenY - footprintH * 0.5),
                        new Point(screenX, screenY - blockH - footprintH * 0.5),
                        new Point(screenX, screenY - blockH + footprintH * 0.5),
                        new Point(screenX - footprintW * 0.5, screenY + footprintH * 0.5),
                    },
                    Fill = new SolidColorBrush(leftColor),
                    Stroke = isSelected ? new SolidColorBrush(Colors.White) : null,
                    StrokeThickness = isSelected ? 1 : 0,
                };
                DesignCanvas.Children.Add(leftFace);

                // Right face
                var rightFace = new Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(screenX + footprintW * 0.5, screenY - footprintH * 0.5),
                        new Point(screenX, screenY - blockH - footprintH * 0.5),
                        new Point(screenX, screenY - blockH + footprintH * 0.5),
                        new Point(screenX + footprintW * 0.5, screenY + footprintH * 0.5),
                    },
                    Fill = new SolidColorBrush(rightColor),
                    Stroke = isSelected ? new SolidColorBrush(Colors.White) : null,
                    StrokeThickness = isSelected ? 1 : 0,
                };
                DesignCanvas.Children.Add(rightFace);

                // Top face (diamond)
                var topFace = new Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(screenX, screenY - blockH - footprintH * 0.5),
                        new Point(screenX + footprintW * 0.5, screenY - blockH),
                        new Point(screenX, screenY - blockH + footprintH * 0.5),
                        new Point(screenX - footprintW * 0.5, screenY - blockH),
                    },
                    Fill = new SolidColorBrush(topColor),
                    Stroke = isSelected ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(0x60, 255, 255, 255)),
                    StrokeThickness = isSelected ? 2 : 1,
                };
                DesignCanvas.Children.Add(topFace);

                // Label on top face
                if (footprintW > 30)
                {
                    var tb = new TextBlock
                    {
                        Text = machine.DisplayName,
                        FontSize = Math.Max(7, cell * 0.22),
                        Foreground = new SolidColorBrush(Colors.White),
                        FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = footprintW * 0.8,
                        TextAlignment = TextAlignment.Center,
                    };
                    Canvas.SetLeft(tb, screenX - footprintW * 0.4);
                    Canvas.SetTop(tb, screenY - blockH - 6);
                    DesignCanvas.Children.Add(tb);
                }
            }
        }

        private void DrawPowerRadius(PlacedMachine machine)
        {
            var spec = machine.Spec;
            if (spec == null || spec.PowerRadius <= 0) return;

            double cell = _gridCellPixels * _zoomFactor;
            double radiusPixels = spec.PowerRadius * cell;

            double x = _panOffset.X + (machine.GridX + machine.GridWidth / 2.0) * cell;
            double y = _panOffset.Y + (machine.GridY + machine.GridDepth / 2.0) * cell;

            var circle = new Ellipse
            {
                Width = radiusPixels * 2,
                Height = radiusPixels * 2,
                Stroke = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xD7, 0x00)),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xD7, 0x00)),
                StrokeDashArray = new DoubleCollection { 4, 4 },
            };
            Canvas.SetLeft(circle, x - radiusPixels);
            Canvas.SetTop(circle, y - radiusPixels);
            DesignCanvas.Children.Add(circle);
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
                if (ViewModel.IsConnectMode)
                {
                    if (ViewModel.ConnectSource == null)
                    {
                        ViewModel.SetConnectSourceCommand.Execute(existing.Id);
                    }
                    else
                    {
                        ViewModel.ConnectToTargetCommand.Execute(existing.Id);
                    }
                }
                else
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
                if (ViewModel.IsConnectMode)
                {
                    ViewModel.ConnectSource = null;
                }
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
                case VirtualKey.D:
                    if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                    {
                        ViewModel.DuplicateSelectedCommand.Execute(null);
                        e.Handled = true;
                    }
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

        private readonly Dictionary<MachineType, Button> _paletteButtons = new();

        private void MachineTypeNameText_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.Tag is MachineType type)
            {
                tb.Text = type.GetDisplayName();
                var color = CategoryColors.GetValueOrDefault(type.GetCategory(), Colors.Gray);
                // Find the parent FontIcon and set its color
                if (tb.Parent is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is FontIcon icon)
                        {
                            icon.Foreground = new SolidColorBrush(color);
                        }
                    }
                }
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

        private void ImportFromGraph_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ImportFromGraphCommand.Execute(null);
        }

        private void ClearConnections_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearConnectionsCommand.Execute(null);
        }

        private async void SaveLayout_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveLayoutCommand.ExecuteAsync(null);
            RedrawCanvas();
        }

        private async void LoadLayout_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadLayoutCommand.ExecuteAsync(null);
            RedrawCanvas();
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

        private void IsometricToggle_Click(object sender, RoutedEventArgs e)
        {
            _isIsometric = IsometricToggle.IsChecked == true;
            RedrawCanvas();
        }

        private void ShowPowerRadiusToggle_Click(object sender, RoutedEventArgs e)
        {
            _showAllPowerRadii = ShowPowerRadiusToggle.IsChecked == true;
            RedrawCanvas();
        }

        private void ConnectModeToggle_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleConnectModeCommand.Execute(null);
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

            // Recipe info
            bool hasRecipe = !string.IsNullOrEmpty(machine.RecipeId);
            RecipeInfoGrid.Visibility = hasRecipe ? Visibility.Visible : Visibility.Collapsed;
            if (hasRecipe)
            {
                PropRecipeText.Text = machine.RecipeId;
            }
            PropLabelText.Text = !string.IsNullOrEmpty(machine.Label) ? machine.Label : "-";
        }

        private void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DuplicateSelectedCommand.Execute(null);
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
