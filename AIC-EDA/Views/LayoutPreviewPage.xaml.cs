using AIC_EDA.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        private WriteableBitmap? _framebuffer;
        private bool _useSoftRender = false;

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
            var graph = App.CurrentGraph;
            if (graph == null || graph.Nodes.Count == 0)
            {
                PlaceholderPanel.Visibility = Visibility.Visible;
                LayoutCanvas.Visibility = Visibility.Collapsed;
                LayoutImage.Visibility = Visibility.Collapsed;
                return;
            }

            PlaceholderPanel.Visibility = Visibility.Collapsed;

            if (_useSoftRender)
            {
                LayoutCanvas.Visibility = Visibility.Collapsed;
                LayoutImage.Visibility = Visibility.Visible;
                RenderToFramebuffer(graph);
            }
            else
            {
                LayoutCanvas.Visibility = Visibility.Visible;
                LayoutImage.Visibility = Visibility.Collapsed;
                DrawToCanvas(graph);
            }

            ZoomText.Text = $"{_scale:F0}%";
        }

        private void DrawToCanvas(ProductionGraph graph)
        {
            LayoutCanvas.Children.Clear();

            if (ShowGridToggle?.IsChecked == true)
            {
                DrawGrid();
            }

            if (ShowConnectionsToggle?.IsChecked == true)
            {
                DrawConnections(graph);
            }

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
                    Text = node.Recipe.Machine.GetDisplayName() + "\nx" + node.Count,
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
                    Text = "L" + node.Layer,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Colors.White),
                    Opacity = 0.7
                };
                Canvas.SetLeft(layerLabel, pos.X - w / 2 + 2);
                Canvas.SetTop(layerLabel, pos.Y - h / 2 + 2);
                LayoutCanvas.Children.Add(layerLabel);
            }
        }

        private void RenderToFramebuffer(ProductionGraph graph)
        {
            // Soft-render to a WriteableBitmap
            int width = (int)LayoutCanvas.ActualWidth;
            int height = (int)LayoutCanvas.ActualHeight;
            if (width <= 0) width = 800;
            if (height <= 0) height = 600;

            if (_framebuffer == null || _framebuffer.PixelWidth != width || _framebuffer.PixelHeight != height)
            {
                _framebuffer = new WriteableBitmap(width, height);
                LayoutImage.Source = _framebuffer;
            }

            var pixels = new byte[width * height * 4];

            // Fill background
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 0] = 0x14; // B
                pixels[i + 1] = 0x14; // G
                pixels[i + 2] = 0x14; // R
                pixels[i + 3] = 0xFF; // A
            }

            // Draw grid
            if (ShowGridToggle?.IsChecked == true)
            {
                DrawGridToBuffer(pixels, width, height);
            }

            // Draw nodes
            DrawNodesToBuffer(pixels, width, height, graph);

            // Copy to bitmap
            using (var stream = _framebuffer.PixelBuffer.AsStream())
            {
                stream.Write(pixels, 0, pixels.Length);
            }
            _framebuffer.Invalidate();
        }

        private void DrawGridToBuffer(byte[] pixels, int width, int height)
        {
            int gridSize = 50;
            byte gridR = 0x40, gridG = 0x40, gridB = 0x40;

            for (int i = -20; i < 80; i++)
            {
                int x = (int)(_panOffset.X + i * gridSize * _scale / 10);
                int y = (int)(_panOffset.Y + i * gridSize * _scale / 10);

                if (x >= 0 && x < width)
                    DrawLineV(pixels, width, height, x, 0, height, gridR, gridG, gridB);
                if (y >= 0 && y < height)
                    DrawLineH(pixels, width, height, 0, width, y, gridR, gridG, gridB);
            }
        }

        private void DrawNodesToBuffer(byte[] pixels, int width, int height, ProductionGraph graph)
        {
            var categoryColors = new Dictionary<MachineCategory, (byte R, byte G, byte B)>
            {
                [MachineCategory.Resource] = (0x1E, 0x90, 0xFF),
                [MachineCategory.Processing] = (0xFF, 0xA5, 0x00),
                [MachineCategory.Logistics] = (0x80, 0x80, 0x80),
                [MachineCategory.Power] = (0xFF, 0xD7, 0x00),
                [MachineCategory.Agriculture] = (0x32, 0xCD, 0x32),
            };

            foreach (var node in graph.Nodes)
            {
                if (node.Position == null) continue;

                var spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine);
                var w = (int)((spec?.Width ?? 2) * _scale);
                var h = (int)((spec?.Depth ?? 2) * _scale);
                var pos = WorldToScreen(node.Position.Value.X, node.Position.Value.Z);
                int left = (int)(pos.X - w / 2);
                int top = (int)(pos.Y - h / 2);

                var cat = node.Recipe.Machine.GetCategory();
                if (!categoryColors.TryGetValue(cat, out var color))
                    color = (0xC0, 0xC0, 0xC0);

                DrawRect(pixels, width, height, left, top, w, h, color.R, color.G, color.B, 200);
                DrawRectBorder(pixels, width, height, left, top, w, h, color.R, color.G, color.B);
            }
        }

        private void DrawLineH(byte[] pixels, int width, int height, int x1, int x2, int y, byte r, byte g, byte b)
        {
            if (y < 0 || y >= height) return;
            for (int x = Math.Max(0, x1); x < Math.Min(width, x2); x++)
            {
                int idx = (y * width + x) * 4;
                pixels[idx + 0] = b;
                pixels[idx + 1] = g;
                pixels[idx + 2] = r;
                pixels[idx + 3] = 0xFF;
            }
        }

        private void DrawLineV(byte[] pixels, int width, int height, int x, int y1, int y2, byte r, byte g, byte b)
        {
            if (x < 0 || x >= width) return;
            for (int y = Math.Max(0, y1); y < Math.Min(height, y2); y++)
            {
                int idx = (y * width + x) * 4;
                pixels[idx + 0] = b;
                pixels[idx + 1] = g;
                pixels[idx + 2] = r;
                pixels[idx + 3] = 0xFF;
            }
        }

        private void DrawRect(byte[] pixels, int width, int height, int left, int top, int w, int h, byte r, byte g, byte b, byte a)
        {
            for (int y = Math.Max(0, top); y < Math.Min(height, top + h); y++)
            {
                for (int x = Math.Max(0, left); x < Math.Min(width, left + w); x++)
                {
                    int idx = (y * width + x) * 4;
                    byte alpha = a;
                    byte invA = (byte)(255 - alpha);
                    pixels[idx + 0] = (byte)((b * alpha + pixels[idx + 0] * invA) / 255);
                    pixels[idx + 1] = (byte)((g * alpha + pixels[idx + 1] * invA) / 255);
                    pixels[idx + 2] = (byte)((r * alpha + pixels[idx + 2] * invA) / 255);
                    pixels[idx + 3] = 0xFF;
                }
            }
        }

        private void DrawRectBorder(byte[] pixels, int width, int height, int left, int top, int w, int h, byte r, byte g, byte b)
        {
            DrawLineH(pixels, width, height, left, left + w, top, r, g, b);
            DrawLineH(pixels, width, height, left, left + w, top + h - 1, r, g, b);
            DrawLineV(pixels, width, height, left, top, top + h, r, g, b);
            DrawLineV(pixels, width, height, left + w - 1, top, top + h, r, g, b);
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
