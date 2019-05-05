using System.Windows;
using System.Windows.Media;

namespace URLServerManagerModern.Windows.Utility
{
    public partial class ColorPickerWindow : Window
    {
        private SolidColorBrush modifiedBrush;
        public ColorPickerWindow(SolidColorBrush brush)
        {
            InitializeComponent();
            modifiedBrush = brush;
            picker.SetRGB(brush.Color.R, brush.Color.G, brush.Color.B);
        }

        private void ConfirmColor(object sender, RoutedEventArgs e)
        {
            modifiedBrush.Color = picker.GetSelectedMediaColor();
            //Debug.WriteLine("Modified color: " + modifiedBrush.Color);
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
