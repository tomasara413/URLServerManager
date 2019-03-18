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
using static URLServerManagerModern.Utilities.Utilities;

namespace URLServerManagerModern.Windows.Main
{
    public partial class SynchronizationConnectionWindow : Window
    {
        public SynchronizationConnectionWindow()
        {
            InitializeComponent();

            hostname.Text = GetPropertyValue("remotelocation");
            database.Text = GetPropertyValue("remotedatabase");

            string property = GetPropertyValue("remoteport");
            int parsedInt;

            if (property != null && int.TryParse(property, out parsedInt))
                port.Text = property;
            else
                port.Text = "3306";
        }


        private void AllowOnlyInteger(object sender, TextCompositionEventArgs e)
        {
            int i;
            e.Handled = !int.TryParse((sender as TextBox).Text + e.Text, out i);
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Sync.IsEnabled = !string.IsNullOrEmpty(port.Text) && !string.IsNullOrEmpty(database.Text) && !string.IsNullOrEmpty(hostname.Text);
        }

        private void Synchronize(object sender, RoutedEventArgs e)
        {
            int portI;
            if (int.TryParse(port.Text, out portI))
            {
                Status status = WatcherWindow.GetStatus(new ProtocolAddress("MySQL Protocol", hostname.Text, portI));
                if (status == Status.Ok)
                {
                    SynchronizationLoginPopup slp = new SynchronizationLoginPopup();
                    if (slp.ShowDialog() == true)
                    {
                        SynchronizationScale ss = SynchronizationScale.Full;

                        if (DLAll.IsChecked == true)
                            ss = SynchronizationScale.RecoverEntities | SynchronizationScale.RecoverDefaultColors;
                        else if (DlDef.IsChecked == true)
                            ss = SynchronizationScale.RecoverDefaultColors;
                        else if (UDef.IsChecked == true)
                            ss = SynchronizationScale.UpdateDefaultColors;
                        else if (DlEn.IsChecked == true)
                            ss = SynchronizationScale.RecoverEntities;

                        SynchronizationConflictResolutionWindow scrw = new SynchronizationConflictResolutionWindow(slp.ID, slp.password, database.Text, hostname.Text, portI, ss);
                        scrw.ShowActivated = true;
                        scrw.Show();
                        Close();
                    }
                }
                else
                    MessageBox.Show(Properties.Resources.SynchronizationFailed + " " + status, Properties.Resources.SynchronizationFailure, MessageBoxButton.OK);
            }
        }
    }
}
