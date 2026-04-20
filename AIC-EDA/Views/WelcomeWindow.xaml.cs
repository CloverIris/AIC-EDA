using AIC_EDA.Models;
using AIC_EDA.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;

namespace AIC_EDA.Views
{
    public sealed partial class WelcomeWindow : Window
    {
        public DashboardViewModel ViewModel { get; } = new();
        private DispatcherTimer? _simTimer;

        public WelcomeWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1200, Height = 850 });

            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
            if (displayArea != null)
            {
                var centerX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - 1200) / 2;
                var centerY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - 850) / 2;
                appWindow.Move(new Windows.Graphics.PointInt32 { X = centerX, Y = centerY });
            }

            // Populate target stack
            RefreshTargetStack();

            // Listen for VM changes
            ViewModel.PropertyChanged += (s, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(ViewModel.PlayerAssets):
                        RefreshAssetStack();
                        DrawRadarChart();
                        break;
                    case nameof(ViewModel.CheckListItems):
                        RefreshCheckListStack();
                        break;
                    case nameof(ViewModel.RoadMapStages):
                        RefreshRoadMapStack();
                        break;
                    case nameof(ViewModel.SharedIntermediates):
                        RefreshSharedStack();
                        break;
                    case nameof(ViewModel.WeightResource):
                    case nameof(ViewModel.WeightProcessing):
                    case nameof(ViewModel.WeightLogistics):
                    case nameof(ViewModel.WeightPower):
                    case nameof(ViewModel.WeightAgriculture):
                        DrawRadarChart();
                        break;
                    case nameof(ViewModel.IsSimulating):
                        if (ViewModel.IsSimulating) StartSimAnimation();
                        else StopSimAnimation();
                        break;
                }
            };

            DrawRadarChart();
            DrawSimCanvas();
        }

        private void WelcomeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate target stack
            RefreshTargetStack();

            // Listen for VM changes
            ViewModel.PropertyChanged += (s, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(ViewModel.PlayerAssets):
                        RefreshAssetStack();
                        DrawRadarChart();
                        break;
                    case nameof(ViewModel.CheckListItems):
                        RefreshCheckListStack();
                        break;
                    case nameof(ViewModel.RoadMapStages):
                        RefreshRoadMapStack();
                        break;
                    case nameof(ViewModel.SharedIntermediates):
                        RefreshSharedStack();
                        break;
                    case nameof(ViewModel.WeightResource):
                    case nameof(ViewModel.WeightProcessing):
                    case nameof(ViewModel.WeightLogistics):
                    case nameof(ViewModel.WeightPower):
                    case nameof(ViewModel.WeightAgriculture):
                        DrawRadarChart();
                        break;
                    case nameof(ViewModel.IsSimulating):
                        if (ViewModel.IsSimulating) StartSimAnimation();
                        else StopSimAnimation();
                        break;
                }
            };

            DrawRadarChart();
            DrawSimCanvas();
        }

        // ===== Q1: Asset Stack =====
        private void RefreshAssetStack()
        {
            AssetStack.Children.Clear();
            foreach (var asset in ViewModel.PlayerAssets)
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto },
                    },
                    Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xD6, 0x00)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                };

                var text = new TextBlock
                {
                    Text = $"[{asset.Type}] {asset.ItemName} x{asset.Quantity:F0}",
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(text, 0);
                row.Children.Add(text);

                var delBtn = new Button
                {
                    Content = "\uE74D",
                    FontFamily = new FontFamily("Segoe Fluent Icons"),
                    FontSize = 10,
                    Padding = new Thickness(4, 2, 4, 2),
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
                    BorderThickness = new Thickness(0),
                };
                delBtn.Click += (s, e) => ViewModel.PlayerAssets.Remove(asset);
                Grid.SetColumn(delBtn, 1);
                row.Children.Add(delBtn);

                AssetStack.Children.Add(row);
            }
        }

        private void AddAsset_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddAssetCommand.Execute(null);
        }

        private void ClearAssets_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearAssetsCommand.Execute(null);
        }

        private void CalcBalance_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CalculateBalancedLayoutCommand.Execute(null);
        }

        // ===== Radar Chart =====
        private void DrawRadarChart()
        {
            RadarCanvas.Children.Clear();
            double cx = 50;
            double cy = 50;
            double maxR = 40;
            string[] labels = { "Res", "Prc", "Log", "Pwr", "Agr" };
            double[] values = { ViewModel.WeightResource, ViewModel.WeightProcessing, ViewModel.WeightLogistics, ViewModel.WeightPower, ViewModel.WeightAgriculture };

            // Draw background pentagon rings
            for (int ring = 1; ring <= 4; ring++)
            {
                double r = maxR * ring / 4.0;
                var polygon = new Polygon
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)),
                    StrokeThickness = 0.5,
                    Fill = null,
                };
                for (int i = 0; i < 5; i++)
                {
                    double angle = Math.PI / 2 + i * 2 * Math.PI / 5;
                    polygon.Points.Add(new Point(cx + r * Math.Cos(angle), cy - r * Math.Sin(angle)));
                }
                RadarCanvas.Children.Add(polygon);
            }

            // Draw axes
            for (int i = 0; i < 5; i++)
            {
                double angle = Math.PI / 2 + i * 2 * Math.PI / 5;
                var line = new Line
                {
                    X1 = cx, Y1 = cy,
                    X2 = cx + maxR * Math.Cos(angle),
                    Y2 = cy - maxR * Math.Sin(angle),
                    Stroke = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
                    StrokeThickness = 0.5,
                };
                RadarCanvas.Children.Add(line);

                // Labels
                var labelR = maxR + 10;
                var tb = new TextBlock
                {
                    Text = labels[i],
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
                };
                Canvas.SetLeft(tb, cx + labelR * Math.Cos(angle) - 6);
                Canvas.SetTop(tb, cy - labelR * Math.Sin(angle) - 4);
                RadarCanvas.Children.Add(tb);
            }

            // Draw data polygon
            var dataPoly = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xD6, 0x00)),
                Stroke = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xD6, 0x00)),
                StrokeThickness = 1.5,
            };
            for (int i = 0; i < 5; i++)
            {
                double angle = Math.PI / 2 + i * 2 * Math.PI / 5;
                double r = maxR * values[i] / 100.0;
                dataPoly.Points.Add(new Point(cx + r * Math.Cos(angle), cy - r * Math.Sin(angle)));
            }
            RadarCanvas.Children.Add(dataPoly);
        }

        // ===== Q2: CheckList & RoadMap =====
        private void GenerateRoadMap_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GenerateRoadMapCommand.Execute(null);
        }

        private void RefreshCheckListStack()
        {
            CheckListStack.Children.Clear();
            foreach (var item in ViewModel.CheckListItems)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
                var cb = new CheckBox
                {
                    IsChecked = item.IsChecked,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                };
                cb.Checked += (s, e) => ViewModel.ToggleCheckItemCommand.Execute(item);
                cb.Unchecked += (s, e) => ViewModel.ToggleCheckItemCommand.Execute(item);
                row.Children.Add(cb);
                row.Children.Add(new TextBlock
                {
                    Text = $"{item.ItemName} x{item.Quantity:F0}",
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center,
                });
                CheckListStack.Children.Add(row);
            }
        }

        private void RefreshRoadMapStack()
        {
            RoadMapStack.Children.Clear();
            foreach (var stage in ViewModel.RoadMapStages.OrderBy(s => s.Order))
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0x20, 0x1A, 0x1A, 0x1A)),
                    BorderBrush = (Brush)Application.Current.Resources["BorderDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 8, 10, 8),
                };
                var stack = new StackPanel { Spacing = 4 };
                stack.Children.Add(new TextBlock
                {
                    Text = $"Phase {stage.Order}: {stage.Title}",
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                });
                stack.Children.Add(new TextBlock
                {
                    Text = stage.Description,
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
                });
                stack.Children.Add(new TextBlock
                {
                    Text = $"Power: {stage.EstimatedPower:F0}kW | Area: {stage.EstimatedArea:F0} grids",
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["AccentCyanBrush"],
                });
                card.Child = stack;
                RoadMapStack.Children.Add(card);
            }
        }

        // ===== Q3: Multi-Target =====
        private void RefreshTargetStack()
        {
            TargetStack.Children.Clear();
            foreach (var target in ViewModel.TargetSelections)
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto },
                    },
                    VerticalAlignment = VerticalAlignment.Center,
                };

                var cb = new CheckBox
                {
                    IsChecked = target.IsSelected,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                };
                cb.Checked += (s, e) => target.IsSelected = true;
                cb.Unchecked += (s, e) => target.IsSelected = false;
                Grid.SetColumn(cb, 0);
                row.Children.Add(cb);

                var name = new TextBlock
                {
                    Text = target.Item.Name,
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0),
                };
                Grid.SetColumn(name, 1);
                row.Children.Add(name);

                var rateBox = new NumberBox
                {
                    Value = target.Rate,
                    Minimum = 1,
                    Maximum = 1000,
                    Width = 80,
                    FontSize = 11,
                };
                rateBox.ValueChanged += (s, e) => target.Rate = rateBox.Value;
                Grid.SetColumn(rateBox, 2);
                row.Children.Add(rateBox);

                TargetStack.Children.Add(row);
            }
        }

        private void AnalyzeShared_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AnalyzeSharedIntermediatesCommand.Execute(null);
        }

        private void RefreshSharedStack()
        {
            SharedStack.Children.Clear();
            foreach (var shared in ViewModel.SharedIntermediates)
            {
                SharedStack.Children.Add(new TextBlock
                {
                    Text = $"{shared.ItemName} — used by {shared.UsedByCount} products ({shared.UsedByProducts})",
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["AccentYellowBrush"],
                    TextTrimming = TextTrimming.CharacterEllipsis,
                });
            }
        }

        // ===== Q4: Simulation =====
        private void ToggleSim_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleSimulationCommand.Execute(null);
        }

        private void DrawSimCanvas()
        {
            SimCanvas.Children.Clear();
            var rand = new Random(42);
            double w = SimCanvas.ActualWidth > 0 ? SimCanvas.ActualWidth : 300;
            double h = SimCanvas.ActualHeight > 0 ? SimCanvas.ActualHeight : 200;

            // Draw some nodes
            for (int i = 0; i < 6; i++)
            {
                double nx = 30 + rand.NextDouble() * (w - 60);
                double ny = 30 + rand.NextDouble() * (h - 60);
                var ellipse = new Ellipse
                {
                    Width = 24,
                    Height = 24,
                    Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x39, 0xFF, 0x14)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1,
                };
                Canvas.SetLeft(ellipse, nx - 12);
                Canvas.SetTop(ellipse, ny - 12);
                SimCanvas.Children.Add(ellipse);

                SimCanvas.Children.Add(new TextBlock
                {
                    Text = $"M{i + 1}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Colors.Black),
                });
                Canvas.SetLeft(SimCanvas.Children[SimCanvas.Children.Count - 1], nx - 8);
                Canvas.SetTop(SimCanvas.Children[SimCanvas.Children.Count - 1], ny - 6);
            }
        }

        private void StartSimAnimation()
        {
            _simTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _simTimer.Tick += (s, e) =>
            {
                // Simple visual update: toggle node colors
                foreach (var child in SimCanvas.Children.OfType<Ellipse>())
                {
                    var rand = new Random();
                    double load = rand.NextDouble();
                    child.Fill = load > 0.7
                        ? new SolidColorBrush(Color.FromArgb(0xFF, 0x39, 0xFF, 0x14))
                        : load > 0.3
                            ? new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xD6, 0x00))
                            : new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x3D, 0x00));
                }
            };
            _simTimer.Start();
        }

        private void StopSimAnimation()
        {
            _simTimer?.Stop();
        }

        // ===== Footer =====
        private void SkipBtn_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["ShowWelcomeScreen"] = ShowOnStartupToggle.IsOn;
            this.Close();
        }
    }
}
