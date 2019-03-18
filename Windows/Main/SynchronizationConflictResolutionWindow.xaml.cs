using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using URLServerManagerModern.Data.DataTypes.Pseudo;
using static URLServerManagerModern.Utilities.Utilities;

namespace URLServerManagerModern.Windows.Main
{
    public partial class SynchronizationConflictResolutionWindow : Window
    {
        string user, password, database, server;

        int port = 3306;

        private void Swap(object sender, RoutedEventArgs e)
        {
            ConflictedPair cp = (ConflictedPair)(sender as FrameworkElement).DataContext;
            PseudoEntity m = cp.en1;
            cp.en1 = cp.en2;
            cp.en2 = m;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Resolve(object sender, RoutedEventArgs e)
        {
            List<ConflictedPair> pairs = (List<ConflictedPair>) wrapper.ItemsSource;
            ConflictedPair cp;
            for (int i = 0; i < pairs.Count; i++)
            {
                cp = pairs[i];

                SavePseudoEntityDirtyAsync(cp.en1).ConfigureAwait(false);
                UpdateRemotePseudoEntityAsync(user, password, database, server, port, cp.en1).ConfigureAwait(false);
            }
            Close();
        }

        SynchronizationScale scale;
        public SynchronizationConflictResolutionWindow(string user, string password, string database, string server, int port = 3306, SynchronizationScale scale = SynchronizationScale.Full)
        {
            this.user = user;
            this.password = password;
            this.database = database;
            this.server = server;
            this.port = port;
            this.scale = scale;
            InitializeComponent();
            BeginSynchronization();
        }

        private async void BeginSynchronization()
        {
            Dictionary<PseudoEntity, PseudoEntity> conflicts = await SynchronizeWithServerAsync(user, password, database, server, port, scale);

            if (conflicts.Count == 0)
            {
                MessageBox.Show(Properties.Resources.SynchronizationSuccessful, Properties.Resources.SynchronizationSuccess, MessageBoxButton.OK);
                Close();
                return;
            }

            List<ConflictedPair> conflictedPairs = new List<ConflictedPair>();
            foreach (KeyValuePair<PseudoEntity, PseudoEntity> pair in conflicts)
                conflictedPairs.Add(new ConflictedPair { en1 = pair.Key, en2 = pair.Value });

            pleaseWait.Visibility = Visibility.Collapsed;

            wrapper.ItemsSource = conflictedPairs;

            resolve.IsEnabled = true;
        }
    }

    public class ConflictedPair : INotifyPropertyChanged
    {
        private PseudoEntity _en1, _en2;
        public PseudoEntity en1 { get { return _en1; } set { _en1 = value; OnPropertyChanged("en1"); } }
        public PseudoEntity en2 { get { return _en2; } set { _en2 = value; OnPropertyChanged("en2"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
