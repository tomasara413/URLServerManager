using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace URLServerManagerModern.Windows.Utility
{
    public partial class ColorPickerWindow : Window
    {
        private SolidColorBrush modifiedBrush;
        public ColorPickerWindow(SolidColorBrush brush)
        {
            InitializeComponent();
            modifiedBrush = brush;
        }

        private void ConfirmColor(object sender, RoutedEventArgs e)
        {
            modifiedBrush.Color = picker.GetSelectedMediaColor();
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
