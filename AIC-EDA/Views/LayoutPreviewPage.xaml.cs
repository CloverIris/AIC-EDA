using AIC_EDA.Core;
using AIC_EDA.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;

namespace AIC_EDA.Views
{
    public sealed partial class LayoutPreviewPage : Page
    {
        // ===== Grid & View State =====
        private double _gridCellPixels = 40.0;      // Base grid cell size: 40 or 70
        private double _zoomFactor = 1.0;           // Zoom multiplier: 0.5 - 3.0
        private Point _panOffset = new(60, 60);     // Pan offset in screen pixels
        private bool _isPanning = false;
        private Point _lastPointerPos;
        private double _canvasWidth = 800;
        private double _canvasHeight = 600;

        // ===== Colors by category (Tetris-like blocks) =====
        private static readonly Dictionary<MachineCategory, Color> CategoryColors = new()
        {
            [MachineCategory.Resource] = Color.FromArgb(0xFF, 0x1E, 0x90, 0xFF),      // DodgerBlue
            [MachineCategory.Processing] = Color.FromArgb(0xFF, 0xFF, 0xA5, 0x00),    // Orange
            [MachineCategory.Logistics] = Color.FromArgb(0xFF, 0x80, 0x80, 0x80),     // Gray
            [MachineCategory.Power] = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00),         // Gold
            [MachineCategory.Agriculture] = Color.FromArgb(0xFF, 0x32, 0xCD, 0x32),   // LimeGreen
        };

        private static readonly Dictionary<MachineCategory, Color> CategoryGlowColors = new()
        {
            [MachineCategory.Resource] = Color.FromArgb(0x40, 0x1E, 0x90, 0xFF),
            [MachineCategory.Processing] = Color.FromArgb(0x40, 0xFF, 0xA5, 0x00),
            [MachineCategory.Logistics] = Color.FromArgb(0x40, 0x80, 0x80, 0x80),
            [MachineCategory.Power] = Color.FromArgb(0x40, 0xFF, 0xD7, 0x00),
            [MachineCategory.Agriculture] = Color.FromArgb(0x40, 0x32, 0xCD, 0x32),
        };

        private bool _isLoaded = false;

        public LayoutPreviewPage()
        {
            this.InitializeComponent();
            this.Loaded += LayoutPreviewPage_Loaded;
            GridSizeCombo.SelectedIndex = 0;
        }

