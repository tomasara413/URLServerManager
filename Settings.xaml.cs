using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using URLServerManager.Datatypes;

namespace URLServerManager
{
    /// <summary>
    /// Interakční logika pro Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private MainWindow mw;
        private int elementsBefore;
        private Brush[] activeColor = new Brush[2];
        private Brush[] originalColor = new Brush[2];
        Thickness oth;
        private bool forceAssSave = false;

        public Settings(MainWindow mw, List<string> passedProtocols)
        {
            elementsBefore = DataHolder.protocolToProgram.Count;

            InitializeComponent();

            local.Text = DataHolder.localFile;
            remote.Text = DataHolder.remoteFile;
            assBox.ItemsSource = DataHolder.protocolToProgram;

            List<string> combolist = passedProtocols;
            if(!combolist.Contains("jiné..."))
                combolist.Add("jiné...");
            protocols.ItemsSource = combolist;

            activeColor[0] = generalB.Background;
            activeColor[1] = generalB.BorderBrush;
            originalColor[0] = assocB.Background;
            originalColor[1] = assocB.BorderBrush;
            generalB.BorderThickness = new Thickness(0);
            oth = assocB.BorderThickness;

            this.mw = mw;
        }

        private void addLocalFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.IsFolderPicker = true;
            ofd.Title = "Vyberte složku s strukturou serverů ve formátu XML";
            ofd.Filter = "Sourbory Typu XML|*.xml";

