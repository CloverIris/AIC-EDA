using AIC_EDA.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AIC_EDA.Views
{
    public sealed partial class BlueprintExportPage : Page
    {
        private BlueprintCodec _codec = new();

        public BlueprintExportPage()
        {
            this.InitializeComponent();
            this.Loaded += BlueprintExportPage_Loaded;
        }

        private void BlueprintExportPage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            var graph = App.CurrentGraph;
            if (graph == null)
            {
                BlueprintTextBox.Text = "请先前往 Recipe Compiler 页面编译生产图";
                StatusInfoBar.Message = "无可用的生产图数据";
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.IsOpen = true;
                return;
            }

            try
            {
                var blueprint = _codec.Encode(graph);
                BlueprintTextBox.Text = blueprint;

                TargetItemText.Text = graph.TargetItem;
                TargetRateText.Text = $"{graph.TargetRate:F1} /min";
                MachineCountText.Text = graph.Nodes.Count.ToString();
                BeltCountText.Text = graph.Edges.Count.ToString();

                StatusInfoBar.Message = "蓝图生成成功";
                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.IsOpen = true;
            }
            catch (System.Exception ex)
            {
                StatusInfoBar.Message = $"蓝图生成失败: {ex.Message}";
                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.IsOpen = true;
            }
        }

        private void ExportBlueprint_Click(object sender, RoutedEventArgs e)
        {
            RefreshPreview();
        }

        private async void ExportJson_Click(object sender, RoutedEventArgs e)
        {
            var graph = App.CurrentGraph;
            if (graph == null)
            {
                StatusInfoBar.Message = "没有可用的生产图";
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.IsOpen = true;
                return;
            }

            var savePicker = new FileSavePicker();
            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(App.MainWindow));
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("JSON文件", new List<string> { ".json" });
            savePicker.SuggestedFileName = $"AIC-EDA-{graph.TargetItem}";

            var file = await savePicker.PickSaveFileAsync().AsTask();
            if (file != null)
            {
                try
                {
                    await _codec.ExportToJsonAsync(graph, file.Path);
                    StatusInfoBar.Message = $"已保存到: {file.Path}";
                    StatusInfoBar.Severity = InfoBarSeverity.Success;
                    StatusInfoBar.IsOpen = true;
                }
                catch (System.Exception ex)
                {
                    StatusInfoBar.Message = $"保存失败: {ex.Message}";
                    StatusInfoBar.Severity = InfoBarSeverity.Error;
                    StatusInfoBar.IsOpen = true;
                }
            }
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(BlueprintTextBox.Text);
            Clipboard.SetContent(dataPackage);

            StatusInfoBar.Message = "已复制到剪贴板";
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.IsOpen = true;
        }
    }
}
