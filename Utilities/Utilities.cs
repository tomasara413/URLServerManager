using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Data.SQLite;

using URLServerManagerModern.Data;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;
using System.Resources;
using System.Diagnostics;
using System.Globalization;
using URLServerManagerModern.Windows.Main;
using System.Security.Permissions;
using System.ComponentModel;
using System.Security;
using URLServerManagerModern.Utilities.IO;

namespace URLServerManagerModern.Utilities
{
    public static class Utilities
    {
        private static MainWindow _mainWindow;
        public static MainWindow mainWindow
        {
            get
            {
                return _mainWindow;
            }
            set
            {
                if (_mainWindow == null)
                    _mainWindow = value;
            }
        }

        public static void LoadOrCreateConfig()
        {
            try
            {
                DataHolder.InitializeConfigProperties();
                DataHolder.InitializeBasicProtocolDictionary();

                string directory = Directory.GetCurrentDirectory() + "\\config\\";
                if (Directory.Exists(directory) && File.Exists(directory + "config.cfg"))
                {
                    DataHolder.configFile = directory + "config.cfg";
                    //read file
                    string[] properties = SplitOnProperty(EscapeQuotationSpaces(string.Join("", File.ReadAllLines(DataHolder.configFile))));

                    if (properties.Length == 0)
                        return;

                    foreach (string s in properties)
                    {
                        string[] pair = s.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (pair.Length == 2)
                        {
                            if (DataHolder.configProperties.ContainsKey(pair[0]))
                                DataHolder.configProperties[pair[0]] = pair[1];
                        }
                    }

                    string assoc = GetPropertyValue("associations");
                    string[] multiProperty;
                    //Debug.WriteLine(assoc.Substring(1, assoc.Length - 2));
                    if (!string.IsNullOrEmpty(assoc))
                    {
                        multiProperty = SplitOnProperty(assoc.Substring(1, assoc.Length - 2));

                        foreach (string s in multiProperty)
                        {
                            string[] couple = s.Split(new char[] { '-' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            //Debug.WriteLine("s: " + s.Split(new char[] { '-' }, 3, StringSplitOptions.RemoveEmptyEntries).Length);

                            if (couple.Length > 0)
                            {
                                Program p = new Program(couple[0].Trim('"'));

                                if (couple.Length > 1)
                                {
                                    assoc = couple[1];
                                    string[] protocolsAndArguments = SplitOnProperty(assoc.Substring(1, assoc.Length - 2));
                                    for (int i = 0; i < protocolsAndArguments.Length; i++)
                                    {
                                        couple = protocolsAndArguments[i].Split(new char[] { '-' }, 2, StringSplitOptions.RemoveEmptyEntries);
                                        if (couple.Length == 2)
                                        {
                                            p.associations.Add(new ProtocolArgumentAssociation(couple[0], couple[1].Trim('"')));
                                            //Debug.WriteLine("[LOADING] Associating program \""+ p.FilePath + "\" with protocol " + couple[0] + " under command args: " + couple[1]);
                                        }
                                    }
                                }
                                DataHolder.programs.Add(p);

                            }
                        }
                    }

                    assoc = GetPropertyValue("portassociations");
                    if (!string.IsNullOrEmpty(assoc))
                    {
                        multiProperty = assoc.Replace("[", "").Replace("]", "").Split('|');

                        foreach (string s in multiProperty)
                        {
                            string[] duo = s.Split(new char[] { '-' }, 2, StringSplitOptions.RemoveEmptyEntries);

                            if (duo.Length > 1 && !DataHolder.protocolToPort.ContainsKey(duo[0]))
                                DataHolder.protocolToPort.Add(duo[0], int.Parse(duo[1]));
                        }
                    }

                    string lang = GetPropertyValue("language");
                    if (!string.IsNullOrEmpty(lang))
                        Properties.Resources.Culture = CultureInfo.GetCultureInfo(lang);
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
            catch (Exception e)
            {
                //throw;
                Log("[ERROR] Reading config", e.ToString());
            }
        }

        public static string EscapeQuotationSpaces(string original)
        {
            StringBuilder returnString = new StringBuilder();

            bool those = false;

            foreach (char c in original)
            {
                if (c.Equals('"'))
                    those = !those;

                if (those)
                    returnString.Append(c);
                else
                {
                    if (!char.IsWhiteSpace(c))
                    {
                        if (c.Equals(','))
                            returnString.Append('|'); //this is not valid sign for dir name
                        else
                            returnString.Append(c);
                    }
                }
            }
            return returnString.ToString();
        }

        public static string[] SplitOnProperty(string original)
        {
            List<string> returnString = new List<string>();
            List<int> delimiters = new List<int>();

            int those = 0;

            for (int i = 0; i < original.Length; i++)
            {
                if (original[i].Equals('(') || original[i].Equals('[') || original[i].Equals('{'))
                    those++;
                else if (original[i].Equals(')') || original[i].Equals(']') || original[i].Equals('}'))
                    those--;

                if (those == 0)
                {
                    if (original[i].Equals('|'))
                        delimiters.Add(i);
                    else if (i == original.Length - 1)
                        delimiters.Add(i + 1);
                }
            }

            for (int i = 0; i < delimiters.Count - 1; i++)
            {
                if (i == 0)
                    returnString.Add(original.Substring(i, delimiters[i]));
                returnString.Add(original.Substring(delimiters[i] + 1, (delimiters[i + 1] - delimiters[i]) - 1));
            }

            if (delimiters.Count <= 1)
                returnString.Add(original);

            return returnString.ToArray();
        }

        public static string GetPropertyValue(string propertyName)
        {
            string ret;
            DataHolder.configProperties.TryGetValue(propertyName, out ret);
            if (ret != null)
                ret = ret.Trim('"');
            return ret;
        }

        public static void SetPropertyValue(string propertyName, string propertyValue)
        {
            if (DataHolder.configProperties.ContainsKey(propertyName))
                DataHolder.configProperties[propertyName] = propertyValue;
        }

        public static bool DoesProtocolHaveAssociation(string protocol)
        {
            foreach (Program p in DataHolder.programs)
            {
                for (int i = 0; i < p.associations.Count; i++)
                {
                    if (p.associations[i].protocol == protocol)
                        return true;
                }
            }
            return false;
        }

        public static Program GetAssociation(string protocol)
        {
            foreach (Program p in DataHolder.programs)
            {
                for (int i = 0; i < p.associations.Count; i++)
                {
                    if (p.associations[i].protocol == protocol)
                        return p;
                }
            }
            return null;
        }

        public static void Log(string errortype, string e)
        {
            string directory = Directory.GetCurrentDirectory() + "\\logs\\";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            StreamWriter sw = new StreamWriter(directory + "\\log-" + DateTime.Today.ToString("dd.MM.yyyy") + ".log", true, Encoding.UTF8);
            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "::" + errortype + ": " + e);
            sw.Close();
            sw.Dispose();
        }

        public static void SaveSettings()
        {
            //load config and look after these properties
            try
            {
                List<string> properties = new List<string>(SplitOnProperty(EscapeQuotationSpaces(string.Join("", File.ReadAllLines(DataHolder.configFile)))));
                int count = properties.Count;
                HashSet<string> generated = new HashSet<string>();
                foreach (KeyValuePair<string, string> p in DataHolder.configProperties)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string[] pair = properties[i].Split(new char[] { '=' }, 2);
                        if (pair[0] == p.Key)
                        {
                            pair[1] = p.Value;
                            //Debug.WriteLine(pair[0] + "=" + pair[1]);
                            generated.Add(p.Key);
                            properties[i] = pair[0] + "=" + pair[1];
                        }
                    }
                    if (!generated.Contains(p.Key))
                        properties.Add(p.Key + "=" + p.Value);
                }

                using (StreamWriter writer = new StreamWriter(DataHolder.configFile, false, Encoding.UTF8))
                    writer.Write(string.Join("," + Environment.NewLine, properties));
                //LoadXML(20,0);
            }
            catch (Exception e)
            {
                throw;
                Log("[ERROR] saving config", e.ToString());
            }
        }

        public static string OpenServerStructure(bool import)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = (!import ? Properties.Resources.ResourceManager.GetString("SQLiteDBFile") + "|*.db|" : "") + Properties.Resources.ResourceManager.GetString("XMLStructureFile") + "|*.xml|" + Properties.Resources.JSONStructureFile + "|*.json|" + Properties.Resources.CSVStructureFile + "|*.csv";
            ofd.Title = Properties.Resources.ResourceManager.GetString("OFDSelectFile");

            if (ofd.ShowDialog() == true)
                return ofd.FileName;

            return null;
        }

