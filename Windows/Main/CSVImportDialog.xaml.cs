using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using URLServerManagerModern.Utilities;
using URLServerManagerModern.Utilities.IO;

namespace URLServerManagerModern.Windows.Main
{
    public partial class CSVImportDialog : Window
    {
        CSVImporter importer;
        TaskQueue queue;
        public CSVImportDialog(CSVImporter importer, TaskQueue queue)
        {
            this.importer = importer;
            this.queue = queue;

            InitializeComponent();
        }

        public CSVImportDialog(CSVImporter importer, TaskQueue queue, string serversCSV) : this(importer, queue)
        {
            if (serversCSV.Contains("defaultCategories"))
                defaultCategories.Text = serversCSV;
            else if (serversCSV.Contains("serverContents"))
                serverContents.Text = serversCSV;
            else if (serversCSV.Contains("addresses"))
                addresses.Text = serversCSV;
            else
                servers.Text = serversCSV;
        }

        private void SelectFiles(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Properties.Resources.CSVStructureFile + "|*.csv";
            ofd.Title = Properties.Resources.ResourceManager.GetString("OFDSelectFile");
            ofd.CheckFileExists = true;

            if (ofd.ShowDialog() == true)
            {
                switch ((sender as FrameworkElement).Name)
                {
                    case "serversButton":
                        servers.Text = ofd.FileName;
                        break;
                    case "addressesButton":
                        addresses.Text = ofd.FileName;
                        break;
                    case "sContButton":
                        serverContents.Text = ofd.FileName;
                        break;
                    case "defaultCatButton":
                        defaultCategories.Text = ofd.FileName;
                        break;
                }
            }
        }

        private void CheckInputs()
        {
            bool atLeastOneError = false;

            errors.Visibility = Visibility.Collapsed;
            errors.ToolTip = "";
            if (!File.Exists(servers.Text.Trim()))
            {
                atLeastOneError = true;
                errors.ToolTip += "servers.csv: " + Properties.Resources.FileDoesNotExist;
                errors.Visibility = Visibility.Visible;
            }

            if (!File.Exists(addresses.Text.Trim()))
            {
                atLeastOneError = true;
                errors.ToolTip += "addresses.csv: " + Properties.Resources.FileDoesNotExist + Environment.NewLine;
                errors.Visibility = Visibility.Visible;
            }

            if (!File.Exists(serverContents.Text.Trim()))
            {
                errors.ToolTip += "serverContents.csv: " + Properties.Resources.FileDoesNotExist + Environment.NewLine;
                errors.Visibility = Visibility.Visible;
            }

            if (!File.Exists(defaultCategories.Text.Trim()))
            {
                errors.ToolTip += "defaultCategories.csv: " + Properties.Resources.FileDoesNotExist;
                errors.Visibility = Visibility.Visible;
            }

            ok.IsEnabled = !atLeastOneError;
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckInputs();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OnImportAccepted(object sender, RoutedEventArgs e)
        {
            Hide();
            await queue.Enqueue(() => Task.Run(() => importer.Import(servers.Text.Trim(), addresses.Text.Trim(), serverContents.Text.Trim(), defaultCategories.Text.Trim())));
            Close();
        }
    }
}
