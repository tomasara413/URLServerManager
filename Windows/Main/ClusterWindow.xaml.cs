using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
using URLServerManagerModern.Data;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;
using URLServerManagerModern.Utilities;
using URLServerManagerModern.Windows.Utility;

namespace URLServerManagerModern.Windows.Main
{
    public partial class ClusterWindow : Window
    {
        EventHandler closedEvent;
        Window parentWindow;
        bool isCluster;
        public ClusterWindow(bool isClusterWindow, string additionalTitle, Window parentWindow)
        {
            InitializeComponent();
            this.parentWindow = parentWindow;

            isCluster = isClusterWindow;
            if (!isClusterWindow)
                Title = Properties.Resources.VirtualizationServerWindow;

            if (!string.IsNullOrEmpty(additionalTitle) && !string.IsNullOrWhiteSpace(additionalTitle))
                Title += " - " + additionalTitle;

            parentWindow.Closed += closedEvent = (s, e) => Close();

            Addresses.ItemsSource = new List<ProtocolAddress>();
            ServersInCollection.ItemsSource = new List<PseudoEntity>();
            //ServersAvailable.ItemsSource = new List<PseudoEntity>();

            BorderColor.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            BackgroundColor.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            TextColor.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            //TODO: Maybe cache loaded servers to shared storage accessible by rowid
            SetAvailableServers(Utilities.Utilities.LoadServers(0, DataHolder.LIMIT));

            List<string> validCategories = Utilities.Utilities.GatherCategories();

            foreach (string cat in validCategories)
                Categories.Items.Insert(Categories.Items.Count - 1, cat);
        }

        PseudoWrappingEntity original;
        public ClusterWindow(bool isClusterWindow, string additionalTitle, Window parentWindow, PseudoWrappingEntity original) : this(isClusterWindow, additionalTitle, parentWindow)
        {
            if (original != null)
            {
                this.original = original;

                FQDN.Text = original.server.fqdn;
                Categories.SelectedItem = original.server.category;
                Description.Text = original.server.desc;
                Addresses.ItemsSource = original.server.protocolAddresses.DeepCopy();

                UseBGC.IsChecked = original.usesFill;
                UseBC.IsChecked = original.usesBorder;
                UseTC.IsChecked = original.usesText;

                ColorUsageChecked(UseBGC, null);
                ColorUsageChecked(UseBC, null);
                ColorUsageChecked(UseTC, null);

                BorderColor.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(original.customBorderColor);
                BackgroundColor.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(original.customBackgroundColor);
                TextColor.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(original.customTextColor);

                SetAvailableServers(ServersAvailable.ItemsSource as List<PseudoEntity>);
                AddPreselectedContents(new List<PseudoEntity>(original.computersInCluster));
            }

            CheckProperties();
        }

        private void SetAvailableServers(List<PseudoEntity> servers)
        {
            if (servers != null)
            {
                offset = servers.Count;
                ServersAvailable.ItemsSource = RemoveUnsuitableServers(servers, isCluster, true);
                endOfContent = servers.Count % DataHolder.LIMIT != 0;
                //Debug.WriteLine("Is itemsource the list passed: {0}", servers == ServersAvailable.ItemsSource);
                ServersAvailable.Items.Refresh();
            }
        }

        private async void SetAvailableServersAsync(List<PseudoEntity> servers)
        {
            if (servers != null)
            {
                offset = servers.Count;
                ServersAvailable.ItemsSource = await RemoveUnsuitableServersAsync(servers, isCluster, true);
                endOfContent = servers.Count % DataHolder.LIMIT != 0;
                //Debug.WriteLine("Is itemsource the list passed: {0}", servers == ServersAvailable.ItemsSource);
                ServersAvailable.Items.Refresh();
            }
        }

        public ClusterWindow(bool isClusterWindow, string additionalTitle, Window parentWindow, PseudoWrappingEntity original, List<PseudoEntity> contentsOfCollection) : this(isClusterWindow, additionalTitle, parentWindow, original)
        {
            AddPreselectedContents(contentsOfCollection);
        }

