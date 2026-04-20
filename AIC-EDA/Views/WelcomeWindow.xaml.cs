using AIC_EDA.Core;
using AIC_EDA.Models;
using AIC_EDA.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace AIC_EDA.Views
{
    public sealed partial class WelcomeWindow : Window
    {
        public ObservableCollection<RecentProject> RecentProjects { get; } = new();

        public WelcomeWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);

            // Window sizing: larger, centered on screen
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1100, Height = 750 });

            // Center on monitor
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
            if (displayArea != null)
            {
                var centerX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - 1100) / 2;
                var centerY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - 750) / 2;
                appWindow.Move(new Windows.Graphics.PointInt32 { X = centerX, Y = centerY });
            }

            // Always on top
            if (App.MainWindow != null)
            {
                var mainHwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                var mainWindowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(mainHwnd);
                var mainAppWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(mainWindowId);
                // We can't use AppWindow for Z-order, but we can use HWND via P/Invoke
                // Instead, rely on activating welcome after main
            }

            LoadHardwareInfo();
            LoadRecentProjects();
            LoadProducts();
        }

        private void LoadHardwareInfo()
        {
            try
            {
                var cpu = GetCpuInfo();
                CpuText.Text = cpu;

                var mem = GetMemoryInfo();
                MemoryText.Text = mem;

                GpuText.Text = GetGpuInfo();

                var os = Environment.OSVersion;
                OsText.Text = $"Windows {os.Version.Major}.{os.Version.Minor} ({(Environment.Is64BitOperatingSystem ? "x64" : "x86")})";
            }
            catch
            {
                CpuText.Text = "Unknown";
                MemoryText.Text = "Unknown";
                GpuText.Text = "Unknown";
                OsText.Text = "Windows";
            }
        }

        private string GetCpuInfo()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key != null)
                {
                    var name = key.GetValue("ProcessorNameString")?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        return $"{name} ({Environment.ProcessorCount} cores)";
                    }
                }
            }
            catch { }
            return $"{Environment.ProcessorCount} Logical Processors";
        }

        private string GetMemoryInfo()
        {
            try
            {
                var status = new MEMORYSTATUSEX();
                status.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref status))
                {
                    var totalGB = status.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    return $"{totalGB:F1} GB";
                }
            }
            catch { }
            return "Unknown";
        }

        private string GetGpuInfo()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                if (key != null)
                {
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        if (subKeyName.StartsWith("0"))
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            var desc = subKey?.GetValue("DriverDesc")?.ToString();
                            if (!string.IsNullOrEmpty(desc))
                                return desc;
                        }
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        private void LoadProducts()
        {
            try
            {
                var db = RecipeDatabaseService.Instance;
                if (!db.IsLoaded) db.LoadDefaultData();
                var finalProducts = db.Items
                    .Where(i => db.Recipes.Any(r => r.Outputs.Any(o => o.Key == i.Id)))
                    .OrderBy(i => i.Name)
                    .ToList();

                TargetProductCombo.ItemsSource = finalProducts;
                TargetProductCombo.DisplayMemberPath = "Name";
            }
            catch { }
        }

        private void LoadRecentProjects()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var recentPath = Path.Combine(localFolder.Path, "recent_projects.json");
                if (File.Exists(recentPath))
                {
                    var json = File.ReadAllText(recentPath);
                    var projects = JsonSerializer.Deserialize<List<RecentProject>>(json);
                    if (projects != null)
                    {
                        foreach (var p in projects.OrderByDescending(p => p.LastModifiedDate))
                        {
                            RecentProjects.Add(p);
                        }
                    }
                }
            }
            catch { }

            RecentProjectsList.ItemsSource = RecentProjects;
            EmptyRecentText.Visibility = RecentProjects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveStartupPreference()
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["ShowWelcomeScreen"] = ShowOnStartupToggle.IsOn;
        }

        private void CreateProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveStartupPreference();

            var selectedItem = TargetProductCombo.SelectedItem as Item;
            if (selectedItem == null)
            {
                return;
            }

            double rate = TargetRateBox.Value;

            var graph = new ProductionGraph
            {
                TargetItem = selectedItem.Name,
                TargetRate = rate
            };

            App.CurrentGraph = graph;
            App.CloseWelcomeWindow();
        }

        private void OpenExistingBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveStartupPreference();
            App.CloseWelcomeWindow();
        }

        private void SkipBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveStartupPreference();
            App.CloseWelcomeWindow();
        }

        private void RecentProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecentProjectsList.SelectedItem is RecentProject project)
            {
                SaveStartupPreference();
                App.CloseWelcomeWindow();
            }
        }
    }

    public class RecentProject
    {
        public string Name { get; set; } = string.Empty;
        public string TargetItem { get; set; } = string.Empty;
        public int MachineCount { get; set; }
        public string LastModified { get; set; } = string.Empty;
        public DateTime LastModifiedDate { get; set; }
    }
}
