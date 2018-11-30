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

namespace URLServerManagerModern.Windows.Main
{
    /// <summary>
    /// Interaction logic for WatcherWindow.xaml
    /// </summary>
    public partial class WatcherWindow : Window
    {
        public WatcherWindow()
        {
            InitializeComponent();

            wrapper.ItemsSource = Utilities.Utilities.LoadServers(0, 5).Where(x => x.server.protocolAddresses.Count > 0).Select(x => x.server).ToArray();
        }
    }
}