        public static void NewServerStructure()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = Properties.Resources.ResourceManager.GetString("SQLiteDBFile") + "|*.db";
            sfd.Title = Properties.Resources.ResourceManager.GetString("SFDFileSavingProcedure");

            if (sfd.ShowDialog() == true)
            {
                SetPropertyValue("localfile", "\"" + sfd.FileName + "\"");
                CreateDatabase(sfd.FileName);
                SaveSettings();
            }
        }

        private static XMLImporter xmli = new XMLImporter();
        private static JSONImporter jsoni = new JSONImporter();
        private static CSVImporter csvi = new CSVImporter();

        private static void OnImportClosed(object s, EventArgs e)
        {
            cid.Closed -= OnImportClosed;
            iscCidClosed = true;
        }

        static CSVImportDialog cid;
        static bool iscCidClosed = true;
        public static void Import(string filePath)
        {
            switch (Path.GetExtension(filePath)?.ToLower())
            {
                case ".xml":
                    xmli.Import(filePath);
                    break;
                case ".json":
                    jsoni.Import(filePath);
                    break;
                case ".csv":
                    if (iscCidClosed)
                    {
                        cid = new CSVImportDialog(csvi, queue, filePath);
                        cid.Closed += OnImportClosed;
                        cid.ShowActivated = true;
                        cid.Show();
                        iscCidClosed = false;
                    }
                    else
                        cid.Activate();
                    break;
            }
        }


