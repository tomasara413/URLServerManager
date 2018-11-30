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
    /// <summary>
    /// Interaction logic for OtherProtocol.xaml
    /// </summary>
    public partial class InputBoxPopup : Window
    {
        public event OptionAccepted OnOptionAccepted;
        public delegate void OptionAccepted(string inputBoxValue);

        public InputBoxPopup(string title, string description)
        {
            InitializeComponent();

            Title = title;
            PopupInputDescText.Text = description;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            OnOptionAccepted?.Invoke(Input.Text.Trim());
            Close();
        }

        private void InputChanged(object sender, TextChangedEventArgs e)
        {
            Ok.IsEnabled = !(string.IsNullOrEmpty(Input.Text) || string.IsNullOrWhiteSpace(Input.Text));
        }
    }
}
