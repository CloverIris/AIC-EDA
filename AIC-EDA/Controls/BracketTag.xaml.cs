using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIC_EDA.Controls
{
    public sealed partial class BracketTag : UserControl
    {
        public static readonly DependencyProperty TagTextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(BracketTag), new PropertyMetadata("", OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TagTextProperty);
            set => SetValue(TagTextProperty, value);
        }

        public BracketTag()
        {
            this.InitializeComponent();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BracketTag control && e.NewValue is string text)
            {
                control.TagText.Text = "[ " + text + " ]";
            }
        }
    }
}
