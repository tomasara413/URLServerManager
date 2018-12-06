using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
using URLServerManagerModern.Data;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;
using URLServerManagerModern.Utilities;

namespace URLServerManagerModern.Windows.Settings
{
    public partial class Settings : Window
    {
        private Brush activeBG;
        private Brush passiveBG, passiveBorder;
        private List<ProtocolPortAssociation> pna = new List<ProtocolPortAssociation>();
        public Settings()
        {
            InitializeComponent();

            passiveBG = protocolAssocB.Background;
            passiveBorder = protocolAssocB.BorderBrush;
            activeBG = generalB.Background;

            localFileInput.Text = Utilities.Utilities.GetPropertyValue("localfile");
            remoteFileInput.Text = Utilities.Utilities.GetPropertyValue("remotefile");

            //Debug.WriteLine(DataHolder.protocolToPort.Count);
            foreach (KeyValuePair<string, int> pair in DataHolder.protocolToPort)
                pna.Add(new ProtocolPortAssociation(pair.Key, pair.Value));

            string property = Utilities.Utilities.GetPropertyValue("fontsize");
            double parsedDouble;

            if (property != null && double.TryParse(property, out parsedDouble))
                fontSize.Text = property.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            else
                fontSize.Text = DataHolder.fontSize.ToString();

            property = Utilities.Utilities.GetPropertyValue("showmodificationindicators");
            bool parsedBool;

            if (property != null && bool.TryParse(property, out parsedBool))
                showLocalModifiers.IsChecked = parsedBool;
            else
                showLocalModifiers.IsChecked = false;

            property = Utilities.Utilities.GetPropertyValue("allowportavailabilitydiagnostics");

            if (property != null && bool.TryParse(property, out parsedBool))
            {
                allowPortDiagnostics.IsChecked = parsedBool;
                diagnosticsTimeout.IsEnabled = parsedBool;
                diagnosticsRenew.IsEnabled = parsedBool;
            }
            else
            {
                allowPortDiagnostics.IsChecked = true;
                diagnosticsTimeout.IsEnabled = true;
                diagnosticsRenew.IsEnabled = true;
            }

            property = Utilities.Utilities.GetPropertyValue("portavailabilitytimeout");
            int parsedInt;

            if (property != null && int.TryParse(property, out parsedInt))
                diagnosticsTimeout.Text = parsedInt.ToString();
            else
                diagnosticsTimeout.Text = "30000";

            property = Utilities.Utilities.GetPropertyValue("portavailabilityrenew");

            if (property != null && int.TryParse(property, out parsedInt))
                diagnosticsRenew.Text = parsedInt.ToString();
            else
                diagnosticsRenew.Text = "200000";

            ProgramBox.ItemsSource = DataHolder.programs.DeepCopy();
            PortAssociationsBox.ItemsSource = pna;

            //Greys out the predefined protocols
            PortAssociationsBox.ItemContainerGenerator.StatusChanged += OnListBoxChange;

            /*List<CategoryColorAssociation> cca = new List<CategoryColorAssociation>();
            cca.Add(new CategoryColorAssociation("test", "#FFFFAA00", "#FFADFF15"));
            cca.Add(new CategoryColorAssociation("test1", null, null));
            DefaultColorCategoryAssignment.ItemsSource = cca;*/

            LoadAvailableLanguages();

            LoadCategoryColors();

            CheckAllSettings();
        }

        List<CultureInfo> availableLanguages = new List<CultureInfo>();
        private void LoadAvailableLanguages()
        {
            string[] directories = Directory.GetDirectories(Directory.GetCurrentDirectory()).Select(x => x.Split('\\').Last()).ToArray();
            Debug.WriteLine("Dirs: " + directories.Length);
            for (int i = 0; i < directories.Length; i++)
            {
                try
                {
                    CultureInfo ci = CultureInfo.GetCultureInfo(directories[i]);
                    if (!ci.Equals(CultureInfo.InvariantCulture))
                    {
                        availableLanguages.Add(ci);
                        AvailableLanguages.Items.Add(ci.DisplayName);
                    }
                }
                catch (Exception e) { }
            }

            if(Properties.Resources.Culture != null)
                AvailableLanguages.SelectedItem = Properties.Resources.Culture.DisplayName;
            else
                AvailableLanguages.SelectedItem = CultureInfo.CurrentUICulture.DisplayName;
            AvailableLanguages.Items.Refresh();
        }