            if (ofd.ShowDialog() == true)
            {
                local.Text = ofd.FileName;
            }
        }

        private void addRemoteFile(object sender, RoutedEventArgs e)
        {
            //CommonOpenFileDialog ofd = new CommonOpenFileDialog();
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.IsFolderPicker = true;
            ofd.Title = "Vyberte složku s strukturou serverů ve formátu XML";
            ofd.Filter = "Sourbory Typu XML|*.xml";

            if (ofd.ShowDialog() == true)
            {
                remote.Text = ofd.FileName;
            }
        }

        private void button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void button_Save(object sender, RoutedEventArgs e)
        {
            if (File.Exists(local.Text))
            {
                DataHolder.localFile = local.Text;
                Utilities.setPropertyValue("localfile", "\"" + local.Text + "\"");
            }
            if (File.Exists(remote.Text))
            {
                DataHolder.remoteFile = remote.Text;
                Utilities.setPropertyValue("remotefile", "\"" + remote.Text + "\"");
            }
            if (DataHolder.protocolToProgram.Count != elementsBefore || forceAssSave)
            {
                string associations = "[";
                for (int i = 0; i < DataHolder.protocolToProgram.Count; i++)/*(protocolProgramAssociation ppa in DataHolder.protocolToProgram)*/
                {
                    associations += DataHolder.protocolToProgram[i].protocol + "-\"" + DataHolder.protocolToProgram[i].filePath + "\"-\"" + DataHolder.protocolToProgram[i].cmdArguments.Replace("\"", "'") + "\"";
                    if (i != DataHolder.protocolToProgram.Count - 1)
                        associations += ",";
                }
                associations += "]";
                Utilities.setPropertyValue("associations", associations);
            }
            Utilities.saveSettings(mw);

            mw.loadXML();

            Close();
        }

        private void asociations(object sender, EventArgs e)
        {
            generalG.IsEnabled = false;
            generalG.Visibility = Visibility.Collapsed;
            asociationsG.IsEnabled = true;
            asociationsG.Visibility = Visibility.Visible;
            generalB.Background = originalColor[0];
            generalB.BorderBrush = originalColor[1];
            generalB.BorderThickness = oth;
            assocB.Background = activeColor[0];
            assocB.BorderBrush = activeColor[1];
            assocB.BorderThickness = new Thickness(0);
        }

        private void general(object sender, EventArgs e)
        {
            generalG.IsEnabled = true;
            generalG.Visibility = Visibility.Visible;
            asociationsG.IsEnabled = false;
            asociationsG.Visibility = Visibility.Collapsed;
            assocB.Background = originalColor[0];
            assocB.BorderBrush = originalColor[1];
            assocB.BorderThickness = oth;
            generalB.Background = activeColor[0];
            generalB.BorderBrush = activeColor[1];
            generalB.BorderThickness = new Thickness(0);
        }

        private void addAssociation(object sender, EventArgs e)
        {
            addAssG.IsEnabled = true;
            addAssG.Visibility = Visibility.Visible;
            
        }

        private void removeAssociation(object sender, EventArgs e)
        {
            DataHolder.protocolToProgram.Remove((protocolProgramAssociation)assBox.SelectedItem);

            assBox.Items.Refresh();
            assBox.SelectedItem = null;
        }

        private void editAssociation(object sender, EventArgs e)
        {
            protocolProgramAssociation ppa = (protocolProgramAssociation)assBox.SelectedItem;

            if (ppa != null)
            {
                addAssG.IsEnabled = true;
                addAssG.Visibility = Visibility.Visible;

                protocols.SelectedItem = ppa.protocol;
                path.Text = ppa.filePath;
                parameters.Text = ppa.cmdArguments;
            }
        }

        private void closeAddAss(object sender, EventArgs e)
        {
            addAssG.IsEnabled = false;
            addAssG.Visibility = Visibility.Collapsed;
            string selectedProtocol = protocols.SelectedItem.ToString();
            protocolProgramAssociation localppa;
            if (Utilities.doesProtocolHaveAssociation(selectedProtocol))
            {
                localppa = Utilities.getAssociation(selectedProtocol);

                if (localppa.filePath != path.Text || localppa.cmdArguments != parameters.Text)
                {
                    localppa.filePath = path.Text;
                    localppa.cmdArguments = parameters.Text.Trim();

                    forceAssSave = true;
                }
            }
            else
            {
                localppa = new protocolProgramAssociation(selectedProtocol, path.Text);
                localppa.cmdArguments = parameters.Text.Trim();
                DataHolder.protocolToProgram.Add(localppa);
            }
            assBox.Items.Refresh();

            protocols.Text = "";
            protocols.SelectedItem = null;
            path.Text = "";
            parameters.Text = "";
        }

        private void findFile(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "vyberte umístění spouštěcího souboru";
            ofd.Filter = "Spustitelné soubory|*.exe;*.app";

            if (ofd.ShowDialog() == true)
            {
                path.Text = ofd.FileName;
            }
        }


        private void checkIfCanAdd(object sender, EventArgs e)
        {
            if (protocols.SelectedItem != null && protocols.SelectedItem.ToString() == "jiné...")
            {
                otherGrid.IsEnabled = true;
                otherGrid.Visibility = Visibility.Visible;
            }

            if (!File.Exists(path.Text) || (Path.GetExtension(path.Text) != ".exe" && Path.GetExtension(path.Text) != ".app"))
            {
                okButt.IsEnabled = false;
                return;
            }

            if (protocols.SelectedItem == null || string.IsNullOrEmpty(protocols.SelectedItem.ToString()) || string.IsNullOrWhiteSpace(protocols.SelectedItem.ToString()))
            {
                okButt.IsEnabled = false;
                return;
            }

            if (string.IsNullOrEmpty(parameters.Text) || string.IsNullOrWhiteSpace(parameters.Text))
            {
                okButt.IsEnabled = false;
                return;
            }

            if (!parameters.Text.Contains("{ip}") || !parameters.Text.Contains("{port}"))
            {
                okButt.IsEnabled = false;
                return;
            }
            okButt.IsEnabled = true;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                otherGrid.IsEnabled = false;
                otherGrid.Visibility = Visibility.Collapsed;
                List<string> newSource = protocols.ItemsSource.Cast<string>().ToList();
                newSource.Add(other.Text);
                protocols.ItemsSource = newSource;
                protocols.Items.Refresh();
                protocols.SelectedItem = other.Text;
            }
        }

        private void checkRemovability(object sender, EventArgs e)
        {
            if ((protocolProgramAssociation)assBox.SelectedItem != null)
            {
                buttEdit.IsEnabled = true;
                buttRem.IsEnabled = true;
            }
            else
            {
                buttRem.IsEnabled = false;
                buttEdit.IsEnabled = false;
            }
        }
    }
}
