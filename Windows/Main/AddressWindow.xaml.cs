using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
using URLServerManagerModern.Windows.Utility;

namespace URLServerManagerModern.Windows.Main
{
    public partial class AddressWindow : Window
    {
        Window parentWindow;
        EventHandler closedEvent;
        public AddressWindow(string additionalTitle, Window parent)
        {
            InitializeComponent();
            parentWindow = parent;

            if (!string.IsNullOrEmpty(additionalTitle) && !string.IsNullOrWhiteSpace(additionalTitle))
                Title += " - " + additionalTitle;

            parent.Closed += closedEvent = (s, e) => Close();

            List<string> protocols = Utilities.Utilities.GatherProtocols();
            for (int i = 0; i < protocols.Count; i++)
                Protocols.Items.Insert(Protocols.Items.Count - 1, protocols[i]);
        }

        private ProtocolAddress edited;
        public AddressWindow(string additionalTitle, Window parent, ProtocolAddress edited) : this(additionalTitle, parent)
        {
            if (edited != null)
            {
                this.edited = edited;

                if(!Protocols.Items.Contains(edited.protocol))
                    Protocols.Items.Insert(Protocols.Items.Count - 1, edited.protocol);
                Protocols.SelectedItem = edited.protocol;

                Address.Text = edited.hostname;
                Port.Text = edited.port.ToString();
                if(!string.IsNullOrEmpty(edited.parameters))
                    AdditionalParameters.Text = edited.parameters.ToString();
            }
        }

        public delegate void AddressSaved(ProtocolAddress pa);
        public event AddressSaved OnAddressSaved;

        private void SaveOrAdd(object sender, RoutedEventArgs e)
        {
            if (edited == null)
                edited = new ProtocolAddress();

            edited.protocol = Protocols.SelectedItem.ToString();
            edited.hostname = Address.Text.Trim();
            if (!string.IsNullOrEmpty(Port.Text))
                edited.port = int.Parse(Port.Text);
            else
                edited.port = DataHolder.protocolToPort[edited.protocol];

            string parameters = AdditionalParameters.Text.Trim();
            if (!string.IsNullOrEmpty(parameters))
                edited.parameters = parameters;

            OnAddressSaved?.Invoke(edited);

            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (otherOpen)
                ibp.Close();

            parentWindow.Closed -= closedEvent;
        }


        bool otherOpen = false;
        InputBoxPopup ibp;
        private void OpenOther(object sender, MouseButtonEventArgs e)
        {
            if (!otherOpen)
            {
                ibp = new InputBoxPopup(Properties.Resources.AddOtherCategory, Properties.Resources.OtherCategoryName);
                ibp.OnOptionAccepted += AddOtherProtocol;
                ibp.Closed += UnregisterOther;
                otherOpen = true;
                ibp.ShowActivated = true;
                ibp.Show();
            }
            else
                ibp.Activate();
        }

        private void UnregisterOther(object s, EventArgs e)
        {
            otherOpen = false;
            ibp.OnOptionAccepted -= AddOtherProtocol;
            ibp.Closed -= UnregisterOther;
        }

        private void AddOtherProtocol(string category)
        {
            if (!Protocols.Items.Contains(category))
                Protocols.Items.Insert(Protocols.Items.Count - 1, category);

            Protocols.SelectedItem = category;
        }

        private void AllowOnlyPositiveInteger(object sender, TextCompositionEventArgs e)
        {
            int i;
            e.Handled = !int.TryParse((sender as TextBox).Text + e.Text, out i) && i <= 0;
        }

        private void AddressTextChanged(object sender, TextChangedEventArgs e)
        {
            CheckInputs();
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckInputs();
        }

        public void CheckInputs()
        {
            bool atLeastOneError = false;
            ErrorsTooltip.ToolTip = "";
            ErrorsTooltip.Visibility = Visibility.Collapsed;

            string protocol = Protocols.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(protocol) || Protocols.SelectedIndex == Protocols.Items.Count - 1)
            {
                atLeastOneError = true;
                ErrorsTooltip.ToolTip = Properties.Resources.ProtocolCannotBeEmpty;
                ErrorsTooltip.Visibility = Visibility.Visible;
            }
            else
            {
                string port = Port.Text;
                if (string.IsNullOrEmpty(port) && !DataHolder.protocolToPort.ContainsKey(protocol))
                {
                    atLeastOneError = true;
                    ErrorsTooltip.ToolTip = Properties.Resources.PortUnassociatedAndEmpty;
                    ErrorsTooltip.Visibility = Visibility.Visible;
                }
            }

            string address = Address.Text.Trim();
            if (string.IsNullOrEmpty(address))
            {
                atLeastOneError = true;
                ErrorsTooltip.ToolTip += Environment.NewLine + Properties.Resources.AddressCannotBeEmpty;
                ErrorsTooltip.Visibility = Visibility.Visible;
            }
            else
            {
                try
                {
                    IPAddress ipa;
                    IdnMapping idnm = new IdnMapping();
                    if (!IPAddress.TryParse(address, out ipa))
                    {
                        if (!Utilities.Utilities.ResolveHostname(idnm.GetAscii(address), 60))
                        {
                            ErrorsTooltip.ToolTip += Environment.NewLine + Properties.Resources.UnableToResolveOrParse;
                            ErrorsTooltip.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        if (ipa.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if (ipa.GetAddressBytes()[0] == 0)
                            {
                                atLeastOneError = true;
                                ErrorsTooltip.ToolTip += Environment.NewLine + Properties.Resources.NotValidDestinationAddress;
                                ErrorsTooltip.Visibility = Visibility.Visible;
                            }
                        }
                        else if (ipa.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        {
                            byte[] bytes = ipa.GetAddressBytes();
                            if (bytes.Count(x => x > 1) == 0)
                            {
                                atLeastOneError = true;
                                ErrorsTooltip.ToolTip += Environment.NewLine + Properties.Resources.NotValidDestinationAddress;
                                ErrorsTooltip.Visibility = Visibility.Visible;
                            }
                        }
                    }

                }
                catch (ArgumentException e)
                {
                    ErrorsTooltip.ToolTip += Environment.NewLine + Properties.Resources.UnableToResolveOrParse;
                    ErrorsTooltip.Visibility = Visibility.Visible;
                }
            }

            

            ErrorsTooltip.ToolTip = ErrorsTooltip.ToolTip.ToString().Trim();

            Ok.IsEnabled = !atLeastOneError;
        }
    }
}
