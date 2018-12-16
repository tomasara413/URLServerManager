using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
    public partial class ServerWindow : Window
    {
        EventHandler closedEvent;
        Window parentWindow;
        public ServerWindow(string additionalTitle, Window parentWindow)
        {
            InitializeComponent();
            this.parentWindow = parentWindow;

            if (!string.IsNullOrEmpty(additionalTitle) && !string.IsNullOrWhiteSpace(additionalTitle))
                Title += " - " + additionalTitle;

            parentWindow.Closed += closedEvent = (s, e) => Close();

            Addresses.ItemsSource = new List<ProtocolAddress>();

            BorderColor.Background = new SolidColorBrush(Color.FromRgb(255,255,255));
            BackgroundColor.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            TextColor.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            List<string> validCategories = Utilities.Utilities.GatherCategories();
            
            foreach (string cat in validCategories)
                Categories.Items.Insert(Categories.Items.Count - 1, cat);
        }

        PseudoServer original;
        public ServerWindow(string additionalTitle, Window parentWindow, PseudoServer original) : this(additionalTitle, parentWindow)
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

                IsVirtual.IsChecked = original.type == EntityType.VirtualServer;
            }

            CheckProperties();
        }

        bool otherOpen = false;
        InputBoxPopup ibp;
        private void OpenOther(object sender, MouseButtonEventArgs e)
        {
            if (!otherOpen)
            {
                ibp = new InputBoxPopup(Properties.Resources.AddOtherCategory, Properties.Resources.OtherCategoryName);
                ibp.OnOptionAccepted += AddOtherCategory;
                ibp.Closed += UnregisterOther;
                otherOpen = true;
                ibp.Show();
            }
            else
                ibp.Activate();
        }

        private void UnregisterOther(object s, EventArgs e)
        {
            otherOpen = false;
            ibp.OnOptionAccepted -= AddOtherCategory;
            ibp.Closed -= UnregisterOther;
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
            if (string.IsNullOrEmpty(FQDN.Text.Trim()) && !IPAddress.TryParse(pa.hostname, out ip))
                FQDN.Text = pa.hostname;
            CheckProperties();
            Addresses.Items.Refresh();
        }

        List<ProtocolAddress> removedAddressses = new List<ProtocolAddress>();
        private void RemoveAddress(object sender, RoutedEventArgs e)
        {
            ProtocolAddress pa = Addresses.SelectedItem as ProtocolAddress;
            if(pa.rowID >= 0)
                removedAddressses.Add(pa);
            (Addresses.ItemsSource as List<ProtocolAddress>).Remove(pa);
            Addresses.Items.Refresh();
            CheckProperties();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            isClosing = true;
            if (otherOpen)
                ibp.Close();

            foreach (KeyValuePair<object, ColorPickerWindow> cpw in cpwhs)
                cpw.Value.Close();

            parentWindow.Closed -= closedEvent;
        }

        public delegate void ServerSaved(PseudoServer s, List<ProtocolAddress> removedAddresses);
        public event ServerSaved OnServerSaved;
        
        public void SaveOrAddServer(object sender, RoutedEventArgs e)
        {
            PseudoServer edited = new PseudoServer(ModificationDetector.Modified, new Server(FQDN.Text.Trim(), Categories.SelectedItem?.ToString(), Description.Text.Trim()));

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

            edited.type = (bool)IsVirtual.IsChecked ? EntityType.VirtualServer : EntityType.Server;

            if (original != null)
            {
                if (edited.Equals(original))
                {
                    Close();
                    return;
                }
            }
            else
                original = new PseudoServer(ModificationDetector.New, new Server(FQDN.Text.Trim(), Categories.SelectedItem?.ToString(), Description.Text.Trim()));

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

            OnServerSaved?.Invoke(original, removedAddressses);
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void CheckProperties()
        {
            bool atLeastOneError = (Addresses.ItemsSource as List<ProtocolAddress>).Count == 0;

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
                //TODO: Apply to subsections of settings which had to have all these mecahnics handled on their own
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
    }
}