        public ClusterWindow(bool isClusterWindow, string additionalTitle, Window parentWindow, List<PseudoEntity> contentsOfCollection) : this(isClusterWindow, additionalTitle, parentWindow)
        {
            AddPreselectedContents(contentsOfCollection);
        }

        private void AddPreselectedContents(List<PseudoEntity> contents)
        {
            if (contents != null)
            {
                List<PseudoEntity> collection = ServersInCollection.ItemsSource as List<PseudoEntity>;
                collection.AddRange(RemoveUnsuitableServers(contents, isCluster));
                ServersInCollection.Items.Refresh();


                (ServersAvailable.ItemsSource as List<PseudoEntity>).RemoveAll(x => collection.Count(y => y.server.rowID == x.server.rowID) > 0);
                ServersAvailable.Items.Refresh();

                CheckProperties();
            }
        }

        InputBoxPopup ibp;
        private void OpenOther(object sender, MouseButtonEventArgs e)
        {
            if (ibp == null)
            {
                ibp = new InputBoxPopup(Properties.Resources.AddOtherCategory, Properties.Resources.OtherCategoryName);
                ibp.OnOptionAccepted += AddOtherCategory;
                ibp.Closed += UnregisterOther;
                ibp.Show();
            }

            ibp.Activate();
        }

        private void UnregisterOther(object s, EventArgs e)
        {
            ibp.OnOptionAccepted -= AddOtherCategory;
            ibp.Closed -= UnregisterOther;
            ibp = null;
        }

        private void AddOtherCategory(string category)
        {
            if (!Categories.Items.Contains(category))
                Categories.Items.Insert(Categories.Items.Count - 1, category);

            Categories.SelectedItem = category;
        }

        private void AddOrModifyAddress(object sender, RoutedEventArgs e)
        {
            AddressWindow aw;
            if ((sender as Button).Name == "Add")
            {
                aw = new AddressWindow(Properties.Resources.Add, this);
                aw.OnAddressSaved += AddAddress;
            }
            else
            {
                aw = new AddressWindow(Properties.Resources.Edit, this, Addresses.SelectedItem as ProtocolAddress);
                aw.OnAddressSaved += ModifyAddress;
            }
            aw.ShowActivated = true;
            aw.Show();
        }

        private void ModifyAddress(ProtocolAddress pa)
        {
            Addresses.Items.Refresh();
        }

        private void AddAddress(ProtocolAddress pa)
        {
            (Addresses.ItemsSource as List<ProtocolAddress>).Add(pa);
            IPAddress ip;
            if (string.IsNullOrEmpty(FQDN.Text) && !IPAddress.TryParse(pa.hostname, out ip))
                FQDN.Text = pa.hostname;
            CheckProperties();
            Addresses.Items.Refresh();
        }

        List<ProtocolAddress> removedAddressses = new List<ProtocolAddress>();
        private void RemoveAddress(object sender, RoutedEventArgs e)
        {
            ProtocolAddress pa = Addresses.SelectedItem as ProtocolAddress;
            if (pa.rowID >= 0)
                removedAddressses.Add(pa);
            (Addresses.ItemsSource as List<ProtocolAddress>).Remove(pa);
            Addresses.Items.Refresh();
            CheckProperties();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (ibp != null)
                ibp.Close();

            foreach (KeyValuePair<object, ColorPickerWindow> cpw in cpwhs)
                cpw.Value.Close();

            parentWindow.Closed -= closedEvent;
        }

        public delegate void ServerSaved(PseudoWrappingEntity s, List<ProtocolAddress> removedAddresses, List<PseudoEntity> removedServers);
        public event ServerSaved OnServerSaved;

