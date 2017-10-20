using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using URLServerManager.Datatypes;

namespace URLServerManager
{
    public partial class MainWindow : Window
    {
        List<server> servers = new List<server>();
        List<protocolAddress> addressesToAdd = new List<protocolAddress>();
        List<string> combocatlist = new List<string>();
        List<string> combolist;

        int total;

        int propInt;
        IPAddress propIP;

        private protocolAddress editpa;
        private server edits;
        private server previousServer;
        private FrameworkElement previousT;
        public MainWindow()
        {
            //načítání konfigu
            loadOrCreateConfig();
            DataHolder.initializeBasicProtocolDictionary();

            combolist = DataHolder.protocolToPort.Keys.ToList();
            combolist.Add("jiné...");

            if (!string.IsNullOrEmpty(DataHolder.localFile))
                loadXML();

            InitializeComponent();
            mainServerWrapper.DataContext = servers;
           
            combocatlist.Add("jiné...");
            pr2add.ItemsSource = combolist;
            cat.ItemsSource = combocatlist;

            if (File.Exists(DataHolder.localFile))
            {
                upSrvButt.IsEnabled = true;
                addSrvButt.IsEnabled = true;
            }
        }

        /**---------------------------------------------------------------------------
        * Start of mostly logical methods *
        ***********************************/

        private void loadOrCreateConfig()
        {
            try
            {
                DataHolder.initializeConfigProperties();
                string directory = Directory.GetCurrentDirectory() + "\\config\\";
                if (Directory.Exists(directory) && File.Exists(directory + "config.cfg"))
                {
                    DataHolder.configFile = directory + "config.cfg";
                    //read file
                    string[] properties = Utilities.splitOnProperty(Utilities.escapeQuotationSpaces(string.Join("", File.ReadAllLines(DataHolder.configFile))));

                    if (properties.Length == 0)
                        return;

                    foreach (string s in properties)
                    {
                        string[] pair = s.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (pair.Length == 2)
                        {
                            foreach (property p in DataHolder.configProperties)
                            {
                                if (pair[0].ToLower() == p.propertyName)
                                {
                                    p.propertyValue = pair[1];
                                }
                            }
                        }
                    }

                    DataHolder.localFile = Utilities.getPropertyValue("localfile");
                    DataHolder.remoteFile = Utilities.getPropertyValue("remotefile");

                    string[] asscs = Utilities.getPropertyValue("associations").Replace("[", "").Replace("]", "").Split('|');

                    foreach (string s in asscs)
                    {
                        string[] tripple = s.Split(new char[] { '-' }, 3, StringSplitOptions.RemoveEmptyEntries);
                        Debug.WriteLine("s: " + s.Split(new char[] { '-' }, 3, StringSplitOptions.RemoveEmptyEntries).Length);

                        if (tripple.Length > 1)
                        {
                            protocolProgramAssociation localAssoc = new protocolProgramAssociation(tripple[0], tripple[1].Replace("\"", ""));
                            if (tripple.Length > 2)
                            {
                                localAssoc.cmdArguments = tripple[2].Replace("\"", "").Replace("'", "\"");
                            }
                            DataHolder.protocolToProgram.Add(localAssoc);
                            Debug.WriteLine(localAssoc.filePath + " " + localAssoc.protocol);
                        }
                    }
                    //DataHolder.protocolToProgram;
                }
                else
                {
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                    if (!File.Exists(directory + "config.cfg"))
                        File.Create(directory + "config.cfg");

                    DataHolder.configFile = directory + "config.cfg";
                }
            }
            catch(Exception e)
            {
                Utilities.log("[ERROR] Reading config", e.ToString());
            }
        }

