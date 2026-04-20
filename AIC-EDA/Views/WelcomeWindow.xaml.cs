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

            // Window sizing
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 960, Height = 680 });

            LoadHardwareInfo();
            LoadRecentProjects();
            LoadProducts();
        }

        private void LoadHardwareInfo()
        {
            try
            {
                // CPU
                var cpu = GetCpuInfo();
                CpuText.Text = cpu;

                // Memory
                var mem = GetMemoryInfo();
                MemoryText.Text = mem;

                // GPU (simplified)
                GpuText.Text = GetGpuInfo();

                // OS
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
                // Try WMI via PowerShell or registry
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

        private async void LoadProducts()
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
                // Show some error indicator
                return;
            }

            double rate = TargetRateBox.Value;

            // Create a basic graph
            var graph = new ProductionGraph
            {
                TargetItem = selectedItem.Name,
                TargetRate = rate
            };

            App.CurrentGraph = graph;
            App.OpenMainWindow();
            App.CloseWelcomeWindow();
        }

        private void OpenExistingBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveStartupPreference();
            // TODO: Implement file picker to open existing project
            App.OpenMainWindow();
            App.CloseWelcomeWindow();
        }

        private void SkipBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveStartupPreference();
            App.OpenMainWindow();
            App.CloseWelcomeWindow();
        }

        private void RecentProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecentProjectsList.SelectedItem is RecentProject project)
            {
                SaveStartupPreference();
                // TODO: Load the project
                App.OpenMainWindow();
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
