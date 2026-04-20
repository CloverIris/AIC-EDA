using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using Windows.UI;

namespace AIC_EDA.Controls
{
    public sealed partial class StepIndicatorBar : UserControl
    {
        private readonly List<string> _steps = new()
        {
            "Setup",
            "Synthesize",
            "Layout",
            "Export"
        };

        public StepIndicatorBar()
        {
            this.InitializeComponent();
            BuildSteps();
        }

        private void BuildSteps()
        {
            StepsContainer.Children.Clear();
            for (int i = 0; i < _steps.Count; i++)
            {
                var stepPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                stepPanel.Tag = i;

                var circle = new Border
                {
                    Width = 22,
                    Height = 22,
                    CornerRadius = new CornerRadius(11),
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0x2A, 0x2A)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x4D, 0x4D, 0x4D)),
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = (i + 1).ToString(),
                        FontSize = 10,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0x66, 0x66)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                stepPanel.Children.Add(circle);

                var label = new TextBlock
                {
                    Text = _steps[i],
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0x66, 0x66)),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                stepPanel.Children.Add(label);

                StepsContainer.Children.Add(stepPanel);

                // Arrow between steps
                if (i < _steps.Count - 1)
                {
                    StepsContainer.Children.Add(new FontIcon
                    {
                        Glyph = "\uE72A",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x4D, 0x4D, 0x4D)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(4, 0, 4, 0),
                    });
                }
            }
        }

        public void SetActiveStep(int stepIndex)
        {
            int childIdx = 0;
            for (int i = 0; i < _steps.Count; i++)
            {
                // Skip arrow elements
                while (childIdx < StepsContainer.Children.Count && StepsContainer.Children[childIdx] is not StackPanel)
                    childIdx++;

                if (childIdx >= StepsContainer.Children.Count) break;

                var stepPanel = (StackPanel)StepsContainer.Children[childIdx];
                var circle = (Border)stepPanel.Children[0];
                var label = (TextBlock)stepPanel.Children[1];
                var numText = (TextBlock)circle.Child;

                if (i < stepIndex)
                {
                    // Completed
                    circle.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x39, 0xFF, 0x14));
                    circle.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x39, 0xFF, 0x14));
                    numText.Text = "\uE73E"; // Checkmark
                    numText.Foreground = new SolidColorBrush(Colors.Black);
                    label.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xB3, 0xB3, 0xB3));
                }
                else if (i == stepIndex)
                {
                    // Active
                    circle.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xD6, 0x00));
                    circle.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xD6, 0x00));
                    numText.Text = (i + 1).ToString();
                    numText.Foreground = new SolidColorBrush(Colors.Black);
                    label.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xD6, 0x00));
                    label.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                }
                else
                {
                    // Pending
                    circle.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0x2A, 0x2A));
                    circle.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x4D, 0x4D, 0x4D));
                    numText.Text = (i + 1).ToString();
                    numText.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0x66, 0x66));
                    label.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0x66, 0x66));
                    label.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                }

                childIdx++;
            }
        }
    }
}
