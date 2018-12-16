using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        private struct ServerAddressPair
        {
            public Server server;
            public ProtocolAddress address;

            public ServerAddressPair(Server s, ProtocolAddress a)
            {
                server = s;
                address = a;
            }
        }

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
        private async void TestManually(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            ProtocolAddress pa = fe.DataContext as ProtocolAddress;
            Server s = (VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(fe))) as FrameworkElement).DataContext as Server;

            fe.IsEnabled = false;

            ServerAddressPair? pair = null;
            if (pa != null)
                pa.status = await GetStatusAsync(new ServerAddressPair() { server = s, address = pa });

            if (pair != null)
                tasks.Remove((ServerAddressPair)pair);

            fe.IsEnabled = true;
        }

        IdnMapping idnm = new IdnMapping();
        Dictionary<ServerAddressPair, Task<Status>> tasks = new Dictionary<ServerAddressPair, Task<Status>>();
        private Task<Status> GetStatusAsync(ServerAddressPair pair)
        {
            Task<Status> t;
            ProtocolAddress pa = pair.address;
            if (!tasks.ContainsKey(pair))
                tasks.Add(pair, t = Task.Run(() => GetStatus(pa)));
            else
            {
                if (tasks[pair].IsCompleted)
                    t = tasks[pair] = Task.Run(() => GetStatus(pa));
                else
                    t = tasks[pair];
            }

            return t;
        }

        private Status GetStatus(ProtocolAddress pa)
        {
            Status result = Status.AddressUnreachable;

            TcpClient client = new TcpClient();
            client.SendTimeout = 60;
            client.ReceiveTimeout = 120;

            Ping pinger = new Ping();
            try
            {
                client.Connect(pa.hostname, pa.port);
                if (client.Connected)
                {
                    client.Close();
                    result = Status.Ok;
                }
            }
            catch (SocketException ex)
            {
                client.Close();

                IPAddress address;
                //Debug.WriteLine("Is IP address: {0}; Resolved: {1}", IPAddress.TryParse(pa.hostname, out address), Utilities.Utilities.ResolveHostname(idnm.GetAscii(pa.hostname), 60));
                if (!IPAddress.TryParse(pa.hostname, out address) && !Utilities.Utilities.ResolveHostname(idnm.GetAscii(pa.hostname), 60))
                    return Status.DNSEntryNotFound;

                try
                {
                    if (pinger.Send(pa.hostname).Status != IPStatus.Success)
                        return Status.AddressUnreachable;
                }
                catch (PingException exe)
                {
                    throw;
                    Utilities.Utilities.Log("[ERROR] Testing ping", exe.ToString());
                }

                return Status.PortNotResponding;

            }
            catch (Exception ex)
            {
                throw;
                Utilities.Utilities.Log("[ERROR] Testing port availability", ex.ToString());
            }

            return result;
        }

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
