using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;

namespace URLServerManagerModern.Windows.Main
{
    public partial class WatcherWindow : Window
    {
        private const int LIMIT = 5;
        private int offset = 0;
        public WatcherWindow()
        {
            InitializeComponent();

            //Utilities.Utilities.LoadServersBasedOnAdresses(0);
            wrapper.ItemsSource = (loadedServers = Utilities.Utilities.LoadServers(offset, LIMIT, null, false)).Where(x => x.server.protocolAddresses.Count > 0).Select(x => x.server).ToList();
            offset = loadedServers.Count;
            endOfContent = loadedServers.Count < LIMIT;
        }

        private bool fetchingData = false, endOfContent = false;
        List<PseudoEntity> loadedServers;
        private async void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!endOfContent && !fetchingData && e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight == e.ExtentHeight)
            {
                fetchingData = true;
                loadedServers = await Utilities.Utilities.LoadServersAsync(offset, LIMIT, null, false);
                fetchingData = false;

                if (loadedServers.Count > 0)
                {
                    (wrapper.ItemsSource as List<Server>).AddRange(loadedServers.Where(x => x.server.protocolAddresses.Count > 0).Select(x => x.server).ToArray());
                    wrapper.Items.Refresh();
                }

                offset += loadedServers.Count;

                endOfContent = loadedServers.Count < LIMIT;
            }
        }
    }
}
