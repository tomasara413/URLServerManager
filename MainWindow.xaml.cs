using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;

namespace URLServerManagerModern
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            PseudoServer s = new PseudoServer(ModificationDetector.Null, new Server("test.ex.dex", "", "testovací serveros"));
            List<PseudoServer> servers = new List<PseudoServer>();
            servers.Add(s);
            mainServerWrapper.DataContext = servers;
            mainServerWrapper.Items.Refresh();
        }

        private void FormKeyListener(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                switch (e.Key)
                {
                    case Key.S:
                        //sync/save as
                        break;
                    case Key.N:
                        //new server structure
                        break;
                }
            }
        }

        private void OpenConnection(object sender, RoutedEventArgs e)
        {
            ProtocolAddress pa = (ProtocolAddress)(sender as FrameworkElement).DataContext;

            if (Utilities.Utilities.DoesProtocolHaveAssociation(pa.protocol))
            {

                ProtocolProgramAssociation p = Utilities.Utilities.GetAssociation(pa.protocol);

                string args = p.cmdArguments.Replace("{ip}", pa.ip).Replace("{port}", pa.port.ToString());

                try
                {

                    Process.Start(p.filePath, args + " " + pa.parameters);

                }
                catch (Exception ex)
                {
                    Utilities.Utilities.Log("[ERROR] Opening System Process", ex.ToString());
                }

            }
        }

        private void SelectServer(object sender, MouseButtonEventArgs e)
        {

        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