        public void SaveOrAddServer(object sender, RoutedEventArgs e)
        {
            PseudoWrappingEntity edited = new PseudoWrappingEntity(ModificationDetector.Modified, new Server(FQDN.Text.Trim(), Categories.SelectedItem?.ToString(), Description.Text.Trim()), (ServersInCollection.ItemsSource as List<PseudoEntity>).ToArray());

            edited.server.fqdn = FQDN.Text.Trim();
            edited.server.category = Categories.SelectedItem?.ToString();
            edited.server.desc = Description.Text.Trim();
            edited.server.protocolAddresses = Addresses.ItemsSource as List<ProtocolAddress>;


            edited.usesFill = (bool)UseBGC.IsChecked;
            edited.usesBorder = (bool)UseBC.IsChecked;
            edited.usesText = (bool)UseTC.IsChecked;

            bool hasDefaultCategory = edited.server.category != null && DataHolder.categoryColors.ContainsKey(edited.server.category);

            edited.customBackgroundColor = edited.usesFill ? ((SolidColorBrush)BackgroundColor.Background).Color.ToString() : hasDefaultCategory ? DataHolder.categoryColors[edited.server.category].fillColor : "#00FFFFFF";
            edited.customBorderColor = edited.usesBorder ? ((SolidColorBrush)BorderColor.Background).Color.ToString() : hasDefaultCategory ? DataHolder.categoryColors[edited.server.category].borderColor : "#00FFFFFF";
            edited.customTextColor = edited.usesText ? ((SolidColorBrush)TextColor.Background).Color.ToString() : hasDefaultCategory ? DataHolder.categoryColors[edited.server.category].textColor : "#000000";

            edited.computersInCluster = ServersInCollection.ItemsSource as List<PseudoEntity>;
            edited.type = isCluster ? EntityType.Cluster : EntityType.VirtualizationServer;

            if (!isCluster)
            {
                if (MessageBox.Show(Properties.Resources.VirtualServerSwitch, Properties.Resources.VirtualServerSwitchCaption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    foreach (PseudoEntity pe in edited.computersInCluster)
                    {
                        if (pe.type == EntityType.Server)
                        {
                            pe.type = EntityType.VirtualServer;
                            Utilities.Utilities.SavePseudoEntity(pe, null, null);
                        }
                    }
                }
                else
                    return;
            }

            if (original == null)
            {
                //Checking each child invidually is essentially waste of resources and time, it is easier to save it no matter the child elements
                removedServers = null;
                original = new PseudoWrappingEntity(ModificationDetector.Modified, new Server(FQDN.Text.Trim(), Categories.SelectedItem?.ToString(), Description.Text.Trim()), (ServersInCollection.ItemsSource as List<PseudoEntity>).ToArray());
            }
            original.server.fqdn = edited.server.fqdn;
            original.server.category = edited.server.category;
            original.server.desc = edited.server.desc;
            original.server.protocolAddresses = edited.server.protocolAddresses;

            ProtocolAddress pa;
            for (int i = 0; i < original.server.protocolAddresses.Count; i++)
            {
                pa = original.server.protocolAddresses[i];
                if (pa.rowID < 0 && removedAddressses.Count > 0)
                {
                    pa.rowID = removedAddressses[0].rowID;
                    removedAddressses.RemoveAt(0);
                }
            }

            original.usesFill = edited.usesFill;
            original.usesBorder = edited.usesBorder;
            original.usesText = edited.usesText;

            original.customBackgroundColor = edited.customBackgroundColor;
            original.customBorderColor = edited.customBorderColor;
            original.customTextColor = edited.customTextColor;

            original.type = edited.type;
            original.computersInCluster = edited.computersInCluster;

            foreach (PseudoEntity en in addedServers)
                Utilities.Utilities.SavePseudoEntity(en, null, null);

            OnServerSaved?.Invoke(original, removedAddressses, removedServers);
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void CheckProperties()
        {
            bool atLeastOneError = false;
            if (!isCluster)
                atLeastOneError = (Addresses.ItemsSource as List<ProtocolAddress>).Count == 0;

            atLeastOneError = atLeastOneError || (isCluster ? (ServersInCollection.ItemsSource as List<PseudoEntity>).Count <= 1 : (ServersInCollection.ItemsSource as List<PseudoEntity>).Count == 0);

            Ok.IsEnabled = !atLeastOneError;
        }

        bool isClosing = false;
        Dictionary<object, ColorPickerWindow> cpwhs = new Dictionary<object, ColorPickerWindow>();
        private void SelectColor(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow cpw;
            if (!cpwhs.TryGetValue(sender, out cpw))
            {
                cpwhs.Add(sender, cpw = new ColorPickerWindow((sender as Button).Background as SolidColorBrush));
                cpw.Closed += (s, ea) => {
                    if (!isClosing)
                        cpwhs.Remove(sender);
                };
                cpw.Show();
            }
            else
                cpw.Activate();
        }

        private void ColorUsageChecked(object sender, RoutedEventArgs e)
        {
            CheckBox c = sender as CheckBox;
            switch (c.Name)
            {
                case "UseBC":
                    BorderColor.IsEnabled = (bool)c.IsChecked;
                    break;
                case "UseBGC":
                    BackgroundColor.IsEnabled = (bool)c.IsChecked;
                    break;
                case "UseTC":
                    TextColor.IsEnabled = (bool)c.IsChecked;
                    break;
            }
        }

        private void AddressesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Remove.IsEnabled = Modify.IsEnabled = Addresses.SelectedItem != null;
        }

        private void SelectParent(object sender, MouseButtonEventArgs e)
        {
            if (sender == null || e == null || e.OriginalSource == null)
                return;

            ListBox lb = sender as ListBox;

            if (lb != null)
            {
                DependencyObject o = lb.ContainerFromElement(e.OriginalSource as DependencyObject);

                if (o != null && (o as ListBoxItem).IsEnabled)
                {
                    lb.SelectedItem = o;
                    lb.SelectedIndex = lb.Items.IndexOf(e.OriginalSource);
                    (o as ListBoxItem).IsSelected = true;
                }
            }
        }

        private void ServerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveFromC.IsEnabled = ServersInCollection.SelectedItem != null;
            AddToC.IsEnabled = ServersAvailable.SelectedItem != null;

            ModifyS.IsEnabled = RemoveS.IsEnabled = ServersInCollection.SelectedItem != null && addedServers.Contains(ServersInCollection.SelectedItem);
        }