        public void loadXML()
        {
            try
            {
                if (!File.Exists(DataHolder.localFile))
                    return;
                XElement xml = XElement.Load(DataHolder.localFile);
                IEnumerable<XElement> xservers = xml.Elements("server");
                servers.Clear();
                combocatlist.Clear();

                foreach (XElement s in xservers)
                {
                    server localServer = new server((string)s.Element("FQDN"), (string)s.Element("Cathegory"), (string)s.Element("Description"));
                    if (!combocatlist.Contains(localServer.cathegory))
                        combocatlist.Add(localServer.cathegory);
                    if (s.Attribute("flag") != null) {
                        switch (s.Attribute("flag").Value)
                        {
                            case "rem":
                                continue;
                            case "edit":
                                localServer.localDetect = new SolidColorBrush(Color.FromRgb(233, 255, 45));
                                break;
                            case "add":
                                //Debug.WriteLine("happend");
                                localServer.localDetect = new SolidColorBrush(Color.FromRgb(45, 205, 35));
                                break;
                            default:
                                break;
                        }

                    }

                    IEnumerable<XElement> addresses = s.Element("Addresses").Elements("Address");
                    foreach (XElement a in addresses)
                    {
                        string protocol = (string)a.Attribute("protocol");
                        string ipendpoint = "";
                        string param = "";
                        foreach (XElement x in a.Elements())
                        {
                            switch (x.Name.ToString())
                            {
                                case "IPEndPoint":
                                    ipendpoint = x.Value;
                                    break;
                                case "AdditionalCMDParameters":
                                    param = x.Value;
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (!combolist.Contains(protocol))
                            combolist.Add(protocol);
                        protocolAddress pa = localServer.addIP(protocol, ipendpoint.Split(':')[0], int.Parse(ipendpoint.Split(':')[1]));
                        if (param != null && !string.IsNullOrEmpty(param.Trim()))
                        {
                            pa.setParameters(param);
                        }
                    }

                    servers.Add(localServer);
                }

                servers = servers.OrderBy(x => x.fqdn).ToList();

                if (xml.Attribute("maxServerID") != null)
                    DataHolder.serverMaxId = int.Parse(xml.Attribute("maxServerID").Value);
                else
                    DataHolder.serverMaxId = -1;
            }
            catch (Exception e)
            {
                Utilities.log("[Error] Reading XML",e.ToString());
            }
            if (mainServerWrapper != null)
            {
                mainServerWrapper.DataContext = servers;
                mainServerWrapper.Items.Refresh();
            }
        }

        /**
        *****************
        * tools methods *
        *****************/

        private void addServer(object sender, RoutedEventArgs e)
        {
            string cathegory = "";
            if (!string.IsNullOrEmpty(otherCat.Text.Trim()))
                cathegory = otherCat.Text.Trim();
            else
                cathegory = cat.Text.Trim();

            server localserver = new server(fqdn.Text.Trim(), cathegory, descBox.Text.Trim());
            localserver.protocolAddresses = new List<protocolAddress>(addressesToAdd);
            localserver.localDetect = new SolidColorBrush(Color.FromRgb(45, 205, 35));
            if (!combocatlist.Contains(localserver.cathegory))
                combocatlist.Add(localserver.cathegory);

            XElement serversElem = XElement.Load(DataHolder.localFile);
            List<XElement> addresses = new List<XElement>();
            foreach (protocolAddress p in localserver.protocolAddresses)
            {
                if (!combolist.Contains(p.protocol))
                    combolist.Add(p.protocol);
                XElement x = new XElement("Address");
                x.SetAttributeValue("protocol", p.protocol);
                x.Add(new XElement("IPEndPoint", p.ip+":"+p.port));
                if (p.parameters != null && !string.IsNullOrEmpty(p.parameters.Trim()))
                    x.Add(new XElement("AdditionalCMDParameters", p.parameters));

                addresses.Add(x);
            }
            XElement server = new XElement("server",
                                            new XElement("FQDN", fqdn.Text.Trim()),
                                            new XElement("Cathegory", cat.Text.Trim()),
                                            new XElement("Description", descBox.Text.Trim()),
                                            new XElement("Addresses", addresses)
                                            );
            server.SetAttributeValue("flag", "add");
            string lastids;
            if (serversElem.Elements("server").Count() > 0)
            {
                if (serversElem.Elements("server").Last().Attribute("id") != null)
                    lastids = serversElem.Elements("server").Last().Attribute("id").Value;
                else
                    lastids = "-1";
            }
            else
                lastids = "-1";

            int lastid;

            if (int.TryParse(lastids, out lastid))
            {
                server.SetAttributeValue("id", lastid + 1);
            }
            else
            {
                server.SetAttributeValue("id", 0);
            }

            serversElem.Add(server);
            serversElem.Save(DataHolder.localFile);

            addCancel(sender, e);

            servers.Add(localserver);

            servers = servers.OrderBy(x => x.fqdn).ToList();

            pr2add.ItemsSource = combolist;
            pr2add.Items.Refresh();

            mainServerWrapper.DataContext = servers;
            mainServerWrapper.Items.Refresh();

            onSearchChanged(sender, e);
        }

        private void editServerH(object sender, RoutedEventArgs e)
        {
            if (edits != null)
            {
                string cathegory = "";
                if (cat.Text.Trim() == "jiné...")
                {
                    if (!string.IsNullOrEmpty(otherCat.Text.Trim()) && !string.IsNullOrWhiteSpace(otherCat.Text.Trim()))
                        cathegory = otherCat.Text.Trim();
                }
                else
                    cathegory = cat.Text.Trim();

                server localserver = new server(fqdn.Text.Trim(), cathegory, descBox.Text.Trim());
                localserver.protocolAddresses = new List<protocolAddress>(addressesToAdd);

                XElement xml = XElement.Load(DataHolder.localFile);
                IEnumerable<XElement> xservers = xml.Elements("server");

                if (localserver.cathegory != edits.cathegory)
                {
                    List<server> nsr = new List<server>(servers);
                    nsr.Remove(edits);
                    if (!combocatlist.Contains(localserver.cathegory) && nsr.Select(x => x.cathegory).ToArray().Contains(edits.cathegory))
                    {
                        combocatlist.Add(localserver.cathegory);
                        combocatlist.Remove(edits.cathegory);
                    }
                }

                editXML(localserver, edits, false);

                for (int i = 0; i < servers.Count; i++)
                {
                    if (servers[i] == edits)
                    {

                        if (edits.localDetect != null)
                            localserver.localDetect = edits.localDetect;
                        else
                            localserver.localDetect = new SolidColorBrush(Color.FromRgb(233, 255, 45));

                        servers[i] = localserver;
                        break;
                    }
                }

                edits = null;

                servers = servers.OrderBy(x => x.fqdn).ToList();
                mainServerWrapper.DataContext = servers;
                mainServerWrapper.Items.Refresh();

                onSearchChanged(sender, e);
            }
            
            addServerOK.Click += addServer;
            addServerOK.Click -= editServerH;
            addCancel(sender, e);
            
            /*if (previousServer != null)
                select(previousServer, e);
            else
            {*/
                dltSrvButt.IsEnabled = false;
                edtSrvButt.IsEnabled = false;
            //}
        }

        private void removeServer(object sender, RoutedEventArgs e)
        {
            if (servers.Where(x => x.selected).ToArray().Length > 0)
            {
                server srv = servers.Where(x => x.selected).Cast<server>().First();

                servers.Remove(srv);

                if (combocatlist.Contains(srv.cathegory) && !servers.Select(x => x.cathegory).ToArray().Contains(srv.cathegory))
                    combocatlist.Remove(srv.cathegory);
                
                //change list to hash set and then check if exists or not, not neccessary rn.
                editXML(srv,srv,true);
                servers = servers.OrderBy(x => x.fqdn).ToList();
                mainServerWrapper.DataContext = servers;
                mainServerWrapper.Items.Refresh();

                dltSrvButt.IsEnabled = false;
                edtSrvButt.IsEnabled = false;

                onSearchChanged(sender, e);
            }
            storno(sender, e);
        }



        private void addServerAddress(object sender, RoutedEventArgs e)
        {
            if (editpa == null)
            {
                protocolAddress pa;
                IPAddress ip;
                string ips;

                if (!IPAddress.TryParse(ip2add.Text.Trim(), out ip))
                {
                    ips = ip2add.Text.Trim();
                    if (fqdn.Text == null || string.IsNullOrEmpty(fqdn.Text.Trim()))
                        fqdn.Text = ip2add.Text.Trim();
                }
                else
                {
                    ips = ip.ToString();
                }

                if (DataHolder.protocolToPort.ContainsKey(pr2add.Text.Trim()) && (p2add.Text == null || string.IsNullOrEmpty(p2add.Text.Trim())))
                {
                    addressesToAdd.Add(pa = new protocolAddress(pr2add.Text, ips, DataHolder.protocolToPort[pr2add.Text]));
                }
                else
                {
                    addressesToAdd.Add(pa = new protocolAddress(pr2add.Text, ips, int.Parse(p2add.Text.Trim())));
                }
                if (addpar.Text != null && !string.IsNullOrEmpty(addpar.Text.Trim()))
                    pa.parameters = addpar.Text.Trim();
                ipCancel(sender, e);
                addressListBox.Items.Refresh();
                checkIfCanAdd();
            }
        }

        private void editServerAddress(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("edit");
            if (editpa != null)
            {
                foreach (protocolAddress p in addressesToAdd)
                {
                    if (p.Equals(editpa))
                    {
                        IPAddress ip;
                        string ips;

                        if (!IPAddress.TryParse(ip2add.Text.Trim(), out ip))
                        {
                            ips = ip2add.Text.Trim();
                            if (fqdn.Text == null || string.IsNullOrEmpty(fqdn.Text.Trim()))
                                fqdn.Text = ip2add.Text.Trim();
                        }
                        else
                        {
                            ips = ip.ToString();
                        }
                        p.ip = ips;
                        p.port = int.Parse(p2add.Text.Trim());
                        p.protocol = pr2add.SelectedItem.ToString();
                        if (addpar.Text != null && !string.IsNullOrEmpty(addpar.Text.Trim()))
                            p.parameters = addpar.Text.Trim();
                        break;
                    }
                }
            }
            editpa = null;
            ipCancel(sender, e);
            total++;
            Debug.WriteLine(total);
            addressListBox.Items.Refresh();
            addAOK.Click -= editServerAddress;
            addAOK.Click += addServerAddress;
        }

        private void removeAddress(object sender, RoutedEventArgs e)
        {
            foreach (object o in addressListBox.SelectedItems.Cast<object>())
            {
                addressesToAdd.Remove((protocolAddress)o);
            }
            addressListBox.Items.Refresh();
            addressListBox.SelectedItems.Clear();
        }
        /**
        *********************
        * main menu methods *
        *********************/
        private void newStruct(object sender, EventArgs ea)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Soubory typu XML|*.xml";
                sfd.Title = "Zvolte umístění souboru";
                if (sfd.ShowDialog() == true)
                {
                    if (!Directory.Exists(System.IO.Path.GetDirectoryName(sfd.FileName)))
                    {
                        Directory.CreateDirectory(sfd.FileName);
                    }

                    Utilities.setPropertyValue("localfile", "\"" + sfd.FileName + "\"");
                    Utilities.saveSettings(this);

                    XElement xservers = new XElement("servers");

                    if (File.Exists(DataHolder.remoteFile))
                    {
                        try
                        {
                            XElement sxdoc = XElement.Load(DataHolder.remoteFile);
                            IEnumerable<XElement> serversie = sxdoc.Element("servers").Elements("server");
                            xservers.Add(serversie);

                            upSrvButt.IsEnabled = true;
                            xservers.SetAttributeValue("lastSynchronized", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                            if (serversie.Last().Attribute("id") != null)
                                xservers.SetAttributeValue("maxServerID", serversie.Last().Attribute("id"));
                            else
                                xservers.SetAttributeValue("maxServerID", -1);
                        }
                        catch (Exception e)
                        {
                            Utilities.log("[Error] Reading remote XML", e.ToString());
                        }
                    }
                    else
                    {
                        xservers.SetAttributeValue("maxServerID", -1);
                    }

                    XDocument xdoc = new XDocument(xservers);
                    xdoc.Declaration = new XDeclaration("1.0", "utf-8", null);
                    xdoc.Save(sfd.FileName);

                    loadXML();

                    addSrvButt.IsEnabled = true;
                }
            }
            catch(Exception e)
            {
                Utilities.log("[ERROR] Creating XML file",e.ToString());
            }
        }

        private void syncRemote(object sender, EventArgs ea)
        {
            if (!File.Exists(DataHolder.remoteFile))
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Soubory typu XML|*.xml";
                sfd.Title = "Zvolte umístění vzdáleného souboru";

                if (sfd.ShowDialog() == true)
                {
                    if (!Directory.Exists(System.IO.Path.GetDirectoryName(sfd.FileName)))
                    {
                        Directory.CreateDirectory(sfd.FileName);
                    }

                    Utilities.setPropertyValue("remotefile", "\"" + sfd.FileName + "\"");
                    Utilities.saveSettings(this);

                    XElement xserverstruct = new XElement("URLManagerRemoteStructure");
                    xserverstruct.Add(new XElement("history"));
                    xserverstruct.Add(new XElement("servers"));

                    XDocument xdoc = new XDocument(xserverstruct);
                    xdoc.Declaration = new XDeclaration("1.0", "utf-8", null);
                    xdoc.Save(sfd.FileName);
                }
                else
                {
                    return;
                }
            }
            if (File.Exists(DataHolder.localFile))
            {
                try
                {
                    XElement remoteXML = XElement.Load(DataHolder.remoteFile);
                    XElement localXML = XElement.Load(DataHolder.localFile);

                    IEnumerable<XElement> remoteServers = remoteXML.Element("servers").Elements("server");
                    XElement remoteHistory = remoteXML.Element("history");
                    IEnumerable<XElement> localServers = localXML.Elements("server");

                    List<XElement> remoteTBR = new List<XElement>();
                    List<XElement> remoteTBA = new List<XElement>();

                    DateTime localDate = DateTime.MinValue;
                    if (localXML.Attribute("lastSynchronized") != null)
                        localDate = DateTime.ParseExact(localXML.Attribute("lastSynchronized").Value, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                    //do small rework - instead of element's last change give date of last synchronization

                    int idn = 0;
                    foreach (XElement l in localServers)
                    {
                        XElement newl = new XElement(l);
                        if (remoteServers.Count() > 0)
                        {
                            XAttribute flag = l.Attribute("flag");

                            if (flag != null) //if flag is null, the element already is on the server
                            {
                                if (flag.Value.Equals("add"))
                                {
                                    newl.RemoveAttributes();
                                    newl.SetAttributeValue("id", int.Parse(remoteServers.Last().Attribute("id").Value) + 1);
                                    remoteTBA.Add(newl);
                                    continue;
                                }
                                else
                                {
                                    foreach (XElement s in remoteServers)
                                    {
                                        XAttribute aS = s.Attribute("id");
                                        XAttribute aL = l.Attribute("id");
                                        if (aS != null && aL != null)
                                        {
                                            if (aS.Value == aL.Value)
                                            {
                                                XElement serverChange;
                                                switch (flag.Value)
                                                {
                                                    case "rem":
                                                        if (remoteHistory.Elements("id").Count() > 0 && remoteHistory.Elements("id").Where(x => x.Attribute("num").Value == l.Attribute("id").Value).Count() > 0)
                                                        {
                                                            serverChange = remoteHistory.Elements("id").Where(x => x.Attribute("num").Value == l.Attribute("id").Value).ToArray()[0];
                                                            DateTime historyDeletedServerDate = DateTime.ParseExact(serverChange.Attribute("lastDeleted").Value, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                                            if (DateTime.Compare(localDate, historyDeletedServerDate) <= 0)
                                                            {
                                                                continue;
                                                            }
                                                            else
                                                            {
                                                                serverChange.SetAttributeValue("lastDeleted", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            XElement id = new XElement("id");
                                                            id.SetAttributeValue("lastDeleted", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                                                            id.SetAttributeValue("num", l.Attribute("id").Value);
                                                            remoteHistory.Add(id);
                                                        }

                                                        remoteTBR.Add(s);
                                                        break;
                                                    case "edit":
                                                        if (remoteHistory.Elements("id").Count() > 0 && remoteHistory.Elements("id").Where(x => x.Attribute("num").Value == l.Attribute("id").Value).Count() > 0)
                                                        {
                                                            serverChange = remoteHistory.Elements("id").Where(x => x.Attribute("num").Value == l.Attribute("id").Value).ToArray()[0];
                                                            DateTime historyDeletedServerDate = DateTime.ParseExact(serverChange.Attribute("lastDeleted").Value, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                                            DateTime historyModifiedServerDate = DateTime.ParseExact(serverChange.Attribute("lastModified").Value, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                                            if (DateTime.Compare(localDate, historyModifiedServerDate) <= 0 || DateTime.Compare(localDate, historyDeletedServerDate) <= 0)
                                                            {
                                                                //possible line for asking if add it as new
                                                                continue;
                                                            }
                                                            else
                                                            {
                                                                serverChange.SetAttributeValue("lastModified", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            XElement id = new XElement("id");
                                                            id.SetAttributeValue("lastModified", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                                                            id.SetAttributeValue("num", l.Attribute("id").Value);
                                                            remoteHistory.Add(id);
                                                        }

                                                        newl.RemoveAttributes();
                                                        newl.SetAttributeValue("id", l.Attribute("id").Value);
                                                        s.ReplaceWith(newl);
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            newl.RemoveAttributes();

                            newl.SetAttributeValue("id", idn);

                            remoteXML.Element("servers").Add(newl);
                            idn++;
                        }
                    }

                    //removes removed elements - not affecting loop
                    foreach (XElement s in remoteTBR)
                    {
                        foreach (XElement x in s.NodesAfterSelf())
                        {
                            x.SetAttributeValue("id", int.Parse(x.Attribute("id").Value) - 1);
                        }
                        s.Remove();
                    }

                    foreach (XElement l in remoteTBA)
                    {
                        remoteXML.Element("servers").Add(l);
                    }

                    remoteXML.Save(DataHolder.remoteFile);

                    XDocument newlocal = new XDocument();
                    newlocal.Declaration = new XDeclaration("1.0", "utf-8", null);
                    XElement newLocalServ = new XElement("servers", remoteServers);
                    newLocalServ.SetAttributeValue("lastSynchronized", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                    XElement last = remoteXML.Element("servers").Elements("server").Last();
                    if (last != null && last.Attribute("id") != null)
                        newLocalServ.SetAttributeValue("maxServerID", last.Attribute("id").Value);
                    else
                        newLocalServ.SetAttributeValue("maxServerID", -1);
                    newlocal.Add(newLocalServ);
                    newlocal.Save(DataHolder.localFile);

                    loadXML();
                }
                catch(Exception e)
                {
                    Utilities.log("[Error] Syncing client and remote XML", e.ToString());
                }
            }
        }

        private void open(object sender, RoutedEventArgs ra)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Sourbory Typu XML|*.xml";
            ofd.Title = "Vyberte složku s strukturou serverů ve formátu XML";

            if (ofd.ShowDialog() == true)
            {
                if ((sender as MenuItem).Name == "local")
                {
                    DataHolder.localFile = ofd.FileName;
                    Utilities.setPropertyValue("localfile", "\"" + ofd.FileName + "\"");
                    upSrvButt.IsEnabled = true;
                    addSrvButt.IsEnabled = true;

                    loadXML();
                }
                else
                {
                    DataHolder.remoteFile = ofd.FileName;
                    Utilities.setPropertyValue("remotefile", "\"" + ofd.FileName + "\"");

                }

                Utilities.saveSettings(this);
            }
        }

        /**
        *******************************
        * basic functionality methods *
        *******************************/

        private void select(object sender, EventArgs e)
        {
            dltSrvButt.IsEnabled = true;
            edtSrvButt.IsEnabled = true;
            server ser = (server)(sender as FrameworkElement).DataContext;
            if (ser != previousServer)
            {
                DependencyObject parrent = VisualTreeHelper.GetParent((DependencyObject)sender);
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parrent); i++)
                {
                    FrameworkElement t;
                    if ((t = (FrameworkElement)VisualTreeHelper.GetChild(parrent, i)).Name == "selectBG")
                    {
                        t.Opacity = 40;
                        if (previousT != null)
                            previousT.Opacity = 0;
                        previousT = t;
                        break;
                    }
                }

                ser.selected = true;
                foreach (server s in servers)
                {
                    if (ser != s)
                    {
                        s.selected = false;
                    }
                }

            }
            previousServer = ser;
        }

        private void openConnection(object sender, RoutedEventArgs e)
        {
            protocolAddress pa = (protocolAddress)(sender as FrameworkElement).DataContext;
            if (Utilities.doesProtocolHaveAssociation(pa.protocol))
            {
                protocolProgramAssociation p = Utilities.getAssociation(pa.protocol);
                string args = p.cmdArguments.Replace("{ip}", pa.ip).Replace("{port}", pa.port.ToString());
                try
                {
                    Process.Start(p.filePath, args + " " + pa.parameters);
                }
                catch {}
            }
        }

        /**
         *******************
         * Heleper methods *
         *******************/

        private void editXML(server localserver, server testaginst, bool delete)
        {
            if (testaginst == null)
            {
                testaginst = localserver;
            }
            XElement xml = XElement.Load(DataHolder.localFile);
            IEnumerable<XElement> xservers = xml.Elements("server");

            foreach (XElement s in xservers)
            {
                server testserv = new server((string)s.Element("FQDN"), (string)s.Element("Cathegory"), (string)s.Element("Description")); //server we test aginst
                IEnumerable<XElement> xaddresses = s.Element("Addresses").Elements("Address");
                foreach (XElement a in xaddresses)
                {
                    string protocol = (string)a.Attribute("protocol");
                    string ipendpoint = "";
                    string param = "";
                    foreach (XElement x in a.Elements())
                    {
                        switch (x.Name.ToString())
                        {
                            case "IPEndPoint":
                                ipendpoint = x.Value;
                                break;
                            case "AdditionalCMDParameters":
                                param = x.Value;
                                break;
                            default:
                                break;
                        }

                    }

                    protocolAddress pa = testserv.addIP(protocol, ipendpoint.Split(':')[0], int.Parse(ipendpoint.Split(':')[1]));
                    if (param != null && !string.IsNullOrEmpty(param.Trim()))
                    {
                        pa.setParameters(param);
                    }
                }

                if (testserv.isEqualTo(testaginst))
                {
                    if (!delete)
                    {
                        List<XElement> addresses = new List<XElement>();
                        foreach (protocolAddress p in localserver.protocolAddresses)
                        {
                            XElement x = new XElement("Address");
                            x.SetAttributeValue("protocol", p.protocol);
                            x.Add(new XElement("IPEndPoint", p.ip + ":" + p.port));
                            if (p.parameters != null && !string.IsNullOrEmpty(p.parameters.Trim()))
                                x.Add(new XElement("AdditionalCMDParameters", p.parameters));

                            addresses.Add(x);
                        }
                        s.SetElementValue("FQDN", localserver.fqdn);
                        s.SetElementValue("Cathegory", localserver.cathegory);
                        s.SetElementValue("Description", localserver.desc);
                        //s.SetElementValue("Addresses", addresses);
                        s.Element("Addresses").ReplaceAll(addresses);
                        if(s.Attribute("flag") == null || s.Attribute("flag").Value != "add")
                            s.SetAttributeValue("flag", "edit");

                        break;
                    }
                    else
                    {
                        if (s.Attribute("id") != null)
                        {
                            int id = int.Parse(s.Attribute("id").Value);
                            if (id <= DataHolder.serverMaxId)
                            {
                                s.SetAttributeValue("flag", "rem");
                            }
                            else
                            {
                                foreach (XElement x in s.NodesAfterSelf())
                                {
                                    x.SetAttributeValue("id",int.Parse(x.Attribute("id").Value)-1);
                                }
                                s.Remove();
                            }
                        }
                        else
                        {
                            Utilities.log("[Warning] XML", "chybný element, vymazávám.");
                            s.Remove();
                        }
                        break;
                    }
                }
            }

            xml.Save(DataHolder.localFile);
        }

        /**
        * End of logical methods
        ********************************************************************************************/



        /**---------------------------------------------------------------------------
        * mostly purely graphic functions and event handlers *
        *******************************************************/

        private void openSettings(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings(this,combolist);
            settings.Show();
        }

        private void editServer(object sender, RoutedEventArgs e)
        {
            if (servers.Where(x => x.selected).ToArray().Length > 0)
            {
                newServer(sender, e);

                edits = servers.Where(x => x.selected).Cast<server>().First();

                fqdn.Text = edits.fqdn;
                cat.SelectedItem = edits.cathegory;
                descBox.Text = edits.desc;
                addressesToAdd = new List<protocolAddress>(edits.getAddresses());
                addressListBox.ItemsSource = addressesToAdd;
                addressListBox.Items.Refresh();

                checkIfCanAdd();

                addServerOK.Click -= addServer;
                addServerOK.Click += editServerH;
            }
        }

        private void storno(object sender, RoutedEventArgs e)
        {
            areYouSure.IsEnabled = false;
            areYouSure.Visibility = Visibility.Collapsed;
        }

        private void areYouSureFnc(object sender, RoutedEventArgs e)
        {
            if (servers.Where(x => x.selected).ToArray().Length > 0)
            {
                areYouSure.IsEnabled = true;
                areYouSure.Visibility = Visibility.Visible;
            }
        }

        private void exit(object sender, RoutedEventArgs e)
        {
            Close();
            Application.Current.Shutdown();
        }

        private void newServer(object sender, RoutedEventArgs e)
        {
            addressesToAdd.Clear();
            addressListBox.ItemsSource = addressesToAdd;

            addGrid.IsEnabled = true;
            addGrid.Visibility = Visibility.Visible;
            backshadeadd.Visibility = Visibility.Visible;

            checkIfCanAdd();
        }

        private void openAddAddress(object sender, RoutedEventArgs e)
        {
            ipgrid.IsEnabled = true;
            ipgrid.Visibility = Visibility.Visible;
            backshadeip.Visibility = Visibility.Visible;
            addServerOK.IsEnabled = false;
        }
        private void editAddress(object sender, RoutedEventArgs e)
        {
            if (addressListBox.SelectedItems.Count > 0)
            {
                editpa = (protocolAddress)addressListBox.SelectedItems[0];

                ip2add.Text = editpa.ip;
                p2add.Text = editpa.port.ToString();
                Debug.WriteLine(p2add.Text);
                pr2add.SelectedItem = editpa.protocol;
                addpar.Text = editpa.parameters;

                addAOK.Click -= addServerAddress;
                addAOK.Click += editServerAddress;

                openAddAddress(sender, e);
            }
        }

        private void ipCancel(object sender, RoutedEventArgs e)
        {
            cancelOther();
            ipgrid.IsEnabled = false;
            ipgrid.Visibility = Visibility.Collapsed;
            backshadeip.Visibility = Visibility.Collapsed;
            ip2add.Text = "";
            p2add.Text = "";
            addpar.Text = "";
            checkIfCanAdd();
        }

        private void addCancel(object sender, RoutedEventArgs e)
        {
            ipCancel(sender, e);
            cancelOtherCat();

            addGrid.IsEnabled = false;
            addGrid.Visibility = Visibility.Collapsed;
            backshadeadd.Visibility = Visibility.Collapsed;
            fqdn.Text = "";
            //cat.Text = "";
            descBox.Text = "";
            combocatlist.Clear();
            cat.SelectedItem = null;
            List<string> ctrllist = new List<string>();

            //if nothing was changed cathegories will remain the same as before
            servers.ForEach(x => {
                                 if (!ctrllist.Contains(x.cathegory))
                                     ctrllist.Add(x.cathegory);
                                 });
            ctrllist.Add("jiné...");
            combocatlist.AddRange(ctrllist);
            cat.ItemsSource = combocatlist;
            cat.Items.Refresh();
        }

        private void closeFind(object sender, RoutedEventArgs e)
        {
            findBar.IsEnabled = false;
            findBar.Visibility = Visibility.Collapsed;
            findRect.Visibility = Visibility.Collapsed;

            mainGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
            mainServerWrapper.DataContext = servers;
            mainServerWrapper.Items.Refresh();
        }

        private void openFind(object sender, RoutedEventArgs e)
        {
            mainGrid.RowDefinitions[1].Height = new GridLength(30, GridUnitType.Star);

            findBar.IsEnabled = true;
            findBar.Visibility = Visibility.Visible;
            findRect.Visibility = Visibility.Visible;
        }

        private void onSearchChanged(object sender, EventArgs ev)
        {
            if (where.IsEnabled && where.Visibility == Visibility.Visible)
            {
                if (where.SelectedIndex == 0)
                {
                    if (exactMatch.IsChecked == false)
                        mainServerWrapper.DataContext = servers.Where(x => x.fqdn.Contains(subject.Text.Trim())).ToList();
                    else
                        mainServerWrapper.DataContext = servers.Where(x => x.fqdn == subject.Text.Trim()).ToList();

                }
                else if (where.SelectedIndex == 1)
                {
                    if (exactMatch.IsChecked == false)
                        mainServerWrapper.DataContext = servers.Where(x => x.cathegory.Contains(subject.Text.Trim())).ToList();
                    else
                        mainServerWrapper.DataContext = servers.Where(x => x.cathegory == subject.Text.Trim()).ToList();
                }

                mainServerWrapper.Items.Refresh();
            }
        }

        private void onCatChanged(object s, EventArgs e)
        {
            if (cat.SelectedItem == null)
                return;
            if (cat.SelectedItem.ToString() == "jiné...")
            {
                otherGridCat.IsEnabled = true;
                otherGridCat.Visibility = Visibility.Visible;
                addServerOK.IsEnabled = false;
            }
        }

        private void onAddressFieldChanged(object sender, EventArgs e)
        {
            if (pr2add.SelectedItem == null)
            {
                addAOK.IsEnabled = false;
                return;
            }

            if (pr2add.SelectedItem.ToString() == "jiné...")
            {
                otherGrid.IsEnabled = true;
                otherGrid.Visibility = Visibility.Visible;
                backshadeother.Visibility = Visibility.Visible;
                addAOK.IsEnabled = false;
                return;
            }

            if (string.IsNullOrEmpty(pr2add.SelectedItem.ToString().Trim()))
            {
                addAOK.IsEnabled = false;
                return;
            }

            bool portMBS = true;
            if (DataHolder.protocolToPort.ContainsKey(pr2add.SelectedItem.ToString().Trim()))
            {
                portMBS = false;
            }

            if (portMBS)
            {
                if (p2add.Text == null || string.IsNullOrEmpty(p2add.Text.Trim()) || !int.TryParse(p2add.Text.Trim(), out propInt))
                {
                    addAOK.IsEnabled = false;
                    return;
                }
            }


            try
            {
                if (ip2add.Text == null || string.IsNullOrEmpty(ip2add.Text.Trim()) || !(IPAddress.TryParse(ip2add.Text.Trim(), out propIP) || Utilities.resolveHostname(ip2add.Text.Trim(),60)))
                {
                    addAOK.IsEnabled = false;
                    return;
                }
            }
            catch
            {
                addAOK.IsEnabled = false;
                return;
            }

            addAOK.IsEnabled = true;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!combolist.Contains(other.Text.Trim()))
                    combolist.Add(other.Text.Trim());
            
                pr2add.ItemsSource = combolist;
                pr2add.Items.Refresh();
                pr2add.SelectedItem = other.Text.Trim();
                cancelOther();
            }
            else if (e.Key == Key.Escape)
            {
                cancelOther();
            }
        }

        private void TextBox_KeyDown1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!combocatlist.Contains(otherCat.Text.Trim()))
                {
                    combocatlist.Add(otherCat.Text.Trim());
                    cat.Items.Refresh();
                }
                cat.SelectedItem = otherCat.Text.Trim();
                cancelOtherCat();
            }
            else if (e.Key == Key.Escape)
            {
                cancelOtherCat();
            }
        }

        public void cancelOtherCat()
        {
            otherGridCat.IsEnabled = false;
            otherGridCat.Visibility = Visibility.Collapsed;
            otherCat.Text = "";
            checkIfCanAdd();
        }

        public void cancelOther()
        {
            otherGrid.IsEnabled = false;
            otherGrid.Visibility = Visibility.Collapsed;
            backshadeother.Visibility = Visibility.Collapsed;
            if (other.Text.Trim() != null && other.Text.Trim() != "jiné...")
            {
                pr2add.Text = other.Text.Trim();
                pr2add.SelectedItem = other.Text.Trim();
            }
            else
            {
                pr2add.Text = "";
                pr2add.SelectedItem = "";

            }
            other.Text = "";
            onAddressFieldChanged(null, null);
        }

        private void addressListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (addressListBox.SelectedItems.Count <= 0)
            {
                removeAddressButt.IsEnabled = false;
                editAddressButt.IsEnabled = false;
            }
            else
            {
                removeAddressButt.IsEnabled = true;
                editAddressButt.IsEnabled = true;
            }
        }

        private void checkIfCanAdd(object sender, DependencyPropertyChangedEventArgs e)
        {
            checkIfCanAdd();
        }

        private void checkIfCanAdd()
        {
            if (addressListBox.Items.Count > 0)
                addServerOK.IsEnabled = true;
            else
                addServerOK.IsEnabled = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.F))
            {
                openFind(sender, e);
            }
        }
    }
    /**
    * End of graphical methods
    ********************************************************************************************/
}
