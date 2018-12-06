using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Drawing;
using URLServerManagerModern.Data;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;
using System.Linq;
using System.Threading.Tasks;

namespace URLServerManagerModern.Windows.Main
{
    public partial class MainWindow : Window
    {
        private List<PseudoEntity> servers = new List<PseudoEntity>();
        public MainWindow()
        {
            Utilities.Utilities.LoadOrCreateConfig();

            InitializeComponent();

            Utilities.Utilities.RefreshAllDynamicResources(this);

            OnLocalServerFileChanged();

            Utilities.Utilities.LoadCategoryColors();
        }

        public void SetServersContent(List<PseudoEntity> content)
        {
            if (content != null && content.Count > 0)
            {
                servers.Clear();
                servers.AddRange(content);
                mainServerWrapper.ItemsSource = servers;
                mainServerWrapper.Items.Refresh();
            }
        }

        public void AddPseudoEntity(PseudoEntity s)
        {
            servers.Add(s);
            mainServerWrapper.Items.Refresh();
        }

        public void AddListOfPseudoEntities(List<PseudoEntity> servers)
        {
            if (servers == null || servers.Count == 0)
                return;
            this.servers.AddRange(servers);
            mainServerWrapper.Items.Refresh();
        }

        private void FormKeyListener(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                switch (e.Key)
                {
                    case Key.S:
                        //sync/save as
                        break;
                    case Key.N:
                        NewServerStructure(null, null);
                        break;
                }
            }
        }

        private void OpenConnection(object sender, RoutedEventArgs e)
        {
            ProtocolAddress pa = (ProtocolAddress)(sender as FrameworkElement).DataContext;
            
            Program[] programs = DataHolder.programs.Where(x => x.associations.Where(y => y.protocol == pa.protocol).Count() > 0).ToArray();
            if (programs.Length > 0)
            {
                if (programs.Length > 1)
                {
                    ProgramSelection ps = new ProgramSelection(pa, programs);
                    ps.ShowActivated = true;
                    ps.Show();
                }
                else
                {
                    string args = programs[0].associations.Where(x => x.protocol == pa.protocol).First().cmdArguments.Replace("{address}", pa.address).Replace("{port}", pa.port.ToString());

                    Task t = new Task(() =>
                    {
                        try
                        {
                            Process.Start(programs[0].FilePath, args + " " + pa.parameters);
                        }
                        catch (Exception ex)
                        {
                            Utilities.Utilities.Log("[ERROR] Opening System Process", ex.ToString());
                        }
                    });

                    t.Start();
                }
            }
            else
            {
                if (MessageBox.Show(Properties.Resources.AssociateMessage, Properties.Resources.ProgramAssociation, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    OpenSettings(null, null);
                    s.SwitchSettingsTab(1);
                }
            }
        }

        private async void OpenServerStructure(object sender, RoutedEventArgs e)
        {
            string ofdFileName = Utilities.Utilities.OpenServerStructure((sender as FrameworkElement).Name.Equals("import"));

            if (!string.IsNullOrEmpty(ofdFileName?.Trim()))
            {
                if ((sender as FrameworkElement).Name == "local")
                {

                    if (Path.GetExtension(ofdFileName) == ".db")
                        Utilities.Utilities.SetPropertyValue("localfile", "\"" + ofdFileName + "\"");
                    else
                    {
                        //Debug.WriteLine(Path.GetDirectoryName(ofdFileName) + "; " + Path.GetFileNameWithoutExtension(ofdFileName));
                        Utilities.Utilities.SetPropertyValue("localfile", "\"" + Path.GetDirectoryName(ofdFileName) + "\\" + Path.GetFileNameWithoutExtension(ofdFileName) + ".db" + "\"");
                        await Utilities.Utilities.ConvertToDB(ofdFileName);
                    }
                    Utilities.Utilities.SaveSettings();
                }
                else
                {
                    if ((sender as FrameworkElement).Name == "remote")
                    {

                    }
                    else
                    {
                        if (Utilities.Utilities.GetPropertyValue("localfile") != null)
                        {
                            if (Path.GetExtension(ofdFileName) != ".db")
                                await Utilities.Utilities.ImportAsync(ofdFileName);
                        }
                    }
                }

                OnLocalServerFileChanged();
            }
        }

        private void NewServerStructure(object sender, RoutedEventArgs e)
        {
            Utilities.Utilities.NewServerStructure();

            OnLocalServerFileChanged();
        }

        private async void ExportServerStructure(object sender, RoutedEventArgs e)
        {
            await Utilities.Utilities.ExportAsync();
        }

        private async void OnLocalServerFileChanged()
        {
            import.IsEnabled = export.IsEnabled = editSubmenu.IsEnabled = toolSubMenu.IsEnabled = File.Exists(Utilities.Utilities.GetPropertyValue("localfile"));
            servers.Clear();
            if (import.IsEnabled)
            {
                mainServerWrapper.ItemsSource = servers = await Utilities.Utilities.LoadServersAsync(0, DataHolder.LIMIT);

                endOfContent = servers.Count % DataHolder.LIMIT != 0;
                mainServerWrapper.Items.Refresh();
            }
        }

        
        private FrameworkElement previousBG;
        private HashSet<FrameworkElement> selectedBackgrounds = new HashSet<FrameworkElement>();

        private void SelectServer(object sender, RoutedEventArgs e)
        {            
            DependencyObject parent = VisualTreeHelper.GetParent((DependencyObject)sender);
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                FrameworkElement t;
                if ((t = (FrameworkElement)VisualTreeHelper.GetChild(parent, i)).Name == "selectBG")
                {
                    t.Opacity = 0.2;

                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        if (!selectedBackgrounds.Add(t))
                        {
                            selectedBackgrounds.Remove(t);
                            t.Opacity = 0;
                        }
                    }
                    else
                    {
                        foreach (FrameworkElement fe in selectedBackgrounds)
                            fe.Opacity = 0;
                        selectedBackgrounds.Clear();

                        if (t.Opacity > 0)
                            selectedBackgrounds.Add(t);
                    }

                    previousBG = t;
                    break;
                }
            }