        private void LayoutPreviewPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            // Set canvas size from parent container
            UpdateCanvasSize();
            DrawLayout();
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvasSize();
            DrawLayout();
        }

        private void UpdateCanvasSize()
        {
            // Canvas doesn't auto-size; get dimensions from parent Border
            if (LayoutCanvas.Parent is FrameworkElement parent)
            {
                _canvasWidth = parent.ActualWidth;
                _canvasHeight = parent.ActualHeight;
            }
            // Also update Canvas explicit size so it can receive pointer events across full area
            if (_canvasWidth > 0 && _canvasHeight > 0)
            {
                LayoutCanvas.Width = _canvasWidth;
                LayoutCanvas.Height = _canvasHeight;
            }
        }

        // ===== PUBLIC API for MainWindow toolbar =====
        public void ZoomIn()
        {
            _zoomFactor = Math.Min(_zoomFactor * 1.2, 3.0);
            DrawLayout();
        }

        public void ZoomOut()
        {
            _zoomFactor = Math.Max(_zoomFactor / 1.2, 0.3);
            DrawLayout();
        }

        public void ResetView()
        {
            _zoomFactor = 1.0;
            _panOffset = new Point(60, 60);
            DrawLayout();
        }

        // ===== MAIN DRAW ENTRY =====
        public void DrawLayout()
        {
            if (!_isLoaded) return;
            if (PlaceholderPanel == null || LayoutCanvas == null) return;

            var graph = App.CurrentGraph;
            if (graph == null || graph.Nodes.Count == 0)
            {
                PlaceholderPanel.Visibility = Visibility.Visible;
                LayoutCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            PlaceholderPanel.Visibility = Visibility.Collapsed;
            LayoutCanvas.Visibility = Visibility.Visible;

            LayoutCanvas.Children.Clear();

            if (ShowGridToggle?.IsChecked == true)
            {
                DrawGrid();
            }

            if (ShowConnectionsToggle?.IsChecked == true)
            {
                DrawConveyorBelts(graph);
            }

            DrawMachines(graph);

            // Update stats text
            GridInfoText.Text = $"Grid: {(int)_gridCellPixels} px";
            ZoomText.Text = $"Zoom: {(int)(_zoomFactor * 100)}%";
            StatsText.Text = $"{graph.Nodes.Count} nodes, {graph.Edges.Count} belts";
        }

        // ===== GRID DRAWING (Full Canvas Coverage) =====
        private void DrawGrid()
        {
            double cellSize = _gridCellPixels * _zoomFactor;
            if (cellSize < 2) return;

            // Calculate visible grid range based on canvas size + pan offset
            int startCol = (int)Math.Floor(-_panOffset.X / cellSize) - 1;
            int endCol = (int)Math.Ceiling((_canvasWidth - _panOffset.X) / cellSize) + 1;
            int startRow = (int)Math.Floor(-_panOffset.Y / cellSize) - 1;
            int endRow = (int)Math.Ceiling((_canvasHeight - _panOffset.Y) / cellSize) + 1;

            var minorBrush = new SolidColorBrush(Color.FromArgb(0x20, 0x80, 0x80, 0x80));
            var majorBrush = new SolidColorBrush(Color.FromArgb(0x45, 0x80, 0x80, 0x80));
            var axisBrush = new SolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xD6, 0x00)); // Yellow axis

            for (int c = startCol; c <= endCol; c++)
            {
                double x = _panOffset.X + c * cellSize;
                bool isMajor = c % 5 == 0;
                bool isAxis = c == 0;

                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = _canvasHeight,
                    Stroke = isAxis ? axisBrush : (isMajor ? majorBrush : minorBrush),
                    StrokeThickness = isAxis ? 1.5 : (isMajor ? 0.8 : 0.5)
                };
                LayoutCanvas.Children.Add(line);

                // Major grid labels
                if (isMajor && cellSize > 15)
                {
                    var label = new TextBlock
                    {
                        Text = c.ToString(),
                        FontSize = 9,
                        Foreground = majorBrush,
                        FontFamily = new FontFamily("Consolas")
                    };
                    Canvas.SetLeft(label, x + 2);
                    Canvas.SetTop(label, 2);
                    LayoutCanvas.Children.Add(label);
                }
            }

            for (int r = startRow; r <= endRow; r++)
            {
                double y = _panOffset.Y + r * cellSize;
                bool isMajor = r % 5 == 0;
                bool isAxis = r == 0;

                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = _canvasWidth,
                    Y2 = y,
                    Stroke = isAxis ? axisBrush : (isMajor ? majorBrush : minorBrush),
                    StrokeThickness = isAxis ? 1.5 : (isMajor ? 0.8 : 0.5)
                };
                LayoutCanvas.Children.Add(line);

                if (isMajor && cellSize > 15 && r != 0)
                {
                    var label = new TextBlock
                    {
                        Text = r.ToString(),
                        FontSize = 9,
                        Foreground = majorBrush,
                        FontFamily = new FontFamily("Consolas")
                    };
                    Canvas.SetLeft(label, 2);
                    Canvas.SetTop(label, y + 2);
                    LayoutCanvas.Children.Add(label);
                }
            }
        }

        // ===== MACHINE DRAWING (Tetris-like grid blocks) =====
        private void DrawMachines(ProductionGraph graph)
        {
            foreach (var node in graph.Nodes)
            {
                if (node.Position == null) continue;

                var spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine);
                int gridW = (int)Math.Ceiling(spec?.Width ?? 2);
                int gridD = (int)Math.Ceiling(spec?.Depth ?? 2);

                // Grid position (integer coordinates)
                int gridX = (int)Math.Floor(node.Position.Value.X);
                int gridZ = (int)Math.Floor(node.Position.Value.Z);

                // Convert to screen coordinates
                double screenX = _panOffset.X + gridX * _gridCellPixels * _zoomFactor;
                double screenY = _panOffset.Y + gridZ * _gridCellPixels * _zoomFactor;
                double screenW = gridW * _gridCellPixels * _zoomFactor;
                double screenH = gridD * _gridCellPixels * _zoomFactor;

                var cat = node.Recipe.Machine.GetCategory();
                if (!CategoryColors.TryGetValue(cat, out var color))
                    color = Colors.LightGray;
                if (!CategoryGlowColors.TryGetValue(cat, out var glowColor))
                    glowColor = Color.FromArgb(0x40, 0xC0, 0xC0, 0xC0);

                // Glow effect (behind the block)
                if (screenW > 8 && screenH > 8)
                {
                    var glow = new Rectangle
                    {
                        Width = screenW + 4,
                        Height = screenH + 4,
                        Fill = new SolidColorBrush(glowColor),
                        RadiusX = 3,
                        RadiusY = 3
                    };
                    Canvas.SetLeft(glow, screenX - 2);
                    Canvas.SetTop(glow, screenY - 2);
                    LayoutCanvas.Children.Add(glow);
                }

                // Main block (Tetris-like)
                var rect = new Rectangle
                {
                    Width = screenW,
                    Height = screenH,
                    Fill = new SolidColorBrush(Color.FromArgb(0xCC, color.R, color.G, color.B)),
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 1.5,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(rect, screenX);
                Canvas.SetTop(rect, screenY);
                LayoutCanvas.Children.Add(rect);

                // Inner highlight (3D effect)
                if (screenW > 12 && screenH > 12)
                {
                    var highlight = new Rectangle
                    {
                        Width = screenW - 4,
                        Height = screenH / 3,
                        Fill = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
                        RadiusX = 1,
                        RadiusY = 1
                    };
                    Canvas.SetLeft(highlight, screenX + 2);
                    Canvas.SetTop(highlight, screenY + 2);
                    LayoutCanvas.Children.Add(highlight);
                }

                // Label
                if (ShowLabelsToggle?.IsChecked == true && screenW > 20 && screenH > 16)
                {
                    string labelText = node.Recipe.Machine.GetDisplayName();
                    if (node.Count > 1)
                        labelText += " x" + node.Count;

                    var label = new TextBlock
                    {
                        Text = labelText,
                        FontSize = Math.Min(11, Math.Max(8, screenH / 4)),
                        Foreground = new SolidColorBrush(Colors.White),
                        TextAlignment = TextAlignment.Center,
                        Width = screenW,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Canvas.SetLeft(label, screenX);
                    Canvas.SetTop(label, screenY + screenH / 2 - 6);
                    LayoutCanvas.Children.Add(label);
                }

                // Layer badge (small corner)
                if (screenW > 20)
                {
                    var badge = new Border
                    {
                        Width = 16,
                        Height = 14,
                        Background = new SolidColorBrush(Color.FromArgb(0xAA, 0x00, 0x00, 0x00)),
                        CornerRadius = new CornerRadius(2),
                        Child = new TextBlock
                        {
                            Text = "L" + node.Layer,
                            FontSize = 8,
                            Foreground = new SolidColorBrush(Colors.White),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    Canvas.SetLeft(badge, screenX + 2);
                    Canvas.SetTop(badge, screenY + 2);
                    LayoutCanvas.Children.Add(badge);
                }
            }
        }

        // ===== CONVEYOR BELT DRAWING (L-shaped Manhattan routing) =====
        private void DrawConveyorBelts(ProductionGraph graph)
        {
            var planner = new RoutePlanner();
            var routes = planner.PlanAllRoutes(graph);

            foreach (var route in routes)
            {
                if (route.Path.Count < 2) continue;

                var source = graph.FindNode(route.SourceId);
                var target = graph.FindNode(route.TargetId);
                if (source?.Position == null || target?.Position == null) continue;

                // Convert path points to screen coordinates
                var screenPoints = new List<Point>();
                foreach (var pt in route.Path)
                {
                    screenPoints.Add(GridToScreen((int)pt.X, (int)pt.Z));
                }

                // Draw L-shaped path segments
                var beltBrush = new SolidColorBrush(Color.FromArgb(0xAA, 0xC0, 0xC0, 0xC0));
                var beltHighlight = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xD6, 0x00));

                for (int i = 1; i < screenPoints.Count; i++)
                {
                    var p1 = screenPoints[i - 1];
                    var p2 = screenPoints[i];

                    // Belt line
                    var line = new Line
                    {
                        X1 = p1.X,
                        Y1 = p1.Y,
                        X2 = p2.X,
                        Y2 = p2.Y,
                        Stroke = beltBrush,
                        StrokeThickness = Math.Max(2, _zoomFactor * 2),
                        StrokeDashArray = new DoubleCollection { 3, 2 }
                    };
                    LayoutCanvas.Children.Add(line);

                    // Belt highlight underneath
                    var hlLine = new Line
                    {
                        X1 = p1.X,
                        Y1 = p1.Y,
                        X2 = p2.X,
                        Y2 = p2.Y,
                        Stroke = beltHighlight,
                        StrokeThickness = Math.Max(4, _zoomFactor * 4)
                    };
                    LayoutCanvas.Children.Add(hlLine);
                }

                // Turn markers (small circles at corners)
                for (int i = 1; i < screenPoints.Count - 1; i++)
                {
                    var prev = screenPoints[i - 1];
                    var curr = screenPoints[i];
                    var next = screenPoints[i + 1];

                    // Check if turn
                    if ((Math.Abs(prev.X - curr.X) > 0.1 && Math.Abs(curr.Y - next.Y) > 0.1) ||
                        (Math.Abs(prev.Y - curr.Y) > 0.1 && Math.Abs(curr.X - next.X) > 0.1))
                    {
                        var turnDot = new Ellipse
                        {
                            Width = Math.Max(6, _zoomFactor * 4),
                            Height = Math.Max(6, _zoomFactor * 4),
                            Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xD6, 0x00)),
                            Stroke = new SolidColorBrush(Colors.White),
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(turnDot, curr.X - turnDot.Width / 2);
                        Canvas.SetTop(turnDot, curr.Y - turnDot.Height / 2);
                        LayoutCanvas.Children.Add(turnDot);
                    }
                }

                // Arrow at target
                if (screenPoints.Count >= 2)
                {
                    DrawArrow(screenPoints[screenPoints.Count - 2], screenPoints[screenPoints.Count - 1]);
                }
            }
        }

        private void DrawArrow(Point from, Point to)
        {
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;

            double nx = dx / len;
            double ny = dy / len;

            double arrowSize = Math.Max(6, _zoomFactor * 5);
            double ax1 = to.X - arrowSize * nx + arrowSize * 0.5 * ny;
            double ay1 = to.Y - arrowSize * ny - arrowSize * 0.5 * nx;
            double ax2 = to.X - arrowSize * nx - arrowSize * 0.5 * ny;
            double ay2 = to.Y - arrowSize * ny + arrowSize * 0.5 * nx;

            var arrow = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xD6, 0x00)),
                Points = new PointCollection
                {
                    new Point(to.X, to.Y),
                    new Point(ax1, ay1),
                    new Point(ax2, ay2)
                }
            };
            LayoutCanvas.Children.Add(arrow);
        }

        // ===== COORDINATE CONVERSION =====
        private Point GridToScreen(int gridX, int gridZ)
        {
            return new Point(
                _panOffset.X + gridX * _gridCellPixels * _zoomFactor,
                _panOffset.Y + gridZ * _gridCellPixels * _zoomFactor);
        }

        private (int gx, int gz) ScreenToGrid(double screenX, double screenY)
        {
            double cellSize = _gridCellPixels * _zoomFactor;
            return (
                (int)Math.Floor((screenX - _panOffset.X) / cellSize),
                (int)Math.Floor((screenY - _panOffset.Y) / cellSize));
        }

        // ===== EVENT HANDLERS =====
        private void RefreshLayout_Click(object sender, RoutedEventArgs e)
        {
            // Re-run layout if graph exists
            var graph = App.CurrentGraph;
            if (graph != null && graph.Nodes.Count > 0)
            {
                var planner = new SpatialPlanner { GridSize = 1.0 };
                planner.AutoLayout2D(graph, maxWidth: 80);
            }
            DrawLayout();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomIn();
        private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomOut();
        private void ZoomReset_Click(object sender, RoutedEventArgs e) => ResetView();

        private void GridSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (GridSizeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (double.TryParse(tag, out double size))
                {
                    _gridCellPixels = size;
                    DrawLayout();
                }
            }
        }

        // ===== POINTER INTERACTION (Pan + Wheel Zoom) =====
        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isPanning = true;
            _lastPointerPos = e.GetCurrentPoint(LayoutCanvas).Position;
            LayoutCanvas.CapturePointer(e.Pointer);
        }

        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPanning) return;
            var pos = e.GetCurrentPoint(LayoutCanvas).Position;
            _panOffset.X += pos.X - _lastPointerPos.X;
            _panOffset.Y += pos.Y - _lastPointerPos.Y;
            _lastPointerPos = pos;
            DrawLayout();
        }

        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isPanning = false;
            LayoutCanvas.ReleasePointerCapture(e.Pointer);
        }

        private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(LayoutCanvas);
            int delta = point.Properties.MouseWheelDelta;

            // Zoom towards mouse pointer
            double mouseX = point.Position.X;
            double mouseY = point.Position.Y;

            // Convert mouse pos to grid before zoom
            var (gridX, gridZ) = ScreenToGrid(mouseX, mouseY);

            if (delta > 0)
                _zoomFactor = Math.Min(_zoomFactor * 1.15, 3.0);
            else
                _zoomFactor = Math.Max(_zoomFactor / 1.15, 0.3);

            // Adjust pan so the same grid cell stays under mouse
            double newScreenX = _panOffset.X + gridX * _gridCellPixels * _zoomFactor;
            double newScreenY = _panOffset.Y + gridZ * _gridCellPixels * _zoomFactor;
            _panOffset.X += mouseX - newScreenX;
            _panOffset.Y += mouseY - newScreenY;

            DrawLayout();
            e.Handled = true;
        }
    }
}