        private void LoadCategoryColors()
        {
            string localFile = Utilities.Utilities.GetPropertyValue("localfile");
            categoryAssocB.IsEnabled = !string.IsNullOrEmpty(localFile) && !string.IsNullOrWhiteSpace(localFile) && File.Exists(localFile);
            if (categoryAssocB.IsEnabled)
            {
                if(DataHolder.categoryColors.Count == 0)
                    Utilities.Utilities.LoadCategoryColors();

                DefaultColorCategoryAssignment.ItemsSource = DataHolder.categoryColors.GetDeepCopiedList();
            }
        }

        private void OnListBoxChange(object sender, EventArgs e)
        {
            if (sender is ItemContainerGenerator)
            {
                if (sender == PortAssociationsBox.ItemContainerGenerator && PortAssociationsBox.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    for (int i = 0; i < DataHolder.InitialProtocolToPortPoolSize && i < PortAssociationsBox.ItemContainerGenerator.Items.Count; i++)
                    {
                        DependencyObject o = PortAssociationsBox.ItemContainerGenerator.ContainerFromIndex(i);
                        if (o != null && o is ListBoxItem)
                            (o as ListBoxItem).IsEnabled = false;
                    }
                }
            }
        }

        private void OnListBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox)
            {
                if (sender == PortAssociationsBox)
                {
                    if (e.AddedItems != null && e.AddedItems.Count > 0)
                    {
                        RemovePortAssociationButton.IsEnabled = true;
                        EditPortAssociationButton.IsEnabled = true;
                    }
                    else
                    {
                        RemovePortAssociationButton.IsEnabled = false;
                        EditPortAssociationButton.IsEnabled = false;
                    }
                }
                else if (sender == ProgramBox)
                {
                    if (e.AddedItems != null && e.AddedItems.Count > 0)
                    {
                        RemoveAssociationButton.IsEnabled = true;
                        EditAssociationButton.IsEnabled = true;
                    }
                    else
                    {
                        RemoveAssociationButton.IsEnabled = false;
                        EditAssociationButton.IsEnabled = false;
                    }
                }

            }
        }

        private void OpenFileDialog(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                FrameworkElement fe = (FrameworkElement)sender;

                string fileName = Utilities.Utilities.OpenServerStructure(false);

                if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrWhiteSpace(fileName))
                {
                    if (fe.Name == "addLocal")
                    {
                        localFileInput.Text = fileName;
                    }
                    else if (fe.Name == "addRemote")
                    {
                        remoteFileInput.Text = fileName;
                    }
                }
            }
        }

        public void SwitchSettingsTab(int newTab)
        {
            switch (newTab)
            {
                default:
                    OpenGeneral(null, null);
                    SwitchSettingCategory(generalB, null);
                    break;
                case 1:
                    OpenProtocolAssoc(null, null);
                    SwitchSettingCategory(protocolAssocB, null);
                    break;
                case 2:
                    OpenCategoryAssoc(null, null);
                    SwitchSettingCategory(categoryAssocB, null);
                    break;
            }
        }

        private void OpenGeneral(object sender, RoutedEventArgs e)
        {
            GeneralGrid.Visibility = Visibility.Visible;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(SettingsWindowsWrapper); i++)
            {
                DependencyObject o = VisualTreeHelper.GetChild(SettingsWindowsWrapper, i);
                if (o != GeneralGrid)
                {
                    if (o is Grid)
                        (o as Grid).Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OpenProtocolAssoc(object sender, RoutedEventArgs e)
        {
            ProtocolAssociationGrid.Visibility = Visibility.Visible;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(SettingsWindowsWrapper); i++)
            {
                DependencyObject o = VisualTreeHelper.GetChild(SettingsWindowsWrapper, i);
                if (o != ProtocolAssociationGrid)
                {
                    Grid g = o as Grid;
                    if (g != null)
                        g.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OpenCategoryAssoc(object sender, RoutedEventArgs e)
        {
            CategoryAssociationGrid.Visibility = Visibility.Visible;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(SettingsWindowsWrapper); i++)
            {
                DependencyObject o = VisualTreeHelper.GetChild(SettingsWindowsWrapper, i);
                if (o != CategoryAssociationGrid)
                {
                    Grid g = o as Grid;
                    if (g != null)
                        g.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SwitchSettingCategory(object sender, RoutedEventArgs e)
        {
            if (!(sender is DependencyObject))
                return;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(SettingsSwitch); i++)
            {
                DependencyObject o = VisualTreeHelper.GetChild(SettingsSwitch, i);
                if (o is Button)
                {
                    if (o == sender)
                    {
                        (o as Button).Background = activeBG;
                        (o as Button).BorderBrush = activeBG;
                        categoryTitle.Text = ((o as Button).Content as TextBlock).Text;
                    }
                    else
                    {
                        (o as Button).Background = passiveBG;
                        (o as Button).BorderBrush = passiveBorder;
                    }
                }
            }
        }

        private void CheckFileExistence(object sender, TextChangedEventArgs e)
        {
            CheckAllSettings();
        }

        private void CancelSettings(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(localFileInput.Text.Trim()) && !string.IsNullOrWhiteSpace(localFileInput.Text.Trim())) {
                if (File.Exists(localFileInput.Text.Trim()))
                    Utilities.Utilities.SetPropertyValue("localfile", "\"" + localFileInput.Text + "\"");
            }
            else
                Utilities.Utilities.SetPropertyValue("localfile", null);


            if (!string.IsNullOrEmpty(remoteFileInput.Text.Trim()) && !string.IsNullOrWhiteSpace(remoteFileInput.Text.Trim())) {
                if(File.Exists(remoteFileInput.Text.Trim()))
                    Utilities.Utilities.SetPropertyValue("remotefile", "\"" + remoteFileInput.Text + "\"");
            }
            else
                Utilities.Utilities.SetPropertyValue("remotefile", null);

            if (!string.IsNullOrEmpty(fontSize.Text) && !string.IsNullOrWhiteSpace(fontSize.Text))
            {
                double fontValue;
                if (double.TryParse(fontSize.Text, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture.NumberFormat, out fontValue))
                {
                    if (fontValue > 35)
                    {
                        Utilities.Utilities.SetPropertyValue("fontsize", "35");
                    }
                    else if (fontValue < 3)
                    {
                        Utilities.Utilities.SetPropertyValue("fontsize", "3");
                    }
                    else
                    {
                        Utilities.Utilities.SetPropertyValue("fontsize", fontSize.Text.Replace(',', '.'));
                    }
                }
            }

            Utilities.Utilities.SetPropertyValue("showmodificationindicators", showLocalModifiers.IsChecked.ToString());
            Utilities.Utilities.SetPropertyValue("allowportavailabilitydiagnostics", allowPortDiagnostics.IsChecked.ToString());

            if (allowPortDiagnostics.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(diagnosticsTimeout.Text) && !string.IsNullOrWhiteSpace(diagnosticsTimeout.Text))
                {
                    int timeoutValue;
                    if (int.TryParse(diagnosticsTimeout.Text, out timeoutValue))
                    {
                        if (timeoutValue < 1)
                            Utilities.Utilities.SetPropertyValue("portavailabilitytimeout", "1");
                        else
                            Utilities.Utilities.SetPropertyValue("portavailabilitytimeout", timeoutValue.ToString());
                    }
                }

                if (!string.IsNullOrEmpty(diagnosticsRenew.Text) && !string.IsNullOrWhiteSpace(diagnosticsRenew.Text))
                {
                    int renewValue;
                    if (int.TryParse(diagnosticsRenew.Text, out renewValue))
                    {
                        if (renewValue < 1)
                            Utilities.Utilities.SetPropertyValue("portavailabilityrenew", "1");
                        else
                            Utilities.Utilities.SetPropertyValue("portavailabilityrenew", renewValue.ToString());
                    }
                }
            }

            if (newCultureInfo != null)
            {
                Properties.Resources.Culture = newCultureInfo;
                Utilities.Utilities.SetPropertyValue("language", newCultureInfo.Name);
            }

            if (ProgramBox.Items.Count > 0)
            {
                StringBuilder associations = new StringBuilder().Append("{");
                List<string> protocols = new List<string>();
                for (int i = 0; i < ProgramBox.Items.Count; i++)
                {
                    Program p = ProgramBox.Items[i] as Program;
                    associations.Append("\"");
                    associations.Append(p.FilePath);
                    associations.Append("\"-[");
                    
                    
                    for (int j = 0; j < p.associations.Count; j++)
                    {
                        ProtocolArgumentAssociation paa = p.associations[j];
                        if (protocols.Contains(paa.protocol))
                        {
                            p.associations.RemoveAt(j);
                            j--;
                            continue;
                        }
                        protocols.Add(paa.protocol);
                        associations.Append(paa.protocol);
                        associations.Append("-\"");
                        associations.Append(paa.cmdArguments.Replace("\"", "'"));
                        associations.Append("\"");
                        if (j < p.associations.Count - 1)
                            associations.Append(",");
                    }
                    associations.Append("]");
                    if (i < ProgramBox.Items.Count - 1)
                        associations.Append(",");

                    protocols.Clear();
                }
                associations.Append("}");
                //Debug.WriteLine(associations.ToString());
                Utilities.Utilities.SetPropertyValue("associations", associations.ToString());
            }
            else
                Utilities.Utilities.SetPropertyValue("associations", "");

            //Debug.WriteLine(PortAssociationsBox.Items.Count);
            if (PortAssociationsBox.Items.Count > DataHolder.InitialProtocolToPortPoolSize)
            {
                StringBuilder portassociations = new StringBuilder().Append("[");
                for (int i = DataHolder.InitialProtocolToPortPoolSize; i < PortAssociationsBox.Items.Count; i++)
                {
                    ProtocolPortAssociation ppa = (PortAssociationsBox.Items[i] as ProtocolPortAssociation);
                    portassociations.Append(ppa.protocol).Append("-").Append(ppa.port);
                    if (i < DataHolder.programs.Count - 1)
                        portassociations.Append(",");

                    if (DataHolder.protocolToPort.ContainsKey(ppa.protocol))
                        DataHolder.protocolToPort[ppa.protocol] = ppa.port;
                    else
                        DataHolder.protocolToPort.Add(ppa.protocol, ppa.port);
                }
                portassociations.Append("]");
                Utilities.Utilities.SetPropertyValue("portassociations", portassociations.ToString());
            }
            else
                Utilities.Utilities.SetPropertyValue("portassociations", "");

            Utilities.Utilities.SaveSettings();
            //We need a deep copy in case we tamper with data inside the list while being saved
            Utilities.Utilities.SaveDefaultCategoriesAsync(DefaultColorCategoryAssignment.ItemsSource as List<CategoryColorAssociation>).ConfigureAwait(false);

            LoadCategoryColors();

            DataHolder.programs = ProgramBox.ItemsSource as List<Program>;
            DataHolder.protocolToPort = (PortAssociationsBox.ItemsSource as List<ProtocolPortAssociation>).ToDictionary(x => x.protocol, y => y.port);

            //Utilities.Utilities.RefreshAllDynamicResources();
        }

        private void SaveAndCloseSettings(object sender, RoutedEventArgs e)
        {
            SaveSettings(sender, e);
            Close();
        }

        private void AllowOnlyNumbers(object sender, TextCompositionEventArgs e)
        {
            double d;
            e.Handled = !double.TryParse((sender as TextBox).Text + e.Text, out d);
        }

        private void AllowOnlyInteger(object sender, TextCompositionEventArgs e)
        {
            int i;
            e.Handled = !int.TryParse((sender as TextBox).Text + e.Text, out i);
        }

        private void DoNotAllowPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                double d;
                if (!double.TryParse((string)e.DataObject.GetData(typeof(string)), out d))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private void TestFontSizeBounds(object sender, TextChangedEventArgs e)
        {
            TextBox t = sender as TextBox;
            if (t.Text.Length > 5)
                t.Text = t.Text.Substring(0, (sender as TextBox).Text.Length - 1);

            CheckAllSettings();
        }

        private void TestAllSettings(object sender, EventArgs e)
        {
            CheckAllSettings();
        }

        private void OnCheckboxChange(object sender, RoutedEventArgs e)
        {
            if (allowPortDiagnostics.IsChecked != null)
            {
                diagnosticsRenew.IsEnabled = (bool)allowPortDiagnostics.IsChecked;
                diagnosticsTimeout.IsEnabled = (bool)allowPortDiagnostics.IsChecked;
            }
            CheckAllSettings();
        }

        private void AssociationsMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == null || e == null || e.OriginalSource == null)
                return;

            ListBox lb = sender as ListBox;
            DependencyObject o = lb.ContainerFromElement(e.OriginalSource as DependencyObject);

            if (o != null && (o as ListBoxItem).IsEnabled)
            {
                lb.SelectedItem = o;
                lb.SelectedIndex = lb.Items.IndexOf(e.OriginalSource);
                (o as ListBoxItem).IsSelected = true;
            }
        }

        private void AddOrModifyProgramAssociation(object sender, RoutedEventArgs e)
        {
            ProgramAssociation p;
            if ((sender as Button).Name == "EditAssociationButton")
            {
                p = new ProgramAssociation(Properties.Resources.Edit, this, ProgramBox.SelectedItem as Program);
                p.OnAssociationSaved += (pr) => ProgramBox.Items.Refresh();
            }
            else
            {
                p = new ProgramAssociation(Properties.Resources.Add, this);
                p.OnAssociationSaved += AddProgramAssociation;
            }
            //associationWindows.Add(p);
            
            p.ShowActivated = true;
            p.Show();

            CheckAllSettings();
        }

        private void AddProgramAssociation(Program p)
        {
            (ProgramBox.ItemsSource as List<Program>).Add(p);
            ProgramBox.Items.Refresh();
        }

        private void RemoveProgramAssociation(object sender, RoutedEventArgs e)
        {
            if (ProgramBox.SelectedItem != null)
            {
                //Debug.WriteLine(ProtocolAssociationsBox.SelectedItem);
                (ProgramBox.ItemsSource as List<Program>).Remove(ProgramBox.SelectedItem as Program);
                ProgramBox.Items.Refresh();
            }
        }

        private void AddOrModifyPortAssociation(object sender, RoutedEventArgs e)
        {
            PortAssociation p;
            if ((sender as Button).Name == "EditPortAssociationButton")
                p = new PortAssociation(Properties.Resources.Edit, this, PortAssociationsBox.SelectedItem as ProtocolPortAssociation);
            else
                p = new PortAssociation(Properties.Resources.Add, this);

            p.ShowActivated = true;
            p.Show();

            CheckAllSettings();
        }

        private void RemovePortAssociation(object sender, RoutedEventArgs e)
        {
            if (PortAssociationsBox.SelectedItem != null)
            {
                (PortAssociationsBox.ItemsSource as List<ProtocolPortAssociation>).Remove(PortAssociationsBox.SelectedItem as ProtocolPortAssociation);
                PortAssociationsBox.Items.Refresh();
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void AddOrModifyDefaultCategoryColor(object sender, RoutedEventArgs e)
        {
            CategoryColorAssociationWindow ccaw;
            if ((sender as Button).Name == "addDCC")
                ccaw = new CategoryColorAssociationWindow(Properties.Resources.Add, this);
            else
                ccaw = new CategoryColorAssociationWindow(Properties.Resources.Edit, this, DefaultColorCategoryAssignment.SelectedItem as CategoryColorAssociation);

            //associationWindows.Add(ccaw);
            ccaw.ShowActivated = true;
            ccaw.Show();

            CheckAllSettings();
        }

        private void RemoveDefaultCategoryColor(object sender, RoutedEventArgs e)
        {
            if (DefaultColorCategoryAssignment.SelectedIndex >= 0)
            {
                for (int i = 0; i < DefaultColorCategoryAssignment.SelectedItems.Count; i++)
                {
                    (DefaultColorCategoryAssignment.ItemsSource as List<CategoryColorAssociation>).Remove(DefaultColorCategoryAssignment.SelectedItems[i] as CategoryColorAssociation);
                    DefaultColorCategoryAssignment.Items.Refresh();
                }
            }
        }

        CultureInfo newCultureInfo = null;
        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            newCultureInfo = availableLanguages.Where(x => AvailableLanguages.SelectedItem.ToString() == x.DisplayName).First();
        }

        internal void CheckAllSettings()
        {
            bool atLeastOneError = false;
            localFileErrors.Visibility = Visibility.Collapsed;
            if (string.IsNullOrEmpty(localFileInput.Text.Trim()) || !File.Exists(localFileInput.Text.Trim()))
            {
                localFileErrors.Visibility = Visibility.Visible;
                localFileErrors.ToolTip = Properties.Resources.ResourceManager.GetString("FileDoesNotExist");
                atLeastOneError = !string.IsNullOrEmpty(localFileInput.Text.Trim());
            }
               

            remoteFileErrors.Visibility = Visibility.Collapsed;
            if (string.IsNullOrEmpty(remoteFileInput.Text.Trim()) || !File.Exists(remoteFileInput.Text.Trim()))
            {
                remoteFileErrors.Visibility = Visibility.Visible;
                remoteFileErrors.ToolTip = Properties.Resources.ResourceManager.GetString("FileDoesNotExist");
                atLeastOneError = !string.IsNullOrEmpty(remoteFileInput.Text.Trim());
            }

            if (string.IsNullOrEmpty(fontSize.Text.Trim()))
            {
                fontSizeErrors.Visibility = Visibility.Visible;
                fontSizeErrors.ToolTip = Properties.Resources.ResourceManager.GetString("FontSizeNull");
                atLeastOneError = true;
            }
            else
            {
                double fontValue;
                if (double.TryParse(fontSize.Text, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture.NumberFormat, out fontValue))
                {
                    fontSizeErrors.Visibility = Visibility.Visible;
                    if (fontValue > 35)
                        fontSizeErrors.ToolTip = Properties.Resources.ResourceManager.GetString("FontSizeTooLarge");
                    else if (fontValue < 3)        
                        fontSizeErrors.ToolTip = Properties.Resources.ResourceManager.GetString("FontSizeTooSmall");
                    else
                        fontSizeErrors.Visibility = Visibility.Collapsed;
                }
            }

            availabilityCheckErrors.Visibility = Visibility.Collapsed;
            if (allowPortDiagnostics.IsChecked == true)
            {
                availabilityCheckErrors.ToolTip = "";

                if (string.IsNullOrEmpty(diagnosticsRenew.Text.Trim()))
                {
                    availabilityCheckErrors.Visibility = Visibility.Visible;
                    availabilityCheckErrors.ToolTip = Properties.Resources.ResourceManager.GetString("RenewNull");

                    atLeastOneError = true;
                }
                else
                {
                    int renewValue;
                    if (int.TryParse(diagnosticsRenew.Text, out renewValue))
                    {
                        if (renewValue < 1)
                        {
                            availabilityCheckErrors.Visibility = Visibility.Visible;
                            availabilityCheckErrors.ToolTip = Properties.Resources.ResourceManager.GetString("RenewTooSmall");
                        } 
                    }
                }

                if (string.IsNullOrEmpty(diagnosticsTimeout.Text.Trim()))
                {
                    availabilityCheckErrors.Visibility = Visibility.Visible;
                    availabilityCheckErrors.ToolTip += Environment.NewLine + Properties.Resources.ResourceManager.GetString("TimeoutNull");
                    atLeastOneError = true;
                }
                else
                {
                    int timeoutValue;
                    if (int.TryParse(diagnosticsTimeout.Text, out timeoutValue))
                    {
                        if (timeoutValue < 1)
                        {
                            availabilityCheckErrors.Visibility = Visibility.Visible;
                            availabilityCheckErrors.ToolTip += Environment.NewLine + Properties.Resources.ResourceManager.GetString("TimeoutTooSmall");
                        }
                    }
                }

                availabilityCheckErrors.ToolTip = (availabilityCheckErrors.ToolTip as string).Trim();
            }

            //atLeastOneError = associationWindows.Count > 0;
                //Debug.WriteLine("over 0");
           
            apply.IsEnabled = ok.IsEnabled = !atLeastOneError;
        }
    }
}