        List<PseudoEntity> addedServers = new List<PseudoEntity>();
        private void AddOrModifyServer(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ServerWindow sw;
            if (button.Name == "AddS")
            {
                sw = new ServerWindow(Properties.Resources.Add, this);
                sw.OnServerSaved += OnServerAdded;
                sw.ShowActivated = true;
                sw.Show();
            }
            else
            {
                sw = new ServerWindow(Properties.Resources.Edit, this, ServersInCollection.SelectedItem as PseudoServer);
                sw.OnServerSaved += OnServerModified;
                sw.ShowActivated = true;
                sw.Show();
            }
        }


        public void OnServerModified(PseudoServer s, List<ProtocolAddress> removed)
        {
            ServersInCollection.Items.Refresh();
        }

        public void OnServerAdded(PseudoServer s, List<ProtocolAddress> removed)
        {
            (ServersInCollection.ItemsSource as List<PseudoEntity>).Add(s);
            addedServers.Add(s);

            if (!isCluster)
                s.type = EntityType.VirtualServer;

            OnServerModified(s, removed);
            CheckProperties();
        }

        List<PseudoEntity> removedServers = new List<PseudoEntity>();
        private void RemoveServer(object sender, RoutedEventArgs e)
        {
            PseudoEntity pe = ServersInCollection.SelectedItem as PseudoEntity;
            addedServers.Remove(pe);
            (ServersInCollection.ItemsSource as List<PseudoEntity>).Remove(pe);
            ServersInCollection.Items.Refresh();
            CheckProperties();
        }

        private void RemoveFromCollection(object sender, RoutedEventArgs e)
        {
            PseudoEntity en = ServersInCollection.SelectedItem as PseudoEntity;
            (ServersInCollection.ItemsSource as List<PseudoEntity>).Remove(en);
            (ServersAvailable.ItemsSource as List<PseudoEntity>).Add(en);
            ServersInCollection.Items.Refresh();
            ServersAvailable.Items.Refresh();
            if(original != null && original.computersInCluster.Contains(en))
                removedServers.Add(en);
            CheckProperties();
        }