        public static async void ImportAsync(string filePath)
        {
            Task t = null;
            switch (Path.GetExtension(filePath)?.ToLower())
            {
                case ".xml":
                    t = queue.Enqueue(() => Task.Run(() => xmli.Import(filePath)));
                    break;
                case ".json":
                    t = queue.Enqueue(() => Task.Run(() => jsoni.Import(filePath)));
                    break;
                case ".csv":
                    if (iscCidClosed)
                    {
                        cid = new CSVImportDialog(csvi, queue, filePath);
                        cid.Closed += OnImportClosed;
                        cid.ShowActivated = true;
                        cid.Show();
                        iscCidClosed = false;
                    }
                    else
                        cid.Activate();
                    break;
            }

            if (t != null)
                await t;


        }


        private static XMLExporter xmle = new XMLExporter();
        private static JSONExporter jsone = new JSONExporter();
        private static CSVExporter csve = new CSVExporter();
        public static void Export()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = Properties.Resources.XMLStructureFile + "|*.xml|" + Properties.Resources.JSONStructureFile + "|*.json|" + Properties.Resources.CSVStructureFile + "|*.csv";
            sfd.Title = Properties.Resources.ResourceManager.GetString("SFDFileSavingProcedure");

            if (sfd.ShowDialog() == true)
            {
                switch (Path.GetExtension(sfd.FileName)?.ToLower())
                {
                    case ".xml":
                        xmle.Export(sfd.FileName);
                        break;
                    case ".json":
                        jsone.Export(sfd.FileName);
                        break;
                    case ".csv":
                        csve.Export(sfd.FileName);
                        break;
                }
            }
        }

        public static async void ExportAsync()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = Properties.Resources.XMLStructureFile + "|*.xml|" + Properties.Resources.JSONStructureFile + "|*.json|" + Properties.Resources.CSVStructureFile + "|*.csv";
            sfd.Title = Properties.Resources.ResourceManager.GetString("SFDFileSavingProcedure");

            if (sfd.ShowDialog() == true)
            {
                Task t = null;
                switch (Path.GetExtension(sfd.FileName)?.ToLower())
                {
                    case ".xml":
                        t = queue.Enqueue(() => Task.Run(() => xmle.Export(sfd.FileName)));
                        break;
                    case ".json":
                        t = queue.Enqueue(() => Task.Run(() => jsone.Export(sfd.FileName)));
                        break;
                    case ".csv":
                        t = queue.Enqueue(() => Task.Run(() => csve.Export(sfd.FileName)));
                        break;
                }

                if (t != null)
                    await t;
            }
        }


        public static void ConvertToDB(string filePath)
        {
            CreateDatabase(GetPropertyValue("localfile"));
            ImportAsync(filePath);
        }

        private static TaskQueue queue = new TaskQueue();

        private static void CreateDatabase(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    File.Delete(filePath);
                }
                SQLiteConnection.CreateFile(filePath);

                using (SQLiteConnection c = EstablishDatabaseConnection(filePath))
                {
                    string table = "CREATE TABLE IF NOT EXISTS servers (rowid INTEGER PRIMARY KEY, Type INTEGER DEFAULT 0, FQDN TEXT, Category TEXT, Desc TEXT); CREATE TABLE IF NOT EXISTS addresses (Protocol TEXT, Address TEXT NOT NULL, Port INTEGER NOT NULL, AdditionalCMDParameters TEXT, ServerID INTEGER NOT NULL, FOREIGN KEY (ServerID) REFERENCES servers(rowid) ON DELETE CASCADE); CREATE TABLE IF NOT EXISTS pseudoServers (ModificationDetector INTEGER, CustomBackgroundColor TEXT, CustomBorderColor TEXT, CustomTextColor TEXT, ServerID INTEGER NOT NULL, FOREIGN KEY (ServerID) REFERENCES servers(rowid) ON DELETE CASCADE); CREATE TABLE IF NOT EXISTS serverContents (ServerID INTEGER NOT NULL, ParentServerID INTEGER NOT NULL, PRIMARY KEY(ParentServerID, ServerID), FOREIGN KEY (ServerID) REFERENCES servers(rowid) ON DELETE CASCADE, FOREIGN KEY (ParentServerID) REFERENCES servers(rowid) ON DELETE CASCADE); CREATE TABLE IF NOT EXISTS defaultCategories (Category TEXT NOT NULL UNIQUE, CustomBackgroundColor TEXT, CustomBorderColor TEXT, CustomTextColor TEXT, PRIMARY KEY(Category)) WITHOUT ROWID;";
                    c.Open();
                    SQLiteCommand cmd = new SQLiteCommand(table, c);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (UnauthorizedAccessException e) {
                ShowElevationDialog();
            }
        }

        public static void ShowElevationDialog()
        {
            if (MessageBox.Show(Properties.Resources.ElevateProcess, Properties.Resources.ElevateProcessCaption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (ElevateProcess(Process.GetCurrentProcess()) != null)
                    Application.Current.Shutdown();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public static Process ElevateProcess(Process source)
        {
            Process target = new Process();
            target.StartInfo = source.StartInfo;
            target.StartInfo.FileName = source.MainModule.FileName;
            target.StartInfo.WorkingDirectory = Path.GetDirectoryName(source.MainModule.FileName);

            //Required for UAC to work
            target.StartInfo.UseShellExecute = true;
            target.StartInfo.Verb = "runas";

            try
            {
                if (!target.Start())
                    return null;
            }
            catch (Win32Exception e)
            {
                //Cancelled
                if (e.NativeErrorCode == 1223)
                    return null;
            }
            return target;
        }

        public static SQLiteConnection EstablishDatabaseConnection(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            return new SQLiteConnection("Data Source=" + filePath + ";Version=3;foreign keys=true;");
        }

        public static List<PseudoEntity> LoadServers(long offset, long limit)
        {
            List<PseudoEntity> servers = new List<PseudoEntity>();
            using (SQLiteConnection c = EstablishDatabaseConnection(GetPropertyValue("localfile")))
            {
                
                if (c != null)
                {
                    SQLiteCommand cmd = new SQLiteCommand(c);

                    cmd.CommandText = "SELECT * FROM servers LEFT JOIN pseudoServers ON rowid = pseudoServers.ServerID ORDER BY FQDN ASC LIMIT " + limit + " OFFSET " + offset;
                    c.Open();
                    SQLiteDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        
                        while (reader.Read())
                            servers.Add(ReadPseudoEntity(c, reader));
                    }

                    reader.Close();
                }
            }
            return servers;
        }

        public static async Task<List<PseudoEntity>> LoadServersAsync(long offset, long limit = DataHolder.LIMIT)
        {
            return await Task.Run(() => LoadServers(offset, limit));
        }

        private static PseudoEntity ReadPseudoEntity(SQLiteConnection c, SQLiteDataReader reader)
        {
            EntityType type = EntityType.Server;
            PseudoEntity s;

            type = (EntityType) Enum.Parse(typeof(EntityType),((long)reader["Type"]).ToString());

            switch (type)
            {
                default:
                    s = new PseudoServer(ModificationDetector.Null, new Server());
                    break;
                case EntityType.VirtualizationServer:
                case EntityType.Cluster:
                    s = new PseudoWrappingEntity(ModificationDetector.Null, new Server(), (PseudoEntity[])null);
                    break;
            }
            s.type = type;

            s.server.rowID = (long)reader["rowid"];

            s.server.fqdn = new SecurityElement("unescape",(string)reader["FQDN"]).Text;
            s.server.category = new SecurityElement("unescape",(string)reader["Category"]).Text;
            s.server.desc = new SecurityElement("unescape",(string)reader["Desc"]).Text;

            string color = DBReaderColumnValueToString(reader["CustomBackgroundColor"]);
            bool hasDefaultCategory = DataHolder.categoryColors.ContainsKey(s.server.category);
            s.usesFill = color != null && !string.IsNullOrEmpty(color) && !string.IsNullOrWhiteSpace(color);
            s.customBackgroundColor = s.usesFill ? color : hasDefaultCategory ? DataHolder.categoryColors[s.server.category].fillColor : "#00FFFFFF";

            color = DBReaderColumnValueToString(reader["CustomBorderColor"]);
            s.usesBorder = color != null && !string.IsNullOrEmpty(color) && !string.IsNullOrWhiteSpace(color);
            s.customBorderColor = s.usesBorder ? color : hasDefaultCategory ? DataHolder.categoryColors[s.server.category].borderColor : "#00FFFFFF";

            color = DBReaderColumnValueToString(reader["CustomTextColor"]);
            s.usesText = color != null && !string.IsNullOrEmpty(color) && !string.IsNullOrWhiteSpace(color);
            s.customTextColor = s.usesText ? color : hasDefaultCategory ? DataHolder.categoryColors[s.server.category].textColor : "#000000";

            SQLiteCommand cmd2 = new SQLiteCommand("SELECT Protocol, Address, Port, AdditionalCMDParameters, rowid FROM addresses WHERE ServerID = " + s.server.rowID, c);
            SQLiteDataReader r2 = cmd2.ExecuteReader();

            

            while (r2.Read())
            {
                ProtocolAddress pa = s.server.AddIP(new SecurityElement("unescape",(string)r2["Protocol"]).Text, new SecurityElement("unescape",(string)r2["Address"]).Text, (int)((long)r2["Port"]));
                pa.parameters = new SecurityElement("unescape",(string)r2["AdditionalCMDParameters"]).Text;
                pa.rowID = (long)r2["rowid"];
            }
            r2.Close();


            if ((int)type > 1)
            {
                //This could cause a catastrophe if there is one huge nested entity
                cmd2 = new SQLiteCommand("SELECT * FROM serverContents INNER JOIN servers ON serverContents.ServerID = servers.rowid INNER JOIN pseudoServers ON serverContents.ServerID = pseudoServers.ServerID WHERE serverContents.ParentServerID = " + s.server.rowID, c);
                r2 = cmd2.ExecuteReader();

                while (r2.Read())
                    ((PseudoWrappingEntity)s).computersInCluster.Add(ReadPseudoEntity(c, r2));
                r2.Close();
            }

            return s;
        }

        public static void LoadCategoryColors()
        {
            using (SQLiteConnection c = EstablishDatabaseConnection(GetPropertyValue("localfile")))
            {
                if (c != null)
                {
                    DataHolder.categoryColors.Clear();
                    SQLiteCommand cmd = new SQLiteCommand(c);

                    cmd.CommandText = "SELECT * FROM defaultCategories";
                    c.Open();
                    SQLiteDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        string ca;
                        while (reader.Read())
                            DataHolder.categoryColors.Add(ca = new SecurityElement("unescape",(string)reader["Category"]).Text, new CategoryColorAssociation(ca, (string)reader["CustomBackgroundColor"], (string)reader["CustomBorderColor"], (string)reader["CustomTextColor"]));
                    }

                    reader.Close();
                }
            }
        }

        public static async void LoadCategoryColorsAsync()
        {
            await Task.Run(() => LoadCategoryColors());
        }

        public static void RefreshAllDynamicResources()
        {
            mainWindow.Resources["fontSize"] = DataHolder.fontSize;
        }

        public static void SaveDefaultCategories(List<CategoryColorAssociation> currentList)
        {
            List<string> toBeRemoved = DataHolder.categoryColors.Where(x => !currentList.Select(y => y.category).Contains(x.Key)).Select(x => x.Key).ToList();

            if(currentList != null)
                DataHolder.categoryColors = currentList.ToDictionary(x => x.category, y => new CategoryColorAssociation(y.category, y.fillColor, y.borderColor, y.textColor));
            using (SQLiteConnection c = EstablishDatabaseConnection(GetPropertyValue("localfile")))
            {
                if (c != null)
                {
                    SQLiteCommand cmd = new SQLiteCommand(c);
                    StringBuilder b = new StringBuilder();
                    b.Append("INSERT OR REPLACE INTO defaultCategories (Category, CustomBackgroundColor, CustomBorderColor, CustomTextColor) VALUES ");
                    int i = 0;
                    foreach (KeyValuePair<string, CategoryColorAssociation> pair in DataHolder.categoryColors)
                    {
                        b.Append("('").Append(SecurityElement.Escape(pair.Key)).Append("', '").Append(pair.Value.fillColor).Append("', '").Append(pair.Value.borderColor).Append("', '").Append(pair.Value.textColor).Append("')");
                        if (i < DataHolder.categoryColors.Count - 1)
                            b.Append(",");
                        else
                            b.Append(";");
                        i++;
                    }
                    for (i = 0; i < toBeRemoved.Count; i++)
                        b.Append("DELETE FROM defaultCategories WHERE Category='").Append(SecurityElement.Escape(toBeRemoved[i])).Append("';");

                    cmd.CommandText = b.ToString();
                    c.Open();
                    if (DataHolder.categoryColors.Count > 0)
                        cmd.ExecuteNonQuery();
                }
            }
        }

        public static async void SaveDefaultCategoriesAsync(List<CategoryColorAssociation> currentList)
        {
            await queue.Enqueue(() => Task.Run(() => SaveDefaultCategories(currentList.DeepCopy())));
        }

        //TODO: Sweep down the code where there could be invalid custom colors when the user is too 'intelligent'
        public static void SavePseudoEntity(PseudoEntity e, List<ProtocolAddress> removed, List<PseudoEntity> removedEntities)
        {
            //throw new NotImplementedException("Saving to the database is not implemented yet");
            using (SQLiteConnection c = EstablishDatabaseConnection(GetPropertyValue("localfile")))
            {
                if (c != null)
                {
                    SQLiteCommand cmd = new SQLiteCommand(c);
                    StringBuilder b = new StringBuilder();
                    SQLiteDataReader reader;
                    c.Open();
                    if (e.server.rowID >= 0)
                    {
                        b.Append("UPDATE servers SET FQDN = '").Append(SecurityElement.Escape(e.server.fqdn)).Append("', Category = '").Append(SecurityElement.Escape(e.server.category)).Append("', Desc = '").Append(SecurityElement.Escape(e.server.desc)).Append("', Type = ").Append((int)e.type).Append(" WHERE rowid = ").Append(e.server.rowID).Append(";");
                        b.Append("UPDATE pseudoServers SET ModificationDetector = '").Append(e.modDetect.ToString()).Append("', CustomBackgroundColor = ").Append(e.usesFill ? "'" + e.customBackgroundColor + "'" : "NULL").Append(", CustomBorderColor = ").Append(e.usesBorder ? "'" + e.customBorderColor + "'" : "NULL").Append(", CustomTextColor = ").Append(e.usesText ? "'" + e.customTextColor + "'" : "NULL").Append(" WHERE ServerID = ").Append(e.server.rowID).Append(";");

                        ProtocolAddress pa;
                        for (int i = 0; i < e.server.protocolAddresses.Count; i++)
                        {
                            pa = e.server.protocolAddresses[i];
                            if (pa.rowID < 0)
                            {
                                b.Append("INSERT INTO addresses (Protocol, Address, Port, AdditionalCMDParameters, ServerID) VALUES ").Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("','").Append(SecurityElement.Escape(pa.address)).Append("','").Append(pa.port).Append("','").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(e.server.rowID).Append(");");

                                cmd.CommandText = b.ToString();
                                b.Clear();
                                cmd.ExecuteNonQuery();

                                cmd = new SQLiteCommand(c);
                                b.Append("SELECT rowid FROM addresses WHERE ServerID = '").Append(e.server.rowID).Append("' ORDER BY rowid DESC LIMIT 1;");

                                cmd.CommandText = b.ToString();
                                b.Clear();
                                reader = cmd.ExecuteReader();
                                reader.Read();
                                pa.rowID = (long)reader["rowid"];
                                reader.Close();

                                cmd = new SQLiteCommand(c);
                            }
                            else
                            {
                                b.Append("UPDATE addresses SET Protocol = '").Append(SecurityElement.Escape(pa.protocol)).Append("', Address = '").Append(SecurityElement.Escape(pa.address)).Append("', Port = '").Append(pa.port).Append("', AdditionalCMDParameters = '").Append(SecurityElement.Escape(pa.parameters)).Append("' WHERE ServerID = ").Append(e.server.rowID).Append(";");
                                cmd.CommandText = b.ToString();
                                b.Clear();
                                cmd.ExecuteNonQuery();

                                cmd = new SQLiteCommand(c);
                            }
                        }

                        if ((int)e.type > 1)
                        {
                            PseudoWrappingEntity pwe = e as PseudoWrappingEntity;
                            PseudoEntity pe;
                            for (int i = 0; i < pwe.computersInCluster.Count; i++)
                            {
                                if (i == 0)
                                    b.Append("INSERT OR REPLACE INTO serverContents (ServerID, ParentServerID) VALUES ");
                                pe = pwe.computersInCluster[i];

                                b.Append("(").Append(pe.server.rowID).Append(", ").Append(e.server.rowID).Append(")");

                                if (i < pwe.computersInCluster.Count - 1)
                                    b.Append(",");
                                else
                                    b.Append(";");
                            }
                        }

                        if (removed != null)
                            for (int i = 0; i < removed.Count; i++)
                                b.Append("DELETE FROM addresses WHERE rowid = ").Append(removed[i].rowID).Append(" AND ServerID = ").Append(e.server.rowID).Append(";");

                        if (removedEntities != null)
                            for (int i = 0; i < removed.Count; i++)
                                b.Append("DELETE FROM serverContents WHERE ServerID = ").Append(removed[i].rowID).Append(" AND ParentServerID = ").Append(e.server.rowID).Append(";");

                        cmd.CommandText = b.ToString();
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        b.Append("INSERT INTO servers (FQDN, Category, Desc, Type) VALUES ('").Append(SecurityElement.Escape(e.server.fqdn)).Append("','").Append(SecurityElement.Escape(e.server.category)).Append("','").Append(SecurityElement.Escape(e.server.desc)).Append("',").Append((int)e.type).Append(");");

                        cmd.CommandText = b.ToString();
                        b.Clear();
                        cmd.ExecuteNonQuery();

                        cmd = new SQLiteCommand(c);
                        //b.Append("SELECT rowid FROM servers WHERE FQDN = '").Append(s.server.fqdn).Append("' AND Category = '").Append(s.server.category).Append("' AND Desc = '").Append(s.server.desc).Append("' ORDER BY rowid DESC LIMIT 1;");
                        b.Append("SELECT rowid FROM servers ORDER BY rowid DESC LIMIT 1;");

                        cmd.CommandText = b.ToString();
                        b.Clear();
                        reader = cmd.ExecuteReader();
                        reader.Read();
                        e.server.rowID = (long)reader["rowid"];
                        reader.Close();

                        cmd = new SQLiteCommand(c);
                        b.Append("INSERT INTO pseudoServers (ModificationDetector, CustomBackgroundColor, CustomBorderColor, CustomTextColor, ServerID) VALUES ('").Append(e.modDetect.ToString()).Append("',").Append(e.usesFill ? "'" + e.customBackgroundColor + "'" : "NULL").Append(", ").Append(e.usesBorder ? "'" + e.customBorderColor + "'" : "NULL").Append(", ").Append(e.usesText ? "'" + e.customTextColor + "'" : "NULL").Append(", ").Append(e.server.rowID).Append(");");
                        //b.Append("INSERT INTO addresses (Protocol, Address, Port, AdditionalCMDParameters, ServerID) VALUES ");
                        ProtocolAddress pa;
                        for (int i = 0; i < e.server.protocolAddresses.Count; i++)
                        {
                            pa = e.server.protocolAddresses[i];
                            b.Append("INSERT INTO addresses (Protocol, Address, Port, AdditionalCMDParameters, ServerID) VALUES ").Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("','").Append(SecurityElement.Escape(pa.address)).Append("','").Append(pa.port).Append("','").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(e.server.rowID).Append(");");

                            cmd.CommandText = b.ToString();
                            b.Clear();
                            cmd.ExecuteNonQuery();

                            cmd = new SQLiteCommand(c);
                            b.Append("SELECT rowid FROM addresses WHERE ServerID = '").Append(e.server.rowID).Append("' ORDER BY rowid DESC LIMIT 1;");

                            cmd.CommandText = b.ToString();
                            b.Clear();
                            reader = cmd.ExecuteReader();
                            reader.Read();
                            pa.rowID = (long)reader["rowid"];
                            reader.Close();

                            if(i < e.server.protocolAddresses.Count - 1)
                                cmd = new SQLiteCommand(c);
                        }

                        if ((int)e.type > 1)
                        {
                            PseudoWrappingEntity pwe = e as PseudoWrappingEntity;
                            PseudoEntity pe;
                            for (int i = 0; i < pwe.computersInCluster.Count; i++)
                            {
                                if (i == 0)
                                    b.Append("INSERT INTO serverContents (ServerID, ParentServerID) VALUES ");
                                pe = pwe.computersInCluster[i];

                                b.Append("(").Append(pe.server.rowID).Append(", ").Append(e.server.rowID).Append(")");

                                if (i < pwe.computersInCluster.Count - 1)
                                    b.Append(",");
                                else
                                    b.Append(";");
                            }
                        }

                        cmd.CommandText = b.ToString();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static async void SavePseudoEntityAsync(PseudoEntity e, List<ProtocolAddress> removed, List<PseudoEntity> removedEntities)
        {
            await queue.Enqueue(() => Task.Run(() => SavePseudoEntity(e.DeepCopy(), removed, removedEntities)));
        }

        public static void RemovePseudoEntity(PseudoEntity e)
        {
            using (SQLiteConnection c = EstablishDatabaseConnection(GetPropertyValue("localfile")))
            {
                if (c != null)
                {
                    SQLiteCommand cmd = new SQLiteCommand(c);
                    StringBuilder b = new StringBuilder();

                    b.Append("DELETE FROM servers WHERE rowid = ").Append(e.server.rowID).Append(";");
                    cmd.CommandText = b.ToString();
                    c.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static async void RemovePseudoEntityAsync(PseudoEntity e)
        {
            await queue.Enqueue(() => Task.Run(() => RemovePseudoEntity(e)));
        }


        public static bool ResolveHostname(string hostNameOrAddress, int millisecond_time_out)
        {
            ResolveState ioContext = new ResolveState(hostNameOrAddress);
            IAsyncResult result = Dns.BeginGetHostEntry(ioContext.HostName, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(millisecond_time_out), true);
            if (!success)
                ioContext.Result = ResolveType.Timeout;
            else
            {
                try
                {
                    IPHostEntry ipList = Dns.EndGetHostEntry(result);
                    if (ipList == null || ipList.AddressList == null || ipList.AddressList.Length == 0)
                        ioContext.Result = ResolveType.InvalidHost;
                    else
                        ioContext.Result = ResolveType.Completed;
                }
                catch
                {
                    ioContext.Result = ResolveType.InvalidHost;
                }
            }

            return ioContext.Result == ResolveType.Completed;
        }

        public static decimal ParseStringToDecimal(string source)
        {
            decimal value = 0m;
            decimal.TryParse(source, out value);

            return value;
        }

        public static decimal ParseStringToDecimal(string source, CultureInfo c)
        {
            decimal value = 0m;
            decimal.TryParse(source, NumberStyles.Float, c.NumberFormat, out value);

            return value;
        }

        public static string DBReaderColumnValueToString(object input)
        {
            if (input.GetType() == typeof(DBNull))
                return null;
            return (string)input;
        }

        public static List<string> GatherProtocols()
        {
            List<string> output = new List<string>();

            foreach (KeyValuePair<string, int> pair in DataHolder.protocolToPort)
            {
                if (!output.Contains(pair.Key))
                    output.Add(pair.Key);
            }

            Program p;
            for (int i = 0; i < DataHolder.programs.Count; i++)
            {
                p = DataHolder.programs[i];
                for (int j = 0; j < p.associations.Count; j++)
                {
                    if (!output.Contains(p.associations[j].protocol))
                        output.Add(p.associations[j].protocol);
                }
            }

            using (SQLiteConnection c = EstablishDatabaseConnection(GetPropertyValue("localfile")))
            {
                if (c != null)
                {
                    SQLiteCommand cmd = new SQLiteCommand(c);

                    cmd.CommandText = "SELECT DISTINCT Protocol FROM addresses";
                    c.Open();
                    SQLiteDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        string line;
                        while (reader.Read())
                        {
                            if(!output.Contains(line = (string)reader["Protocol"]))
                                output.Add(line);
                        }
                    }
                    reader.Close();
                }
            }

            return output;
        }

        private static HashSet<string> validCategories = new HashSet<string>();
        public static List<string> GatherCategories()
        {
            validCategories.Clear();

            using (SQLiteConnection c = EstablishDatabaseConnection(GetPropertyValue("localfile")))
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
                            validCategories.Add((string)reader["Category"]);
                    }
                }
            }

            return validCategories.ToList();
        }

        public static async Task<List<string>> GatherProtocolsAsync()
        {
            return await Task.Run(() => GatherProtocols());
        }

        public static List<ProtocolAddress> DeepCopy(this List<ProtocolAddress> sourceList)
        {
            List<ProtocolAddress> lpa = new List<ProtocolAddress>();

            ProtocolAddress pa;
            foreach (ProtocolAddress p in sourceList)
            {
                pa = new ProtocolAddress(p.protocol, p.address, p.port);
                pa.parameters = p.parameters;
                pa.rowID = p.rowID;
                lpa.Add(pa);
            }

            return lpa;
        }

        public static List<Program> DeepCopy(this List<Program> sourceList)
        {
            List<Program> lp = new List<Program>();

            Program deepProgram;
            foreach (Program p in sourceList)
            {
                lp.Add(deepProgram = new Program(p.FilePath));
                deepProgram.associations = p.associations.DeepCopy();
            }

            return lp;
        }

        public static List<ProtocolArgumentAssociation> DeepCopy(this List<ProtocolArgumentAssociation> sourceList)
        {
            List<ProtocolArgumentAssociation> paa = new List<ProtocolArgumentAssociation>();

            foreach (ProtocolArgumentAssociation p in sourceList)
                paa.Add(p.Copy());

            return paa;
        }

        public static List<CategoryColorAssociation> GetDeepCopiedList(this Dictionary<string, CategoryColorAssociation> sourceDict)
        {
            List<CategoryColorAssociation> cca = new List<CategoryColorAssociation>();

            foreach (KeyValuePair<string,CategoryColorAssociation> p in sourceDict)
                cca.Add(new CategoryColorAssociation(p.Value.category, p.Value.fillColor, p.Value.borderColor, p.Value.textColor));

            return cca;
        }

        public static List<CategoryColorAssociation> DeepCopy(this List<CategoryColorAssociation> sourceList)
        {
            List<CategoryColorAssociation> cca = new List<CategoryColorAssociation>();

            foreach (CategoryColorAssociation p in sourceList)
                cca.Add(new CategoryColorAssociation(p.category, p.fillColor, p.borderColor, p.textColor));

            return cca;
        }

        public static List<PseudoEntity> DeepCopy(this List<PseudoEntity> source)
        {
            PseudoServer s;
            List<PseudoEntity> output = new List<PseudoEntity>();
            foreach (PseudoEntity e in source)
            {   
                if ((s = e as PseudoServer) != null)
                    output.Add(s.DeepCopy());
                else
                    output.Add((e as PseudoWrappingEntity).DeepCopy());
            }

            return output;
        }
    }
}
