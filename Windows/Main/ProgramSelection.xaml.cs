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

namespace URLServerManagerModern.Windows.Main
{
    public partial class ProgramSelection : Window
    {
        ProtocolAddress pa;
        public ProgramSelection(ProtocolAddress address, Program[] programs)
        {
            InitializeComponent();
            pa = address;

            if (programs.Length == 1)
            {
                SelectionList.Items.Add(programs[0]);
                SelectionList.Items.Refresh();
                SelectionList.SelectedItem = programs[0];
                OpenConnection(null, null);
            }
            else
            {
                SelectionList.ItemsSource = programs;
                SelectionList.Items.Refresh();
                SelectionList.SelectedItem = programs[0];
            }
        }

        private void OpenConnection(object sender, RoutedEventArgs e)
        {
            Program p = SelectionList.SelectedItem as Program;
            string args = p.associations.Where(x => x.protocol == pa.protocol).First().cmdArguments.Replace("{address}", pa.address).Replace("{port}", pa.port.ToString());

            Hide();

            try
            {
                Process.Start(p.FilePath, args + " " + pa.parameters);
            }
            catch (Exception ex)
            {
                Utilities.Utilities.Log("[ERROR] Opening System Process", ex.ToString());
            }

            Close();
        }

        private void selectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Select.IsEnabled = SelectionList.SelectedItem != null;
        }

        private void SelectParent(object sender, MouseButtonEventArgs e)
        {
            SelectionList.SelectedItem = (sender as ScrollViewer).DataContext as Program;
        }
    }
}