        private void AddToCollection(object sender, RoutedEventArgs e)
        {
            PseudoEntity en = ServersAvailable.SelectedItem as PseudoEntity;
            (ServersInCollection.ItemsSource as List<PseudoEntity>).Add(en);
            (ServersAvailable.ItemsSource as List<PseudoEntity>).Remove(en);
            ServersInCollection.Items.Refresh();
            ServersAvailable.Items.Refresh();
            removedServers.Remove(en);
            CheckProperties();
        }

        private int offset = 0;
        private async Task<List<PseudoEntity>> RemoveUnsuitableServersAsync(List<PseudoEntity> servers, bool isCluster, bool fillListUpToMax = false)
        {
            if (original == null)
                return servers;

            List<PseudoEntity> suitableList = new List<PseudoEntity>();
            foreach (PseudoEntity pe in servers)
            {
                if (!IsPotentiallyInfinitelyRecursive(pe, original.server.rowID))
                {
                    if (isCluster)
                        suitableList.Add(pe);
                    else
                    {
                        if(pe.type != EntityType.VirtualizationServer)
                            suitableList.Add(pe);
                    }
                }
            }

            

            List<PseudoEntity> fillup;
            while (fillListUpToMax && suitableList.Count < servers.Count)
            {
                fillup = await Utilities.Utilities.LoadServersAsync(offset, servers.Count - suitableList.Count > DataHolder.LIMIT ? DataHolder.LIMIT : servers.Count - suitableList.Count);
                offset += fillup.Count;

                suitableList.AddRange(await RemoveUnsuitableServersAsync(fillup, isCluster, true));
                if (fillup.Count == 0)
                    break;
            }

            return suitableList;
        }

        private List<PseudoEntity> RemoveUnsuitableServers(List<PseudoEntity> servers, bool isCluster, bool fillListUpToMax = false)
        {
            if (original == null)
                return servers;

            List<PseudoEntity> suitableList = new List<PseudoEntity>();
            foreach (PseudoEntity pe in servers)
            {
                if (!IsPotentiallyInfinitelyRecursive(pe, original.server.rowID))
                {
                    if (isCluster)
                        suitableList.Add(pe);
                    else
                    {
                        if (pe.type != EntityType.VirtualizationServer)
                            suitableList.Add(pe);
                    }
                }
            }



            List<PseudoEntity> fillup;
            while (fillListUpToMax && suitableList.Count < servers.Count)
            {
                fillup = Utilities.Utilities.LoadServers(offset, servers.Count - suitableList.Count > DataHolder.LIMIT ? DataHolder.LIMIT : servers.Count - suitableList.Count);
                offset += fillup.Count;

                suitableList.AddRange(RemoveUnsuitableServers(fillup, isCluster, true));
                if (fillup.Count == 0)
                    break;
            }

            return suitableList;
        }

        private bool IsPotentiallyInfinitelyRecursive(PseudoEntity pe, long rowid)
        {
            PseudoWrappingEntity pwe;
            if ((pwe = pe as PseudoWrappingEntity) != null)
            {
                if (pe.server.rowID == rowid)
                    return true;

                PseudoWrappingEntity pwe2;
                foreach (PseudoEntity pe2 in pwe.computersInCluster)
                {   
                    if ((pwe2 = pe2 as PseudoWrappingEntity) != null)
                    {
                        if (IsPotentiallyInfinitelyRecursive(pwe2, rowid))
                            return true;
                    }
                }
            }

            return false;
        }

        private bool fetchingData = false, endOfContent = false;
        List<PseudoEntity> loadedServers;

        private async void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!endOfContent && !fetchingData && e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight == e.ExtentHeight)
            {
                fetchingData = true;
                loadedServers = await RemoveUnsuitableServersAsync(await Utilities.Utilities.LoadServersAsync(ServersAvailable.Items.Count, DataHolder.LIMIT), isCluster, true);
                fetchingData = false;

                if (loadedServers.Count > 0)
                {
                    (ServersAvailable.ItemsSource as List<PseudoEntity>).AddRange(loadedServers);
                    ServersAvailable.Items.Refresh();
                }

                endOfContent = loadedServers.Count < DataHolder.LIMIT;
            }
        }
    }
}
