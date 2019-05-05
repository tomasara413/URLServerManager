using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
using URLServerManagerModern.Data.DataTypes.Pseudo;
using URLServerManagerModern.Windows.Utility;

namespace URLServerManagerModern.Windows.Settings
{
    public partial class CategoryColorAssociationWindow : Window//, ISubSettingsWindow
    {
        CategoryColorAssociation cca;
        private Settings settings;
        EventHandler closedEvent;
        public CategoryColorAssociationWindow(string additionalTitle, Settings settings)
        {
            InitializeComponent();
            this.settings = settings;

            if (settings == null)
                throw new ArgumentNullException("Settings property cannot be null");

            if (!string.IsNullOrEmpty(additionalTitle) && !string.IsNullOrWhiteSpace(additionalTitle))
                Title += " - " + additionalTitle;

            PendingCancelation = false;
            
            settings.Closed += closedEvent = (o, e) => Close();

            HashSet<string> validCategories = new HashSet<string>();
            using (SQLiteConnection c = Utilities.Utilities.EstablishSQLiteDatabaseConnection(Utilities.Utilities.GetPropertyValue("locallocation")))
            {
                if (c != null)
                {
                    SQLiteCommand cmd = new SQLiteCommand(c);

                    cmd.CommandText = "SELECT DISTINCT Category FROM servers";
                    c.Open();
                    SQLiteDataReader reader = cmd.ExecuteReader();


                    if (reader.HasRows)
                    {
                        string cat;
                        while (reader.Read())
                        {
                            cat = (string)reader["Category"];
                            if (!string.IsNullOrEmpty(cat?.Trim()))
                                validCategories.Add(cat);
                        }
                    }

                    cmd.Dispose();
                    cmd = new SQLiteCommand(c);

                    cmd.CommandText = "SELECT Category FROM defaultCategories";
                    reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                            validCategories.Remove((string)reader["Category"]);
                    }
                }
            }

            foreach (string cat in validCategories)
                Categories.Items.Insert(Categories.Items.Count - 1, cat);
        }

        public CategoryColorAssociationWindow(string additionalTitle, Settings settings, CategoryColorAssociation association) : this(additionalTitle, settings)
        {
            if (association != null)
            {
                cca = association;

                Categories.Items.Add(cca.category);
                Categories.SelectedItem = cca.category;
                Categories.IsEnabled = false;

                BorderColor.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(cca.borderColor);
                BackgroundColor.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(cca.fillColor);
                TextColor.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(cca.textColor);
            }
        }

        public bool PendingCancelation { get; set; }
        private void SaveChanges(object sender, RoutedEventArgs e)
        {
            if (cca != null)
            {
                cca.category = Categories.SelectedItem.ToString();
                cca.borderColor = ((SolidColorBrush)BorderColor.Background).Color.ToString();
                cca.fillColor = ((SolidColorBrush)BackgroundColor.Background).Color.ToString();
                cca.textColor = ((SolidColorBrush)TextColor.Background).Color.ToString();
            }
            else
            {
                cca = new CategoryColorAssociation(Categories.SelectedItem.ToString(), ((SolidColorBrush)BackgroundColor.Background).Color.ToString(), ((SolidColorBrush)BorderColor.Background).Color.ToString(), ((SolidColorBrush)TextColor.Background).Color.ToString());

                (settings.DefaultColorCategoryAssignment.ItemsSource as List<CategoryColorAssociation>).Add(cca);
            }

            settings.DefaultColorCategoryAssignment.Items.Refresh();
            Close();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
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

        public void AddOtherCategory(string category)
        {
            if ((settings.DefaultColorCategoryAssignment.ItemsSource as List<CategoryColorAssociation>).Count(x => x.category == category) != 0)
            {
                MessageBox.Show(this, Properties.Resources.CategoryColorDefined, Properties.Resources.Error, MessageBoxButton.OK);
                return;
            }

            if (!Categories.Items.Contains(category))
                Categories.Items.Insert(Categories.Items.Count - 1, category);
                
            Categories.SelectedItem = category;
        }

        Dictionary<object, ColorPickerWindow> cpwhs = new Dictionary<object, ColorPickerWindow>();
        private void OpenColorPicker(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow cpw;
            if (!cpwhs.TryGetValue(sender, out cpw))
            {
                cpwhs.Add(sender, cpw = new ColorPickerWindow((sender as Button).Background as SolidColorBrush));
                //TODO: Apply to subsections of settings which had to have all these mecahnics handled on their own
                cpw.Closed += (s, ea) => {
                    if(!isClosing)
                        cpwhs.Remove(sender);
                };
                cpw.Show();
            }
            else
                cpw.Activate();
        }

        private bool isClosing = false;
        private void OnWindowClosed(object sender, EventArgs e)
        {
            isClosing = true;
            foreach (KeyValuePair<object, ColorPickerWindow> cpw in cpwhs)
                cpw.Value.Close();

            /*if (!PendingCancelation)
                settings.GetAssociationWindows().Remove(this);*/

            if (otherOpen)
                ibp.Close();

            settings.Closed -= closedEvent;

            settings.CheckAllSettings();
        }

        private void CheckValidSelection(object sender, SelectionChangedEventArgs e)
        {
            Ok.IsEnabled = Categories.SelectedIndex > -1;
        }
    }
}
