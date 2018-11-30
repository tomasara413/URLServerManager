using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using URLServerManagerModern.Data;
using URLServerManagerModern.Data.DataTypes.Pseudo;
using URLServerManagerModern.Windows.Utility;

namespace URLServerManagerModern.Windows.Settings
{
    public partial class PortAssociation : Window
    {
        private Settings settings;
        private ProtocolPortAssociation ppa;
        EventHandler closedEvent;
        public PortAssociation(string additionalTitle, Settings settings)
        {
            InitializeComponent();

            this.settings = settings;

            //PendingCancelation = false;

            if (settings == null)
                throw new ArgumentNullException("Settings property cannot be null");

            if (!string.IsNullOrEmpty(additionalTitle) && !string.IsNullOrWhiteSpace(additionalTitle))
                Title += " - " + additionalTitle;

            settings.Closed += closedEvent = (o, e) => Close();

            List<string> protocols = Utilities.Utilities.GatherProtocols();
            Debug.WriteLine(protocols.Count);
            for (int i = DataHolder.protocolToPort.Count; i < protocols.Count; i++)
                Protocols.Items.Insert(Protocols.Items.Count - 1, protocols[i]);

            CheckInputs();
        }

        public PortAssociation(string additionalTitle, Settings settings, ProtocolPortAssociation ppa) : this(additionalTitle, settings)
        {
            if (ppa != null)
            {
                this.ppa = ppa;

                if (!Protocols.Items.Contains(ppa.protocol))
                    Protocols.Items.Insert(Protocols.Items.Count - 1, ppa.protocol);
                Protocols.SelectedItem = ppa.protocol;
                Port.Text = ppa.port.ToString();
            }

            CheckInputs();
        }

        private void CheckInputs()
        {
            bool atLeastOneError = false;

            if (Protocols.SelectedItem == null || string.IsNullOrEmpty(Protocols.SelectedItem.ToString()) || string.IsNullOrWhiteSpace(Protocols.SelectedItem.ToString()))
                atLeastOneError = true;

            if (string.IsNullOrEmpty(Port.Text) || string.IsNullOrWhiteSpace(Port.Text))
                atLeastOneError = true;

            OkButton.IsEnabled = !atLeastOneError;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckInputs();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            CheckInputs();
        }

        private void AllowOnlyInteger(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox)
            {
                int i;
                if (!int.TryParse((sender as TextBox).Text + e.Text, out i))
                {
                    e.Handled = true;
                }
            }
        }

        //public bool PendingCancelation { get; set; }
        private void OnWindowClosed(object sender, EventArgs e)
        {
            /*if (!PendingCancelation)
                settings.GetAssociationWindows().Remove(this);*/

            if (otherOpen && ibp != null)
                ibp.Close();

            settings.Closed -= closedEvent;

            settings.CheckAllSettings();
        }

        private void SaveOrAddAssociation(object sender, RoutedEventArgs e)
        {
            if (ppa != null)
            {
                ppa.protocol = Protocols.SelectedItem.ToString().Trim();
                ppa.port = int.Parse(Port.Text);
            }
            else
            {
                ppa = new ProtocolPortAssociation(Protocols.SelectedItem.ToString().Trim(), int.Parse(Port.Text));

                (settings.PortAssociationsBox.ItemsSource as List<ProtocolPortAssociation>).Add(ppa);
            }

            settings.PortAssociationsBox.Items.Refresh();
            Close();
        }

        private void DoNotAllowPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                int i;
                if (!int.TryParse((string)e.DataObject.GetData(typeof(string)), out i))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        bool otherOpen = false;
        InputBoxPopup ibp;
        private void OpenOther(object sender, MouseButtonEventArgs e)
        {
            if (!otherOpen)
            {
                ibp = new InputBoxPopup(Properties.Resources.AddOtherProtocol, Properties.Resources.OtherProtocolName);
                ibp.OnOptionAccepted += AddOtherProtocol;
                ibp.Closed += UnregisterOther;
                otherOpen = true;
                ibp.Show();
            }
        }

        private void UnregisterOther(object s, EventArgs e)
        {
            otherOpen = false;
            ibp.OnOptionAccepted -= AddOtherProtocol;
            ibp.Closed -= UnregisterOther;
        }

        public void AddOtherProtocol(string protocol)
        {
            if ((settings.PortAssociationsBox.ItemsSource as List<ProtocolPortAssociation>).Count(x => x.protocol == protocol) > 0)
            {
                MessageBox.Show(Properties.Resources.DefaultProtocolAlreadyPresent, Properties.Resources.Error, MessageBoxButton.OK);
                return;
            }
            if(!Protocols.Items.Contains(protocol))
                Protocols.Items.Insert(Protocols.Items.Count - 1, protocol);
            Protocols.SelectedItem = protocol;
        }
    }
}
