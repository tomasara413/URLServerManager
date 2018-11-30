using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
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
using URLServerManagerModern.Data;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Utilities;

namespace URLServerManagerModern.Windows.Settings
{
    public partial class ProgramAssociation : Window
    {
        private Program p;
        private Window parentWindow;

        EventHandler closedEvent;
        public ProgramAssociation(string additionalTitle, Window parentWindow)
        {
            InitializeComponent();

            parentWindow.Closed += closedEvent = (o, e) => Close();

            if (!string.IsNullOrEmpty(additionalTitle) && !string.IsNullOrWhiteSpace(additionalTitle))
                Title += " - " + additionalTitle;

            this.parentWindow = parentWindow;

            Associations.ItemsSource = new List<ProtocolArgumentAssociation>();

            CheckInputs();
        }

        public ProgramAssociation(string additionalTitle, Settings settings, Program p) : this(additionalTitle, settings)
        {
            if (p != null)
            {
                this.p = p;

                ExePath.Text = p.FilePath;
                IconImage.Source = p.icon;
                Associations.ItemsSource = p.associations.DeepCopy();
            }

            CheckInputs();
        }

        public void AddProtocolAssociation(ProtocolArgumentAssociation paa)
        {
            (Associations.ItemsSource as List<ProtocolArgumentAssociation>).Add(paa);
            CheckInputs();
            
            Associations.Items.Refresh();
        }

        public delegate void AssociationSaved(Program p);
        public event AssociationSaved OnAssociationSaved;
        private void SaveOrAddAssociation(object sender, RoutedEventArgs e)
        {
            if (p == null)
                p = new Program(ExePath.Text.Trim());

            p.FilePath = ExePath.Text.Trim();
            p.associations = Associations.ItemsSource as List<ProtocolArgumentAssociation>;

            OnAssociationSaved?.Invoke(p);

            Close();
        }

        private void CheckInputs()
        {
            bool atLeastOneError = string.IsNullOrEmpty(ExePath.Text) || string.IsNullOrWhiteSpace(ExePath.Text) || !File.Exists(ExePath.Text) || (System.IO.Path.GetExtension(ExePath.Text) != ".exe" && System.IO.Path.GetExtension(ExePath.Text) != ".bat" && System.IO.Path.GetExtension(ExePath.Text) != ".app" && System.IO.Path.GetExtension(ExePath.Text) != ".jar");

            List<ProtocolArgumentAssociation> paal = Associations.ItemsSource as List<ProtocolArgumentAssociation>;
            bool duplicatesFound = false;
            for (int i = 0; i < paal.Count; i++)
            {
                for (int j = i + 1; j < paal.Count; j++)
                {
                    if (duplicatesFound = (paal[i].protocol == paal[j].protocol))
                    {
                        AssociationsErrors.ToolTip = Properties.Resources.DuplicateProtocolEntries;
                        AssociationsErrors.Visibility = Visibility.Visible;
                        goto breakMeOut;
                    }
                }
            }
            breakMeOut:;
            if(!duplicatesFound)
                AssociationsErrors.Visibility = Visibility.Collapsed;

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

        //public bool PendingCancelation { get; set; }
        private void OnWindowClosed(object sender, EventArgs e)
        {
            parentWindow.Closed -= closedEvent;

            //parentWindow.CheckAllSettings();
        }

        private void OpenProgramDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Properties.Resources.ExecutableFiles + "|*.exe;*.jar;*.app";
            ofd.Title = Properties.Resources.ExecutableFilePath;

            if (File.Exists(ExePath.Text) || Directory.Exists(ExePath.Text))
                ofd.InitialDirectory = System.IO.Path.GetDirectoryName(ExePath.Text);

            if (ofd.ShowDialog() == true)
            {
                ExePath.Text = ofd.FileName;
                IconImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(System.Drawing.Icon.ExtractAssociatedIcon(ofd.FileName).ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }

        private void AddOrModifyAssociation(object sender, RoutedEventArgs e)
        {
            ProtocolAssociation pa;
            if ((sender as Button).Name == "Edit")
            {
                pa = new ProtocolAssociation(Properties.Resources.Edit, this, Associations.SelectedItem as ProtocolArgumentAssociation);
                pa.OnAssociationSaved += (a) => {
                    CheckInputs();
                    Associations.Items.Refresh();
                };
            }
            else
            {
                pa = new ProtocolAssociation(Properties.Resources.Add, this);
                pa.OnAssociationSaved += AddProtocolAssociation;
            }

            pa.ShowActivated = true;
            pa.Show();
        }

        private void RemoveAssociation(object sender, RoutedEventArgs e)
        {
            if (Associations.SelectedItem != null)
            {
                (Associations.ItemsSource as List<ProtocolArgumentAssociation>).Remove(Associations.SelectedItem as ProtocolArgumentAssociation);
                Associations.Items.Refresh();
            }
        }

        private void OnAssociationsSelctionChanged(object sender, SelectionChangedEventArgs e)
        {
            Remove.IsEnabled = Edit.IsEnabled = (sender as ListBox).SelectedItem != null;
        }
    }
}
