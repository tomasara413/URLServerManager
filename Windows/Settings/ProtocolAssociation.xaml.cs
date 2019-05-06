using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

namespace URLServerManagerModern.Windows.Settings
{
    //TODO: Protcet against dual protocol entries or make it a feature, I dunno
    public partial class ProtocolAssociation : Window
    {
        Window parentWindow;
        EventHandler closedEvent;
        public ProtocolAssociation(string additionalTitle, Window window)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(additionalTitle?.Trim()))
                Title += " - " + additionalTitle;

            parentWindow = window ?? throw new Exception("Program association property cannot be null");

            window.Closed += closedEvent = (o, e) => Close();

            List<string> protocols = Utilities.Utilities.GatherProtocols();
            for (int i = 0; i < protocols.Count; i++)
                Protocols.Items.Insert(Protocols.Items.Count - 1, protocols[i]);

            CheckInputs();
        }



        ProtocolArgumentAssociation edited;
        public ProtocolAssociation(string additionalTitle, Window pa, ProtocolArgumentAssociation paa) : this(additionalTitle, pa)
        {
            if (paa == null)
                return;
            edited = paa;

            if(!Protocols.Items.Contains(edited.protocol))
                Protocols.Items.Add(edited.protocol);

            Protocols.SelectedItem = edited.protocol;
            Parameters.Text = edited.cmdArguments;
        }

        public delegate void AssociationSaved(ProtocolArgumentAssociation paa);
        public event AssociationSaved OnAssociationSaved;
        private void SaveOrAddAssociation(object sender, RoutedEventArgs e)
        {
            if (edited == null)
                edited = new ProtocolArgumentAssociation(Protocols.SelectedItem.ToString(), Parameters.Text.Trim());
            else
            {
                edited.protocol = Protocols.SelectedItem.ToString();
                edited.cmdArguments = Parameters.Text.Trim();
            }

            OnAssociationSaved?.Invoke(edited);
            Close();
        }

        public void AddOtherProtocol(string protocol)
        {
            if(!Protocols.Items.Contains(protocol))
                Protocols.Items.Insert(Protocols.Items.Count - 1, protocol);
            Protocols.SelectedItem = protocol;
        }

        private void CheckInputs()
        {
            bool atLeastOneError = false;

            if (Protocols.SelectedItem == null || Protocols.SelectedIndex == Protocols.Items.Count - 1 || string.IsNullOrEmpty(Protocols.SelectedItem.ToString()) || string.IsNullOrWhiteSpace(Protocols.SelectedItem.ToString()))
                atLeastOneError = true;

            if (string.IsNullOrEmpty(Parameters.Text) || string.IsNullOrWhiteSpace(Parameters.Text) || !(Parameters.Text.Contains("{address}") && Parameters.Text.Contains("{port}")))
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
        
        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (ibp != null)
                ibp.Close();

            parentWindow.Closed -= closedEvent;
        }

        InputBoxPopup ibp;
        private void OpenOther(object sender, MouseButtonEventArgs e)
        {
            if (ibp == null)
            {
                ibp = new InputBoxPopup(Properties.Resources.AddOtherProtocol, Properties.Resources.OtherProtocolName);
                ibp.OnOptionAccepted += AddOtherProtocol;
                ibp.Closed += UnregisterOther;
                ibp.Show();
            }

            ibp.Activate();
        }

        private void UnregisterOther(object sender, EventArgs e)
        {
            ibp.OnOptionAccepted -= AddOtherProtocol;
            ibp.Closed -= UnregisterOther;
            ibp = null;
        }
    }
}
