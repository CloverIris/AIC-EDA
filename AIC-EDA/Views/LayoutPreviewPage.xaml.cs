using AIC_EDA.Models;
using AIC_EDA.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;

namespace AIC_EDA.Views
{
    public sealed partial class LayoutPreviewPage : Page
    {
        private double _scale = 12.0;
        private Point _panOffset = new(50, 50);
        private bool _isPanning = false;
        private Point _lastPointerPos;

        public LayoutPreviewPage()
        {
            this.InitializeComponent();
            this.Loaded += LayoutPreviewPage_Loaded;
        }

        private void LayoutPreviewPage_Loaded(object sender, RoutedEventArgs e)
        {
            DrawLayout();
        }

        public void DrawLayout()
        {
            LayoutCanvas.Children.Clear();

            var graph = App.CurrentGraph;
            if (graph == null || graph.Nodes.Count == 0)
            {
                PlaceholderText.Visibility = Visibility.Visible;
                return;
            }

            PlaceholderText.Visibility = Visibility.Collapsed;

            // 绘制网格
            if (ShowGridToggle.IsChecked == true)
            {
                DrawGrid();
            }

            // 绘制连接
            if (ShowConnectionsToggle.IsChecked == true)
            {
                DrawConnections(graph);
            }

            // 绘制节点
            DrawNodes(graph);
        }

        private void DrawGrid()
        {
            int gridSize = 50;
            var color = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128));

            for (int i = -20; i < 80; i++)
            {
                var lineV = new Line
                {
                    X1 = _panOffset.X + i * gridSize * _scale / 10,
                    Y1 = 0,
                    X2 = _panOffset.X + i * gridSize * _scale / 10,
                    Y2 = 2000,
                    Stroke = color,
                    StrokeThickness = 0.5
                };
                LayoutCanvas.Children.Add(lineV);

                var lineH = new Line
                {
                    X1 = 0,
                    Y1 = _panOffset.Y + i * gridSize * _scale / 10,
                    X2 = 2000,
                    Y2 = _panOffset.Y + i * gridSize * _scale / 10,
                    Stroke = color,
                    StrokeThickness = 0.5
                };
                LayoutCanvas.Children.Add(lineH);
            }
        }

        private void DrawConnections(ProductionGraph graph)
        {
            foreach (var edge in graph.Edges)
            {
                var source = graph.FindNode(edge.SourceId);
                var target = graph.FindNode(edge.TargetId);
                if (source?.Position == null || target?.Position == null) continue;

                var sp = WorldToScreen(source.Position.Value.X, source.Position.Value.Z);
                var ep = WorldToScreen(target.Position.Value.X, target.Position.Value.Z);

                var line = new Line
                {
                    X1 = sp.X,
                    Y1 = sp.Y,
                    X2 = ep.X,
                    Y2 = ep.Y,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                LayoutCanvas.Children.Add(line);
            }
        }

        private void DrawNodes(ProductionGraph graph)
        {
            var categoryColors = new Dictionary<MachineCategory, Color>
            {
                [MachineCategory.Resource] = Colors.DodgerBlue,
                [MachineCategory.Processing] = Colors.Orange,
                [MachineCategory.Logistics] = Colors.Gray,
                [MachineCategory.Power] = Colors.Gold,
                [MachineCategory.Agriculture] = Colors.LimeGreen,
            };

            foreach (var node in graph.Nodes)
            {
                if (node.Position == null) continue;

                var spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine);
                var w = (spec?.Width ?? 2) * _scale;
                var h = (spec?.Depth ?? 2) * _scale;
                var pos = WorldToScreen(node.Position.Value.X, node.Position.Value.Z);

                var cat = node.Recipe.Machine.GetCategory();
                if (!categoryColors.TryGetValue(cat, out var color))
                    color = Colors.LightGray;

                // 设备矩形
                var rect = new Rectangle
                {
                    Width = w,
                    Height = h,
                    Fill = new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B)),
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 1,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(rect, pos.X - w / 2);
                Canvas.SetTop(rect, pos.Y - h / 2);
                LayoutCanvas.Children.Add(rect);

                // 设备标签
                var label = new TextBlock
                {
                    Text = $"{node.Recipe.Machine.GetDisplayName()}\nx{node.Count}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = TextAlignment.Center,
                    Width = w
                };
                Canvas.SetLeft(label, pos.X - w / 2);
                Canvas.SetTop(label, pos.Y - 10);
                LayoutCanvas.Children.Add(label);

                // 层级标签
                var layerLabel = new TextBlock
                {
                    Text = $"L{node.Layer}",
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Colors.White),
                    Opacity = 0.7
                };
                Canvas.SetLeft(layerLabel, pos.X - w / 2 + 2);
                Canvas.SetTop(layerLabel, pos.Y - h / 2 + 2);
                LayoutCanvas.Children.Add(layerLabel);
            }
        }

        private Point WorldToScreen(float worldX, float worldZ)
        {
            return new Point(
                _panOffset.X + worldX * _scale,
                _panOffset.Y + worldZ * _scale);
        }

        private void RefreshLayout_Click(object sender, RoutedEventArgs e)
        {
            DrawLayout();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _scale *= 1.2;
            DrawLayout();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _scale /= 1.2;
            DrawLayout();
        }

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
    }
}