            removeServer.IsEnabled = editServer.IsEnabled = selectedBackgrounds.Count > 0;
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private Settings.Settings s;
        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            if (s == null)
            {
                s = new Settings.Settings();
                s.Closed += OnSettingsClosed;
                s.Show();
            }

            s.Activate();
        }

        private void OnSettingsClosed(object sender, EventArgs e)
        {
            s = null;
            Utilities.Utilities.RefreshAllDynamicResources(this);
            (sender as Window).Closed -= OnSettingsClosed;
        }

        private void AddOrModifyServer(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            switch (item.Name)
            {
                case "newServer":
                    ServerWindow sw = new ServerWindow(Properties.Resources.Add, this);
                    sw.OnServerSaved += OnServerAdded;
                    sw.ShowActivated = true;
                    sw.Show();
                    break;
                case "newVirtualizationServer":
                case "newCluster":
                    ClusterWindow cw;
                    if (selectedBackgrounds.Count > 0)
                        cw = new ClusterWindow(item.Name == "newCluster", Properties.Resources.Add, this, selectedBackgrounds.Select(x => x.DataContext).Cast<PseudoEntity>().ToList());
                    else
                        cw = new ClusterWindow(item.Name == "newCluster", Properties.Resources.Add, this);
                    cw.OnServerSaved += OnWrapperAdded;
                    cw.ShowActivated = true;
                    cw.Show();
                    break;
                case "editServer":
                    foreach (FrameworkElement fe in selectedBackgrounds)
                    {
                        PseudoServer ps;
                        if ((ps = fe.DataContext as PseudoServer) != null)
                        {
                            sw = new ServerWindow(Properties.Resources.Edit, this, fe.DataContext as PseudoServer);
                            sw.OnServerSaved += OnServerModified;
                            sw.ShowActivated = true;
                            sw.Show();
                        }
                        else
                        {
                            PseudoWrappingEntity pwe = fe.DataContext as PseudoWrappingEntity;
                            cw = new ClusterWindow(pwe.type == EntityType.Cluster, Properties.Resources.Edit, this, pwe);
                            cw.OnServerSaved += OnWrapperModified;
                            cw.ShowActivated = true;
                            cw.Show();
                        }
                    }
                    break;
            }
        }

        public void OnServerModified(PseudoServer s, List<ProtocolAddress> removed)
        {
            mainServerWrapper.Items.Refresh();

            Utilities.Utilities.SavePseudoEntityAsync(s, removed, null).ConfigureAwait(false);
        }

        public void OnServerAdded(PseudoServer s, List<ProtocolAddress> removed)
        {
            servers.Add(s);
            
            OnServerModified(s, removed);
        }

        public void OnWrapperModified(PseudoWrappingEntity s, List<ProtocolAddress> removed, List<PseudoEntity> removedEntities)
        {
            mainServerWrapper.Items.Refresh();

            Utilities.Utilities.SavePseudoEntityAsync(s, removed, removedEntities).ConfigureAwait(false);
        }

        public void OnWrapperAdded(PseudoWrappingEntity s, List<ProtocolAddress> removed, List<PseudoEntity> removedEntities)
        {
            servers.Add(s);

            OnWrapperModified(s, removed, removedEntities);
        }

        private void RemoveServer(object sender, RoutedEventArgs e)
        {
            foreach (FrameworkElement fe in selectedBackgrounds)
            {
                if (MessageBox.Show(Properties.Resources.RemoveServerMessage, Properties.Resources.RemoveServer, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    PseudoEntity s = fe.DataContext as PseudoEntity;
                    servers.Remove(s);
                    if (s.server.rowID >= 0)
                        Utilities.Utilities.RemovePseudoEntityAsync(s).ConfigureAwait(false);
                }
            }
            mainServerWrapper.Items.Refresh();
        }

        private void SelectParent(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement sw = sender as FrameworkElement;
            if (sw != null)
                SelectServer(LogicalTreeHelper.GetParent(sw), null);
        }

        //opens watcher - tool to watch over open ports
        WatcherWindow watcher;
        private void OpenWatcher(object sender, RoutedEventArgs e)
        {
            if (watcher == null)
            {
                watcher = new WatcherWindow();
                watcher.Closed += OnWatcherClosed;
                watcher.Show();
            }
            watcher.Activate();   
        }

        private void OnWatcherClosed(object sender, EventArgs e)
        {
            (sender as Window).Closed -= OnWatcherClosed;
            watcher = null;
        }


        private bool fetchingData = false, endOfContent = false;
        List<PseudoEntity> loadedServers;
        private async void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!endOfContent && !fetchingData && e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight == e.ExtentHeight)
            {
                fetchingData = true;
                loadedServers = await Utilities.Utilities.LoadServersAsync(servers.Count, DataHolder.LIMIT);
                fetchingData = false;

                if (loadedServers.Count > 0)
                {
                    servers.AddRange(loadedServers);
                    mainServerWrapper.Items.Refresh();
                }

                endOfContent = loadedServers.Count < DataHolder.LIMIT;
            }
        }
    }
}
