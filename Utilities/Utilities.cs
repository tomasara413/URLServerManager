using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.SQLite;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Security.Permissions;
using System.ComponentModel;
using System.Security;
using System.Collections.ObjectModel;
using URLServerManagerModern.Data;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;
using URLServerManagerModern.Windows.Main;
using URLServerManagerModern.Utilities.IO;

namespace URLServerManagerModern.Utilities
{
    public static class Utilities
    {
        #region Config File Parsing
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
        #endregion


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
            }
            catch (Exception e)
            {
                //throw;
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
                SetPropertyValue("locallocation", "\"" + sfd.FileName + "\"");
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
            cid = null;
        }

        static CSVImportDialog cid;
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
                    if (cid == null)
                    {
                        cid = new CSVImportDialog(csvi, queue, filePath);
                        cid.Closed += OnImportClosed;
                        cid.ShowActivated = true;
                        cid.Show();
                    }
                    else
                        cid.Activate();
                    break;
            }
        }

        public static async Task ImportAsync(string filePath)
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
                    if (cid == null)
                    {
                        cid = new CSVImportDialog(csvi, queue, filePath);
                        cid.Closed += OnImportClosed;
                        cid.ShowActivated = true;
                        cid.Show();
                        TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

                        EventHandler handler = null;
                        handler = (sender, args) => {
                            completion.SetResult(true);
                            (sender as Window).Closed -= handler;
                        };

                        cid.Closed += handler;

                        t = completion.Task;
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

        public static async Task ExportAsync()
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


        public static async Task ConvertToDB(string filePath)
        {
            CreateDatabase(GetPropertyValue("locallocation"));
            await ImportAsync(filePath);
        }

        private static TaskQueue queue = new TaskQueue();

        private static readonly string localTableSQL = "CREATE TABLE IF NOT EXISTS servers (rowid INTEGER PRIMARY KEY, Type INTEGER DEFAULT 0, FQDN TEXT, Category TEXT, Desc TEXT); CREATE TABLE IF NOT EXISTS addresses (Protocol TEXT NOT NULL, TCP INTEGER NOT NULL DEFAULT 1, Address TEXT NOT NULL, Port INTEGER NOT NULL, AdditionalCMDParameters TEXT, ServerID INTEGER NOT NULL, FOREIGN KEY (ServerID) REFERENCES servers(rowid) ON DELETE CASCADE); CREATE TABLE IF NOT EXISTS pseudoServers (ModificationDetector INTEGER, CustomBackgroundColor TEXT, CustomBorderColor TEXT, CustomTextColor TEXT, ServerID INTEGER NOT NULL, FOREIGN KEY (ServerID) REFERENCES servers(rowid) ON DELETE CASCADE); CREATE TABLE IF NOT EXISTS serverContents (ServerID INTEGER NOT NULL, ParentServerID INTEGER NOT NULL, PRIMARY KEY(ParentServerID, ServerID), FOREIGN KEY (ServerID) REFERENCES servers(rowid) ON DELETE CASCADE, FOREIGN KEY (ParentServerID) REFERENCES servers(rowid) ON DELETE CASCADE); CREATE TABLE IF NOT EXISTS defaultCategories (Category TEXT NOT NULL UNIQUE, CustomBackgroundColor TEXT, CustomBorderColor TEXT, CustomTextColor TEXT, PRIMARY KEY(Category)) WITHOUT ROWID;";
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
                cachedServers.Clear();
                cachedAddresses.Clear();

                using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(filePath))
                {
                    string table = localTableSQL;
                    c.Open();
                    SQLiteCommand cmd = new SQLiteCommand(table, c);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (UnauthorizedAccessException e) {
                ShowElevationDialog();
            }
        }

        public static void UpdateCachedServers()
        {
            Array.ForEach(cachedServers.Where(x => DataHolder.categoryColors.ContainsKey(x.Value.server.category)).ToArray(), x => {
                PseudoEntity s = x.Value;
                x.Value.customBackgroundColor = s.usesFill ? x.Value.customBackgroundColor : DataHolder.categoryColors[s.server.category].fillColor;
                x.Value.customBorderColor = s.usesBorder ? x.Value.customBorderColor : DataHolder.categoryColors[s.server.category].borderColor;
                x.Value.customTextColor = s.usesText ? x.Value.customTextColor : DataHolder.categoryColors[s.server.category].textColor;
            });
        }


        private static bool addressTestFinished = true, shouldTestingOccur = false;
        private static DateTime finishedAt;
        private static int renewTimeout;
        private static object lockOBJ = new object();
        public static void TestAllAddresses(object o)
        {
            lock (lockOBJ)
            {
                if (addressTestFinished &&
                    bool.TryParse(GetPropertyValue("allowportavailabilitydiagnostics"), out shouldTestingOccur) && shouldTestingOccur && 
                    int.TryParse(GetPropertyValue("portavailabilityrenew"), out renewTimeout) && DateTime.Now > finishedAt.AddMilliseconds(renewTimeout))
                {
                    addressTestFinished = false;
                    ProtocolAddress[] protocolAddresses = cachedAddresses.Values.ToArray().DeepCopy();

                    ProtocolAddress protocolAddress;
                    int timeout;
                    for (int i = 0; i < protocolAddresses.Length; i++)
                    {
                        protocolAddress = protocolAddresses[i];
                        if (protocolAddress.isTCP)
                        {
                            timeout = int.Parse(GetPropertyValue("portavailabilitytimeout"));
                            cachedAddresses[protocolAddress.rowID].status = WatcherWindow.GetStatus(protocolAddress, timeout, 2 * timeout);
                        }
                    };

                    addressTestFinished = true;
                    finishedAt = DateTime.Now;
                }
            }
        }

        public static SQLiteConnection EstablishSQLiteDatabaseConnection(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            return new SQLiteConnection("Data Source=" + filePath + ";Version=3;foreign keys=true;");
        }

        public static MySqlConnection EstablishMySQLDatabaseConnection(string user, string password, string database, string server, int port)
        {
            return new MySqlConnection("Server=" + server + ";Port=" + port + ";User ID=" + SecurityElement.Escape(user.Trim()) + ";Password=" + SecurityElement.Escape(password.Trim()) + ";Database=" + SecurityElement.Escape(database));
        }

        /**
         * <param name="select">Phrase for further specification of the selection parameters. It requires SQL format with WHERE keyword</param>
         **/
        public static List<PseudoEntity> LoadServers(long offset, long limit = DataHolder.LIMIT, string select = "WHERE ModificationDetector <> 2", bool readInDepth = true, string orderby = "ORDER BY FQDN ASC")
        {
            List<PseudoEntity> servers = new List<PseudoEntity>();
            try
            {
                using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
                {

                    if (c != null)
                    {
                        SQLiteCommand cmd = new SQLiteCommand(c);

                        cmd.CommandText = "SELECT * FROM servers LEFT JOIN pseudoServers ON rowid = pseudoServers.ServerID " + (select == null ? "" : select + " ") + (orderby == null ? "" : orderby + " ") + "LIMIT " + limit + " OFFSET " + offset;
                        c.Open();
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                        {

                            while (reader.Read())
                                servers.Add(ReadPseudoEntity(c, reader, readInDepth));
                        }

                        reader.Close();
                    }
                }
            }
            catch (Exception e) {
                //throw;
                Log("[ERROR] Loading entities", e.ToString());
            }
            return servers;
        }

        public static List<PseudoEntity> LoadRemoteServers(string user, string password, string database, string server, int port = 3306, long offset = 0, long limit = DataHolder.LIMIT, string select = "WHERE `ModificationDetector` <> 2", bool readInDepth = true, string orderby = "ORDER BY `FQDN` ASC")
        {
            List<PseudoEntity> servers = new List<PseudoEntity>();
            try
            {
                using (MySqlConnection c = EstablishMySQLDatabaseConnection(user, password, database, server, port))
                {

                    if (c != null)
                    {
                        MySqlCommand cmd = c.CreateCommand();

                        cmd.CommandText = "SELECT * FROM servers LEFT JOIN pseudoServers ON rowid = `pseudoServers`.`ServerID` " + (select == null ? "" : select + " ") + (orderby == null ? "" : orderby + " ") + "LIMIT " + limit + " OFFSET " + offset;
                        c.Open();
                        MySqlDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                        {

                            while (reader.Read())
                                servers.Add(ReadPseudoEntity(c, reader, user, password, database, server, port, readInDepth));
                        }

                        reader.Close();
                    }
                }
            }
            catch (Exception e)
            {
                //throw;
                Log("[ERROR] Loading entities", e.ToString());
            }
            return servers;
        }

        /**
         * <summary>
         * Looks up the ids of servers with certain address
         * </summary>
         * <param name="addressSelect">Phrase for further specification of the selection parameters. It requires SQL format with WHERE keyword</param>
         * <param name="additionalSelect">Specifies further which servers should be selected, this phrase souhld not include the SQL WHERE keyword</param>
         **/
        public static List<PseudoEntity> ReverseAddressLookup(long offset, long limit = DataHolder.LIMIT, string addressSelect = null, string additionalSelect = "ModificationDetector <> 2", bool readInDepth = true)
        {
            string select = "";
            if(string.IsNullOrEmpty(addressSelect?.Trim()))
                return LoadServers(offset, limit, string.IsNullOrEmpty(additionalSelect?.Trim()) ? null : "WHERE " + additionalSelect, readInDepth);
            try
            {
                using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
                {

                    if (c != null)
                    {
                        SQLiteCommand cmd = new SQLiteCommand(c);

                        cmd.CommandText = "SELECT DISTINCT servers.rowid FROM servers LEFT JOIN addresses ON rowid = addresses.ServerID  " + addressSelect + " ORDER BY FQDN ASC LIMIT 20 OFFSET 0";
                        c.Open();
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        StringBuilder sb = new StringBuilder();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                                sb.Append((long)reader[0]).Append(",");
                        }

                        if(sb.Length > 0)
                            sb.Remove(sb.Length - 1, 1);
                        select = sb.ToString();

                        reader.Close();
                    }
                }
            }
            catch (Exception e)
            {
                //throw;
                Log("[ERROR] Loading entities", e.ToString());
                return LoadServers(offset, limit, string.IsNullOrEmpty(additionalSelect?.Trim()) ? null : "WHERE " + additionalSelect, readInDepth);
            }

            return LoadServers(offset, limit, "WHERE rowid IN (" + select + ")" + (string.IsNullOrEmpty(additionalSelect?.Trim()) ? "" : "AND " + additionalSelect), readInDepth);
        }

        public static async Task<List<PseudoEntity>> LoadServersAsync(long offset, long limit = DataHolder.LIMIT, string select = "WHERE ModificationDetector <> 2", bool readInDepth = true)
        {
            return await Task.Run(() => LoadServers(offset, limit, select, readInDepth));
        }

        public static async Task<List<PseudoEntity>> ReverseAddressLookupAsync(long offset, long limit = DataHolder.LIMIT, string addressSelect = null, string additionalSelect = "ModificationDetector <> 2", bool readInDepth = true)
        {
            return await Task.Run(() => ReverseAddressLookup(offset, limit, addressSelect, additionalSelect, readInDepth));
        }

        static Dictionary<long, PseudoEntity> cachedServers = new Dictionary<long, PseudoEntity>();

        //In theory this implementation does not need cahing since different servers will have different addresses rendering this cache useless
        static Dictionary<long, ProtocolAddress> cachedAddresses = new Dictionary<long, ProtocolAddress>();

        /**
         * <summary>
         * Reads pseudo entity using provided connection and reader
         * </summary>
         * <param name="readInDepth">Sets whether to read all the servers in wrapper entities</param>
         **/
         //TODO: Change readInDepth to an int which dictates how many layers deep we should go
        private static PseudoEntity ReadPseudoEntity(SQLiteConnection c, SQLiteDataReader reader, bool readInDepth = true)
        {
            EntityType type = EntityType.Server;
            PseudoEntity s;

            type = (EntityType) Enum.Parse(typeof(EntityType),((long)reader["Type"]).ToString());

            switch (type)
            {
                default:
                    s = new PseudoServer(ModificationDetector.New, new Server());
                    break;
                case EntityType.VirtualizationServer:
                case EntityType.Cluster:
                    s = new PseudoWrappingEntity(ModificationDetector.New, new Server(), (PseudoEntity[])null);
                    break;
            }
            s.type = type;

            s.server.rowID = (long)reader["rowid"];

            lock (cachedServers)
            {
                if (cachedServers.ContainsKey(s.server.rowID))
                    return cachedServers[s.server.rowID];
                else
                    cachedServers.Add(s.server.rowID, s);
            }

            s.server.fqdn = new SecurityElement("unescape",(string)reader["FQDN"]).Text;
            s.server.category = new SecurityElement("unescape",(string)reader["Category"]).Text;
            s.server.desc = new SecurityElement("unescape",(string)reader["Desc"]).Text;

            s.modDetect = (ModificationDetector)Enum.Parse(typeof(ModificationDetector), ((long)reader["ModificationDetector"]).ToString());

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

            SQLiteCommand cmd = new SQLiteCommand("SELECT Protocol, TCP, Address, Port, AdditionalCMDParameters, rowid FROM addresses WHERE ServerID = " + s.server.rowID, c);
            SQLiteDataReader r2 = cmd.ExecuteReader();

            while (r2.Read())
                s.server.protocolAddresses.Add(ReadProtocolAddress(r2));

            r2.Close();


            if ((int)type > 1 && readInDepth)
            {
                //This could cause a catastrophe if there is one huge nested entity
                cmd = new SQLiteCommand("SELECT * FROM serverContents INNER JOIN servers ON serverContents.ServerID = servers.rowid INNER JOIN pseudoServers ON serverContents.ServerID = pseudoServers.ServerID WHERE serverContents.ParentServerID = " + s.server.rowID, (SQLiteConnection)c);
                r2 = cmd.ExecuteReader();

                while (r2.Read())
                    ((PseudoWrappingEntity)s).computersInCluster.Add(ReadPseudoEntity(c, r2, readInDepth));
                r2.Close();
            }

            return s;
        }

        private static PseudoEntity ReadPseudoEntity(MySqlConnection c, MySqlDataReader reader, string user, string password, string database, string server, int port = 3306, bool readInDepth = true)
        {
            EntityType type = EntityType.Server;
            PseudoEntity s;

            type = (EntityType)Enum.Parse(typeof(EntityType), ((int)reader["Type"]).ToString());

            switch (type)
            {
                default:
                    s = new PseudoServer(ModificationDetector.New, new Server());
                    break;
                case EntityType.VirtualizationServer:
                case EntityType.Cluster:
                    s = new PseudoWrappingEntity(ModificationDetector.New, new Server(), (PseudoEntity[])null);
                    break;
            }
            s.type = type;

            s.server.rowID = (int)reader["rowid"];

            s.server.fqdn = new SecurityElement("unescape", (string)reader["FQDN"]).Text;
            s.server.category = new SecurityElement("unescape", (string)reader["Category"]).Text;
            s.server.desc = new SecurityElement("unescape", (string)reader["Desc"]).Text;

            s.modDetect = (ModificationDetector)Enum.Parse(typeof(ModificationDetector), ((int)reader["ModificationDetector"]).ToString());

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

            using (MySqlConnection c2 = EstablishMySQLDatabaseConnection(user, password, database, server, port))
            {
                MySqlCommand cmd = c2.CreateCommand();
                cmd.CommandText = "SELECT `Protocol`, `TCP`, `Address`, `Port`, `AdditionalCMDParameters`, `rowid` FROM addresses WHERE `ServerID` =" + s.server.rowID;
                c2.Open();
                MySqlDataReader r2 = cmd.ExecuteReader();

                while (r2.Read())
                    s.server.protocolAddresses.Add(ReadProtocolAddress(r2));

                r2.Close();
                c2.Close();
            }

            if ((int)type > 1 && readInDepth)
            {

                //This could cause a catastrophe if there is one huge nested entity
                using (MySqlConnection c2 = EstablishMySQLDatabaseConnection(user, password, database, server, port))
                {
                    MySqlCommand cmd = c2.CreateCommand();
                    cmd.CommandText = "SELECT * FROM serverContents INNER JOIN servers ON `serverContents`.`ServerID` = `servers`.`rowid` INNER JOIN pseudoServers ON `serverContents`.`ServerID` = `pseudoServers`.`ServerID` WHERE `serverContents`.`ParentServerID` = " + s.server.rowID;
                    c2.Open();
                    MySqlDataReader r2 = cmd.ExecuteReader();

                    while (r2.Read())
                        ((PseudoWrappingEntity)s).computersInCluster.Add(ReadPseudoEntity(c2, r2, user, password, database, server, port, readInDepth));
                    r2.Close();
                    c2.Close();
                }
            }

            return s;
        }

        private static ProtocolAddress ReadProtocolAddress(SQLiteDataReader r2)
        {
            ProtocolAddress pa = new ProtocolAddress(new SecurityElement("unescape", (string)r2["Protocol"]).Text, new SecurityElement("unescape", (string)r2["Address"]).Text, (int)((long)r2["Port"]));
            pa.rowID = (long)r2["rowid"];
            pa.isTCP = (long)r2["TCP"] == 1;

            lock (cachedAddresses)
            {
                if (cachedAddresses.ContainsKey(pa.rowID))
                   return cachedAddresses[pa.rowID];
                else
                {
                    cachedAddresses.Add(pa.rowID, pa);
                    
                    pa.parameters = new SecurityElement("unescape", (string)r2["AdditionalCMDParameters"]).Text;
                }
            }

            return pa;
        }

        private static ProtocolAddress ReadProtocolAddress(MySqlDataReader r2)
        {
            ProtocolAddress pa = new ProtocolAddress(new SecurityElement("unescape", (string)r2["Protocol"]).Text, new SecurityElement("unescape", (string)r2["Address"]).Text, (int)r2["Port"]);
            pa.rowID = (int)r2["rowid"];
            pa.isTCP = (bool)r2["TCP"];

            pa.parameters = new SecurityElement("unescape", (string)r2["AdditionalCMDParameters"]).Text;

            return pa;
        }

        public static void LoadCategoryColors()
        {
            try
            {
                using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
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
                                DataHolder.categoryColors.Add(ca = new SecurityElement("unescape", (string)reader["Category"]).Text, new CategoryColorAssociation(ca, (string)reader["CustomBackgroundColor"], (string)reader["CustomBorderColor"], (string)reader["CustomTextColor"]));
                        }

                        reader.Close();
                    }
                }
            }
            catch (Exception e)
            {
                //throw;
                Log("[ERROR] Loading categories", e.ToString());
            }
        }

        public static async Task LoadCategoryColorsAsync()
        {
            await Task.Run(() => LoadCategoryColors());
        }

        public static void SaveDefaultCategories(List<CategoryColorAssociation> currentList)
        {
            List<string> toBeRemoved = DataHolder.categoryColors.Where(x => !currentList.Select(y => y.category).Contains(x.Key)).Select(x => x.Key).ToList();

            if(currentList != null)
                DataHolder.categoryColors = currentList.ToDictionary(x => x.category, y => new CategoryColorAssociation(y.category, y.fillColor, y.borderColor, y.textColor));

            try
            {
                using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
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
            catch (Exception e)
            {
                //throw;
                Log("[ERROR] Saving default categories", e.ToString());
            }
        }

        public static async Task SaveDefaultCategoriesAsync(List<CategoryColorAssociation> currentList)
        {
            if(currentList != null)
                await queue.Enqueue(() => Task.Run(() => SaveDefaultCategories(currentList.DeepCopy())));
        }

        //TODO: Sweep down the code where there could be invalid custom colors when the user is too 'intelligent'
        public static void SavePseudoEntity(PseudoEntity e, List<ProtocolAddress> removed, List<PseudoEntity> removedEntities)
        {
            try {
                using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
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
                            b.Append("UPDATE pseudoServers SET ModificationDetector = ").Append((int)e.modDetect).Append(", CustomBackgroundColor = ").Append(e.usesFill ? "'" + e.customBackgroundColor + "'" : "NULL").Append(", CustomBorderColor = ").Append(e.usesBorder ? "'" + e.customBorderColor + "'" : "NULL").Append(", CustomTextColor = ").Append(e.usesText ? "'" + e.customTextColor + "'" : "NULL").Append(" WHERE ServerID = ").Append(e.server.rowID).Append(";");

                            ProtocolAddress pa;
                            for (int i = 0; i < e.server.protocolAddresses.Count; i++)
                            {
                                pa = e.server.protocolAddresses[i];
                                if (pa.rowID < 0)
                                {
                                    b.Append("INSERT INTO addresses (Protocol, TCP, Address, Port, AdditionalCMDParameters, ServerID) VALUES ").Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(",'").Append(SecurityElement.Escape(pa.hostname)).Append("',").Append(pa.port).Append(",'").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(e.server.rowID).Append(");");

                                    cmd.CommandText = b.ToString();
                                    b.Clear();
                                    cmd.ExecuteNonQuery();

                                    cmd = new SQLiteCommand(c);
                                    b.Append("SELECT last_insert_rowid() as rowid FROM addresses;");

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
                                    b.Append("UPDATE addresses SET Protocol = '").Append(SecurityElement.Escape(pa.protocol)).Append("', TCP = ").Append(pa.isTCP ? "1" : "0").Append(", Address = '").Append(SecurityElement.Escape(pa.hostname)).Append("', Port = ").Append(pa.port).Append(", AdditionalCMDParameters = '").Append(SecurityElement.Escape(pa.parameters)).Append("' WHERE rowid = ").Append(pa.rowID).Append(";");
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
                                    b.Append("DELETE FROM addresses WHERE rowid = ").Append(removed[i].rowID).Append(";");

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
                            b.Append("SELECT last_insert_rowid() AS rowid FROM servers;");

                            cmd.CommandText = b.ToString();
                            b.Clear();
                            reader = cmd.ExecuteReader();
                            reader.Read();
                            e.server.rowID = (long)reader["rowid"];
                            reader.Close();

                            cmd = new SQLiteCommand(c);
                            b.Append("INSERT INTO pseudoServers (ModificationDetector, CustomBackgroundColor, CustomBorderColor, CustomTextColor, ServerID) VALUES ('").Append((int)e.modDetect).Append("',").Append(e.usesFill ? "'" + e.customBackgroundColor + "'" : "NULL").Append(", ").Append(e.usesBorder ? "'" + e.customBorderColor + "'" : "NULL").Append(", ").Append(e.usesText ? "'" + e.customTextColor + "'" : "NULL").Append(", ").Append(e.server.rowID).Append(");");
                            //b.Append("INSERT INTO addresses (Protocol, Address, Port, AdditionalCMDParameters, ServerID) VALUES ");
                            ProtocolAddress pa;
                            for (int i = 0; i < e.server.protocolAddresses.Count; i++)
                            {
                                pa = e.server.protocolAddresses[i];
                                b.Append("INSERT INTO addresses (Protocol, TCP, Address, Port, AdditionalCMDParameters, ServerID) VALUES ").Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(",'").Append(SecurityElement.Escape(pa.hostname)).Append("',").Append(pa.port).Append(",'").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(e.server.rowID).Append(");");

                                cmd.CommandText = b.ToString();
                                b.Clear();
                                cmd.ExecuteNonQuery();

                                cmd = new SQLiteCommand(c);
                                b.Append("SELECT last_insert_rowid() AS rowid FROM addresses;");

                                cmd.CommandText = b.ToString();
                                b.Clear();
                                reader = cmd.ExecuteReader();
                                reader.Read();
                                pa.rowID = (long)reader["rowid"];
                                reader.Close();

                                if (i < e.server.protocolAddresses.Count - 1)
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
            catch (Exception ex)
            {
                //throw;
                Log("[ERROR] Saving entity", ex.ToString());
            }
        }

        public static async Task SavePseudoEntityAsync(PseudoEntity e, List<ProtocolAddress> removed, List<PseudoEntity> removedEntities)
        {
            if (e != null)
                await queue.Enqueue(() => Task.Run(() => SavePseudoEntity(e.DeepCopy(), removed, removedEntities)));
        }

        public static void SavePseudoEntityDirty(PseudoEntity e)
        {
            try
            {
                using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
                {
                    if (c != null)
                    {
                        SQLiteCommand cmd = new SQLiteCommand(c);
                        StringBuilder b = new StringBuilder();
                        SQLiteDataReader reader;
                        c.Open();
                        if (e.server.rowID >= 0)
                        {
                            b.Append("DELETE FROM addresses WHERE ServerID = ").Append(e.server.rowID).Append(";");
                            b.Append("UPDATE servers SET FQDN = '").Append(SecurityElement.Escape(e.server.fqdn)).Append("', Category = '").Append(SecurityElement.Escape(e.server.category)).Append("', Desc = '").Append(SecurityElement.Escape(e.server.desc)).Append("', Type = ").Append((int)e.type).Append(" WHERE rowid = ").Append(e.server.rowID).Append(";");
                            b.Append("UPDATE pseudoServers SET ModificationDetector = ").Append((int)e.modDetect).Append(", CustomBackgroundColor = ").Append(e.usesFill ? "'" + e.customBackgroundColor + "'" : "NULL").Append(", CustomBorderColor = ").Append(e.usesBorder ? "'" + e.customBorderColor + "'" : "NULL").Append(", CustomTextColor = ").Append(e.usesText ? "'" + e.customTextColor + "'" : "NULL").Append(" WHERE ServerID = ").Append(e.server.rowID).Append(";");

                            ProtocolAddress pa;
                            for (int i = 0; i < e.server.protocolAddresses.Count; i++)
                            {
                                pa = e.server.protocolAddresses[i];
                                if (pa.rowID < 0)
                                {
                                    b.Append("INSERT INTO addresses (Protocol, TCP, Address, Port, AdditionalCMDParameters, ServerID) VALUES ").Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(",'").Append(SecurityElement.Escape(pa.hostname)).Append("',").Append(pa.port).Append(",'").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(e.server.rowID).Append(");");

                                    cmd.CommandText = b.ToString();
                                    b.Clear();
                                    cmd.ExecuteNonQuery();

                                    cmd = new SQLiteCommand(c);
                                    b.Append("SELECT last_insert_rowid() as rowid FROM addresses;");

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
                                    b.Append("INSERT OR REPLACE INTO addresses (rowid, Protocol, TCP, Address, Port, AdditionalCMDParameters, ServerID) VALUES ").Append("(").Append(pa.rowID).Append(",'").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(",'").Append(SecurityElement.Escape(pa.hostname)).Append("',").Append(pa.port).Append(",'").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(e.server.rowID).Append(");");
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
                                    {
                                        b.Append("DELETE FROM serverContents WHERE ParentServerID = ").Append(e.server.rowID).Append(";");
                                        b.Append("INSERT OR REPLACE INTO serverContents (ServerID, ParentServerID) VALUES ");
                                    }
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
                        else
                        {
                            b.Append("INSERT INTO servers (FQDN, Category, Desc, Type) VALUES ('").Append(SecurityElement.Escape(e.server.fqdn)).Append("','").Append(SecurityElement.Escape(e.server.category)).Append("','").Append(SecurityElement.Escape(e.server.desc)).Append("',").Append((int)e.type).Append(");");

                            cmd.CommandText = b.ToString();
                            b.Clear();
                            cmd.ExecuteNonQuery();

                            cmd = new SQLiteCommand(c);
                            //b.Append("SELECT rowid FROM servers WHERE FQDN = '").Append(s.server.fqdn).Append("' AND Category = '").Append(s.server.category).Append("' AND Desc = '").Append(s.server.desc).Append("' ORDER BY rowid DESC LIMIT 1;");
                            b.Append("SELECT last_insert_rowid() AS rowid FROM servers;");

                            cmd.CommandText = b.ToString();
                            b.Clear();
                            reader = cmd.ExecuteReader();
                            reader.Read();
                            e.server.rowID = (long)reader["rowid"];
                            reader.Close();

                            cmd = new SQLiteCommand(c);
                            b.Append("INSERT INTO pseudoServers (ModificationDetector, CustomBackgroundColor, CustomBorderColor, CustomTextColor, ServerID) VALUES ('").Append((int)e.modDetect).Append("',").Append(e.usesFill ? "'" + e.customBackgroundColor + "'" : "NULL").Append(", ").Append(e.usesBorder ? "'" + e.customBorderColor + "'" : "NULL").Append(", ").Append(e.usesText ? "'" + e.customTextColor + "'" : "NULL").Append(", ").Append(e.server.rowID).Append(");");
                            //b.Append("INSERT INTO addresses (Protocol, Address, Port, AdditionalCMDParameters, ServerID) VALUES ");
                            ProtocolAddress pa;
                            for (int i = 0; i < e.server.protocolAddresses.Count; i++)
                            {
                                pa = e.server.protocolAddresses[i];
                                b.Append("INSERT INTO addresses (Protocol, TCP, Address, Port, AdditionalCMDParameters, ServerID) VALUES ").Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(",'").Append(SecurityElement.Escape(pa.hostname)).Append("',").Append(pa.port).Append(",'").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(e.server.rowID).Append(");");

                                cmd.CommandText = b.ToString();
                                b.Clear();
                                cmd.ExecuteNonQuery();

                                cmd = new SQLiteCommand(c);
                                b.Append("SELECT last_insert_rowid() AS rowid FROM addresses;");

                                cmd.CommandText = b.ToString();
                                b.Clear();
                                reader = cmd.ExecuteReader();
                                reader.Read();
                                pa.rowID = (long)reader["rowid"];
                                reader.Close();

                                if (i < e.server.protocolAddresses.Count - 1)
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
            catch (Exception ex)
            {
                //throw;
                Log("[ERROR] Saving entity", ex.ToString());
            }
        }

        public static async Task SavePseudoEntityDirtyAsync(PseudoEntity e)
        {
            if (e != null)
                await queue.Enqueue(() => Task.Run(() => SavePseudoEntityDirty(e.DeepCopy())));
        }

        public static void RemovePseudoEntity(PseudoEntity e)
        {
            try
            {
                using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
                {
                    if (c != null)
                    {
                        SQLiteCommand cmd = new SQLiteCommand(c);
                        StringBuilder b = new StringBuilder();

                        if(string.IsNullOrEmpty(GetPropertyValue("remotelocation")?.Trim()))
                            b.Append("DELETE FROM servers WHERE rowid = ").Append(e.server.rowID).Append(";");
                        else
                            b.Append("UPDATE pseudoServers SET ModificationDetector = ").Append((int)ModificationDetector.Removed).Append(" WHERE ServerID = ").Append(e.server.rowID).Append(";");
                        cmd.CommandText = b.ToString();
                        c.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                //throw;
                Log("[ERROR] Removing entity", ex.ToString());
            }
        }

        public static async Task RemovePseudoEntityAsync(PseudoEntity e)
        {
            if (e != null)
            {
                cachedServers.Remove(e.server.rowID);
                await queue.Enqueue(() => Task.Run(() => RemovePseudoEntity(e)));
            }
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

        public static DateTime DBReaderColumnValueToDateTime(object input)
        {
            if (input.GetType() == typeof(DBNull))
                return DateTime.MinValue;
            return (DateTime)input;
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

            using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
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

            using (SQLiteConnection c = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
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


        public static void RefreshAllDynamicResources(MainWindow mainWindow)
        {
            mainWindow.Resources["fontSize"] = DataHolder.fontSize;
        }


        static Dictionary<long, long> savedIDToTempRowID = new Dictionary<long, long>();
        static List<PseudoWrappingEntity> insertedNestedEntities = new List<PseudoWrappingEntity>();

        public enum SynchronizationScale {
            UpdateEntities = 1,
            UpdateDefaultColors = 2,
            RecoverEntities = 4,
            RecoverDefaultColors = 8,
            Full = 1 | 2 | 8
        }

        public static Dictionary<PseudoEntity, PseudoEntity> SynchronizeWithServer(string user, string password, string database = "URLManagerDatabase", string server = null, int port = 3306, SynchronizationScale scale = SynchronizationScale.Full)
        {
            Dictionary<PseudoEntity, PseudoEntity> unresolved = new Dictionary<PseudoEntity, PseudoEntity>();
            if (string.IsNullOrEmpty(server?.Trim()))
                server = GetPropertyValue("remotelocation");

            //If we update the default colors, we do not need to retrieve them back, since we will have the most recent version already
            if ((scale & SynchronizationScale.UpdateDefaultColors) != 0)
                scale &= ~SynchronizationScale.RecoverDefaultColors;

            //Recover entities is meant only to sync our state with the remote without updating it with our possibly corrupted data (unwanted deletion/modification).
            //We cannot allow the user to update the database only as for it could cause synchronization problems.
            if ((scale & SynchronizationScale.RecoverEntities) != 0)
                scale &= ~SynchronizationScale.UpdateEntities;

            if (!string.IsNullOrEmpty(server?.Trim()) && user != null && password != null)
            {
                try
                {
                    StringBuilder command = new StringBuilder();
                    StringBuilder insertBuilder = new StringBuilder();
                    
                    StringBuilder defaultCategories = new StringBuilder();
                    StringBuilder pseudoServerInsert = new StringBuilder();
                    StringBuilder addressInsert = new StringBuilder();
                    StringBuilder serverContentInsert = new StringBuilder();
                    PseudoEntity pe;
                    ProtocolAddress pa;
                    
                    #region Sending Client Data
                    StringBuilder removeBuilder = new StringBuilder();

                    command.Append("BEGIN;");
                    command.Append("CREATE TABLE IF NOT EXISTS servers (`rowid` INT NOT NULL AUTO_INCREMENT PRIMARY KEY, `Type` INT NOT NULL DEFAULT 0, `FQDN` TEXT NULL, `Category` TEXT NULL, `Desc` TEXT NULL);")
                            .Append("CREATE TABLE IF NOT EXISTS addresses (`rowid` INT NOT NULL AUTO_INCREMENT PRIMARY KEY, `Protocol` TEXT NOT NULL, `TCP` TINYINT(1) NOT NULL DEFAULT 1, `Address` TEXT NOT NULL, `Port` INT NOT NULL, `AdditionalCMDParameters` TEXT NULL DEFAULT NULL, `ServerID` INT NOT NULL, FOREIGN KEY(`ServerID`) REFERENCES servers(`rowid`) ON DELETE CASCADE);")
                            .Append("CREATE TABLE IF NOT EXISTS pseudoServers (`ModificationDetector` INT NOT NULL, `CustomBackgroundColor` VARCHAR(9) NULL, `CustomBorderColor` VARCHAR(9) NULL, `CustomTextColor` VARCHAR(9) NULL, `ServerID` INT NOT NULL UNIQUE, FOREIGN KEY (`ServerID`) REFERENCES servers(`rowid`) ON DELETE CASCADE);")
                            .Append("CREATE TABLE IF NOT EXISTS serverContents (`ServerID` INT NOT NULL, `ParentServerID` INT NOT NULL, PRIMARY KEY(`ParentServerID`, `ServerID`), FOREIGN KEY (ServerID) REFERENCES servers(`rowid`) ON DELETE CASCADE, FOREIGN KEY (`ParentServerID`) REFERENCES servers(`rowid`) ON DELETE CASCADE);")
                            .Append("CREATE TABLE IF NOT EXISTS changeJournual (`ServerID` INT PRIMARY KEY NOT NULL, `LastModified` DATETIME NULL, `LastRemoved` DATETIME NULL);");

                    if ((scale & SynchronizationScale.RecoverDefaultColors) != 0)
                        command.Append("CREATE TABLE IF NOT EXISTS defaultCategories (`Category` VARCHAR(191) NOT NULL UNIQUE, `CustomBackgroundColor` VARCHAR(9) NULL, `CustomBorderColor` VARCHAR(9) NULL, `CustomTextColor` VARCHAR(9) NULL, PRIMARY KEY(`Category`));");

                    int offset = 0;
                    List<PseudoEntity> entities;

                    if ((scale & SynchronizationScale.UpdateEntities) != 0)
                    {
                        command.Append("CREATE TABLE IF NOT EXISTS rowids (`tempRowID` INT PRIMARY KEY NOT NULL, `realRowID` INT NOT NULL);");

                        long tempRowID = 0;
                        DateTime lastSynced;
                        DateTime.TryParse(GetPropertyValue("lastsynchronization"), out lastSynced);

                        entities = LoadServers(offset, 20, "WHERE ModificationDetector <> 3", true, "ORDER BY ModificationDetector ASC");
                        do
                        {
                            for (int i = 0; i < entities.Count; i++)
                            {
                                pe = entities[i];
                                //CategoryColorAssociation cca;

                                reprocess:;
                                if (pe.modDetect == ModificationDetector.New)
                                {
                                    if (pe.type > EntityType.VirtualServer)
                                        insertedNestedEntities.Add(pe as PseudoWrappingEntity);

                                    savedIDToTempRowID.Add(pe.server.rowID, tempRowID);
                                    if (tempRowID == 0)
                                    {
                                        pseudoServerInsert.Append("INSERT INTO pseudoServers (`ModificationDetector`, `CustomBackgroundColor`, `CustomBorderColor`, `CustomTextColor`, `ServerID`) VALUES ");
                                        addressInsert.Append("INSERT INTO addresses (`Protocol`, `TCP`, `Address`, `Port`, `AdditionalCMDParameters`, `ServerID`) VALUES ");
                                    }
                                    insertBuilder.Append("INSERT INTO servers (`FQDN`, `Category`, `Desc`, `Type`) VALUES ").Append("('").Append(SecurityElement.Escape(pe.server.fqdn)).Append("','").Append(SecurityElement.Escape(pe.server.category)).Append("','").Append(SecurityElement.Escape(pe.server.desc)).Append("',").Append((int)pe.type).Append(");");
                                    insertBuilder.Append("INSERT INTO rowids (`tempRowID`, `realRowID`) VALUES ").Append("(").Append(tempRowID).Append(",(SELECT LAST_INSERT_ID()));");
                                    insertBuilder.Append("INSERT INTO changeJournual (`ServerID`, `LastModified`) VALUES ((SELECT `realRowID` FROM rowids WHERE `tempRowID` = ").Append(savedIDToTempRowID[pe.server.rowID]).Append(" LIMIT 1)").Append(",'").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:s")).Append("');");

                                    pseudoServerInsert.Append("('").Append((int)ModificationDetector.Null).Append("', ").Append(pe.usesFill ? "'" + pe.customBackgroundColor + "'" : "NULL").Append(", ").Append(pe.usesBorder ? "'" + pe.customBorderColor + "'" : "NULL").Append(", ").Append(pe.usesText ? "'" + pe.customTextColor + "'" : "NULL").Append(", (SELECT `realRowID` FROM rowids WHERE `tempRowID` = ").Append(tempRowID).Append(" LIMIT 1)),");

                                    for (int j = 0; j < pe.server.protocolAddresses.Count; j++)
                                    {
                                        pa = pe.server.protocolAddresses[j];
                                        addressInsert.Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(", '").Append(SecurityElement.Escape(pa.hostname)).Append("', ").Append(pa.port).Append(", '").Append(SecurityElement.Escape(pa.parameters)).Append("', (SELECT `realRowID` FROM rowids WHERE `tempRowID` = ").Append(tempRowID).Append(" LIMIT 1)),");
                                    }

                                    tempRowID++;
                                }
                                else if (pe.modDetect == ModificationDetector.Modified)
                                {
                                    using (MySqlConnection connection = EstablishMySQLDatabaseConnection(user, password, database, server, port))
                                    {
                                        if (connection != null)
                                        {
                                            connection.Open();

                                            MySqlCommand cmd1 = connection.CreateCommand();
                                            cmd1.CommandText = "SELECT `LastModified`, `LastRemoved` FROM changeJournual WHERE `ServerID`=" + pe.server.rowID + " LIMIT 1;";

                                            MySqlDataReader dr = cmd1.ExecuteReader();
                                            if (dr.Read())
                                            {
                                                DateTime removed = DBReaderColumnValueToDateTime(dr["LastRemoved"]);
                                                DateTime modifed = DBReaderColumnValueToDateTime(dr["LastModified"]);
                                                if (modifed > removed)
                                                {
                                                    //Server has been modified since our last syncronization, but has not been deleted
                                                    if (modifed > lastSynced && removed < lastSynced)
                                                    {
                                                        unresolved.Add(pe, LoadRemoteServers(user, password, database, server, port, 0, 1, "WHERE `rowid` = '" + pe.server.rowID + "'", true, null)?[0]);
                                                        continue;
                                                    }

                                                    //Has been effectivelly replaced by a different server
                                                    if (modifed > lastSynced && removed > lastSynced)
                                                    {
                                                        pe.modDetect = ModificationDetector.New;
                                                        goto reprocess;
                                                    }
                                                }
                                                //The server has been removed before it was modified and since server is modified while it is added, this means, that currently there is no server associated with this id
                                                else
                                                {
                                                    pe.modDetect = ModificationDetector.New;
                                                    goto reprocess;
                                                }

                                                insertBuilder.Append("UPDATE changeJournual SET `LastModified` = '").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:s")).Append("' WHERE `ServerID`=").Append(pe.server.rowID).Append(";");
                                            }
                                            else
                                            {
                                                Log("[ERROR] Synchronization", "The state of local database does not permit a safe synchronization.");
                                                return null;
                                            }
                                            dr.Close();
                                        }
                                    }

                                    insertBuilder.Append("UPDATE servers SET `FQDN` = '").Append(SecurityElement.Escape(pe.server.fqdn)).Append("', `Category` = '").Append(SecurityElement.Escape(pe.server.category)).Append("',`Desc`= '").Append(SecurityElement.Escape(pe.server.desc)).Append("', `Type` = ").Append((int)pe.type).Append(" WHERE `rowid` = ").Append(pe.server.rowID).Append(";");
                                    insertBuilder.Append("UPDATE pseudoServers SET `ModificationDetector` = ").Append((int)ModificationDetector.Null).Append(", `CustomBackgroundColor` = ").Append(pe.usesFill ? "'" + pe.customBackgroundColor + "'" : "NULL").Append(", `CustomBorderColor` = ").Append(pe.usesBorder ? "'" + pe.customBorderColor + "'" : "NULL").Append(", `CustomTextColor` = ").Append(pe.usesText ? "'" + pe.customTextColor + "'" : "NULL").Append(" WHERE `ServerID` = ").Append(pe.server.rowID).Append(";");

                                    addressInsert.Append("DELETE FROM addresses WHERE `ServerID` = ").Append(pe.server.rowID).Append(";");
                                    for (int j = 0; j < pe.server.protocolAddresses.Count; j++)
                                    {
                                        pa = pe.server.protocolAddresses[j];
                                        addressInsert.Append("INSERT INTO addresses (`Protocol`, `TCP`, `Address`, `Port`, `AdditionalCMDParameters`, `ServerID`) VALUES ('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(",'").Append(SecurityElement.Escape(pa.hostname)).Append("',").Append(pa.port).Append(",'").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(pe.server.rowID).Append(");");
                                    }

                                    if (pe.type > EntityType.VirtualServer)
                                    {
                                        insertedNestedEntities.Add(pe as PseudoWrappingEntity);
                                        serverContentInsert.Append("DELETE FROM serverContents WHERE `ParentServerID` = ").Append(pe.server.rowID).Append(";");

                                    }
                                }
                                else
                                {
                                    using (MySqlConnection connection = EstablishMySQLDatabaseConnection(user, password, database, server, port))
                                    {
                                        if (connection != null)
                                        {
                                            connection.Open();

                                            MySqlCommand cmd1 = connection.CreateCommand();
                                            cmd1.CommandText = "SELECT `LastModified`, `LastRemoved` FROM `changeJournual` WHERE `ServerID`=" + pe.server.rowID + " LIMIT 1;";

                                            MySqlDataReader dr = cmd1.ExecuteReader();
                                            if (dr.Read())
                                            {
                                                DateTime removed = DBReaderColumnValueToDateTime(dr["LastRemoved"]);
                                                DateTime modifed = DBReaderColumnValueToDateTime(dr["LastModified"]);
                                                if (modifed > removed)
                                                {
                                                    //Remove
                                                    if (removed < lastSynced)
                                                    {
                                                        removeBuilder.Append("UPDATE changeJournual SET `LastRemoved` = '").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:s")).Append("' WHERE `ServerID`=").Append(pe.server.rowID).Append(";");
                                                        //cascades should handle rest of the hassle, but we will do it anyway just in case
                                                        removeBuilder.Append("DELETE FROM servers WHERE `rowid`=").Append(pe.server.rowID).Append(";");
                                                        removeBuilder.Append("DELETE FROM addresses WHERE `ServerID`=").Append(pe.server.rowID).Append(";");
                                                        removeBuilder.Append("DELETE FROM pseudoServers WHERE `ServerID`=").Append(pe.server.rowID).Append(";");
                                                        removeBuilder.Append("DELETE FROM serverContents WHERE `ParentServerID`=").Append(pe.server.rowID).Append(";");
                                                    }
                                                }
                                            }
                                            dr.Close();
                                        }
                                    }
                                }
                            }

                            offset += 20;
                        } while ((entities = LoadServers(offset, 20, "WHERE ModificationDetector <> 3", true, "ORDER BY ModificationDetector ASC")).Count >= 20);

                        if (insertedNestedEntities.Count > 0)
                        {
                            bool insertPresent = false;
                            PseudoWrappingEntity pwe;
                            for (int i = 0; i < insertedNestedEntities.Count; i++)
                            {
                                pwe = insertedNestedEntities[i];

                                for (int j = 0; j < pwe.computersInCluster.Count; j++)
                                {
                                    pe = pwe.computersInCluster[j];

                                    if (pe.modDetect == ModificationDetector.Removed)
                                        continue;


                                    if (!insertPresent)
                                    {
                                        serverContentInsert.Append("INSERT INTO serverContents (`ServerID`, `ParentServerID`) VALUES ");
                                        insertPresent = true;
                                    }

                                    if (pe.modDetect != ModificationDetector.New)
                                        serverContentInsert.Append("(").Append(pe.server.rowID).Append(", ").Append(pwe.server.rowID).Append(")");
                                    else
                                        serverContentInsert.Append("((SELECT `realRowID` FROM rowids WHERE `tempRowID` = ").Append(savedIDToTempRowID[pe.server.rowID]).Append(" LIMIT 1)").Append(", ").Append(pwe.server.rowID).Append(")");

                                    serverContentInsert.Append(",");
                                }

                            }
                        }
                    }

                    if ((scale & SynchronizationScale.UpdateDefaultColors) != 0)
                    {
                        command.Append("DROP TABLE IF EXISTS `defaultCategories`;");
                        if (DataHolder.categoryColors.Count > 0)
                        {
                            command.Append("CREATE TABLE IF NOT EXISTS defaultCategories (`Category` VARCHAR(191) NOT NULL UNIQUE, `CustomBackgroundColor` VARCHAR(9) NULL, `CustomBorderColor` VARCHAR(9) NULL, `CustomTextColor` VARCHAR(9) NULL, PRIMARY KEY(`Category`));");
                            defaultCategories.Append("INSERT INTO defaultCategories (`Category`, `CustomBackgroundColor`, `CustomBorderColor`, `CustomTextColor`) VALUES ");
                            foreach (KeyValuePair<string, CategoryColorAssociation> pair in DataHolder.categoryColors)
                                defaultCategories.Append("('").Append(SecurityElement.Escape(pair.Key)).Append("', '").Append(pair.Value.fillColor).Append("', '").Append(pair.Value.borderColor).Append("', '").Append(pair.Value.textColor).Append("'),");
                        }
                    }


                    if (pseudoServerInsert.Length > 0)
                    {
                        pseudoServerInsert.Length--;
                        pseudoServerInsert.Append(";");
                    }
                    if (addressInsert.Length > 0)
                    {
                        addressInsert.Length--;
                        addressInsert.Append(";");
                    }

                    command.Append(removeBuilder).Append(insertBuilder).Append(pseudoServerInsert).Append(addressInsert);

                    if (serverContentInsert.Length > 0)
                    {
                        serverContentInsert.Length--;
                        command.Append(serverContentInsert.Append(";"));
                    }

                    if (defaultCategories.Length > 0)
                    {
                        defaultCategories.Length--;
                        command.Append(defaultCategories.Append(";"));
                    }


                    command.Append("DROP TABLE IF EXISTS rowids;");
                    command.Append("COMMIT;");

                    using (MySqlConnection connection = EstablishMySQLDatabaseConnection(user, password, database, server, port))
                    {
                        if (connection != null)
                        {
                            connection.Open();
                            MySqlCommand cmd = connection.CreateCommand();
                            cmd.CommandText = command.ToString();
                            if (connection.State != System.Data.ConnectionState.Open)
                                connection.Open();

                            cmd.ExecuteNonQuery();
                        }
                    }
                    #endregion

                    #region Syncing With Server

                    command.Clear();
                    pseudoServerInsert.Clear();
                    addressInsert.Clear();
                    defaultCategories.Clear();
                    pseudoServerInsert.Clear();
                    insertedNestedEntities.Clear();
                    serverContentInsert.Clear();
                    insertBuilder.Clear();
                    offset = 0;
                    bool pseudoAppended = false;

                    command.Append("BEGIN;");
                    if ((scale & SynchronizationScale.RecoverEntities) != 0 || (scale & SynchronizationScale.UpdateEntities) != 0)
                    {
                        command.Append("DROP TABLE IF EXISTS pseudoServers;")
                                .Append("DROP TABLE IF EXISTS addresses;")
                                .Append("DROP TABLE IF EXISTS servers;")
                                .Append("DROP TABLE IF EXISTS serverContents;");

                        command.Append(localTableSQL);

                        entities = LoadRemoteServers(user, password, database, server, port, offset, 20, null);

                        do
                        {
                            for (int i = 0; i < entities.Count; i++)
                            {
                                pe = entities[i];

                                if (pe.type > EntityType.VirtualServer)
                                    insertedNestedEntities.Add(pe as PseudoWrappingEntity);

                                if (!pseudoAppended)
                                {
                                    pseudoServerInsert.Append("INSERT INTO pseudoServers ('ModificationDetector', 'CustomBackgroundColor', 'CustomBorderColor', 'CustomTextColor', 'ServerID') VALUES ");
                                    addressInsert.Append("INSERT INTO addresses ('Protocol', 'TCP', 'Address', 'Port', 'AdditionalCMDParameters', 'ServerID') VALUES ");
                                    insertBuilder.Append("INSERT INTO servers (rowid, FQDN, Category, Desc, Type) VALUES ");
                                    pseudoAppended = true;
                                }

                                insertBuilder.Append("(").Append(pe.server.rowID).Append(",'").Append(SecurityElement.Escape(pe.server.fqdn)).Append("','").Append(SecurityElement.Escape(pe.server.category)).Append("','").Append(SecurityElement.Escape(pe.server.desc)).Append("',").Append((int)pe.type).Append("),");
                                pseudoServerInsert.Append("('").Append((int)pe.modDetect).Append("', ").Append(pe.usesFill ? "'" + pe.customBackgroundColor + "'" : "NULL").Append(", ").Append(pe.usesBorder ? "'" + pe.customBorderColor + "'" : "NULL").Append(", ").Append(pe.usesText ? "'" + pe.customTextColor + "'" : "NULL").Append(",").Append(pe.server.rowID).Append("),");
                                for (int j = 0; j < pe.server.protocolAddresses.Count; j++)
                                {
                                    pa = pe.server.protocolAddresses[j];
                                    addressInsert.Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(", '").Append(SecurityElement.Escape(pa.hostname)).Append("', ").Append(pa.port).Append(", '").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(pe.server.rowID).Append("),");
                                }
                            }
                            offset += 20;
                        } while ((entities = LoadRemoteServers(user, password, database, server, port, offset, 20, null)).Count >= 20);

                    }

                    if ((scale & SynchronizationScale.RecoverDefaultColors) != 0)
                    {
                        using (MySqlConnection connection = EstablishMySQLDatabaseConnection(user, password, database, server, port))
                        {
                            if (connection != null)
                            {
                                connection.Open();
                                MySqlCommand cmd = connection.CreateCommand();
                                cmd.CommandText = "SELECT * FROM defaultCategories";
                                if (connection.State != System.Data.ConnectionState.Open)
                                    connection.Open();
                                MySqlDataReader reader = cmd.ExecuteReader();

                                if (reader.HasRows)
                                {
                                    defaultCategories.Append("DROP TABLE IF EXISTS defaultCategories;");
                                    defaultCategories.Append("CREATE TABLE IF NOT EXISTS defaultCategories (Category TEXT NOT NULL UNIQUE, CustomBackgroundColor TEXT, CustomBorderColor TEXT, CustomTextColor TEXT, PRIMARY KEY(Category)) WITHOUT ROWID;");
                                    defaultCategories.Append("INSERT INTO defaultCategories (Category, CustomBackgroundColor, CustomBorderColor, CustomTextColor) VALUES ");
                                    while (reader.Read())
                                        defaultCategories.Append("('").Append((string)reader["Category"]).Append("','").Append((string)reader["CustomBackgroundColor"]).Append("','").Append((string)reader["CustomBorderColor"]).Append("','").Append((string)reader["CustomTextColor"]).Append("'),");
                                }
                                reader.Close();
                            }
                        }
                    }

                    if (insertedNestedEntities.Count > 0)
                    {
                        bool insertPresent = false;
                        PseudoWrappingEntity pwe;
                        for (int i = 0; i < insertedNestedEntities.Count; i++)
                        {
                            pwe = insertedNestedEntities[i];

                            for (int j = 0; j < pwe.computersInCluster.Count; j++)
                            {
                                pe = pwe.computersInCluster[j];

                                if (!insertPresent)
                                {
                                    serverContentInsert.Append("INSERT INTO serverContents (`ServerID`, `ParentServerID`) VALUES ");
                                    insertPresent = true;
                                }

                                serverContentInsert.Append("(").Append(pe.server.rowID).Append(", ").Append(pwe.server.rowID).Append("),");
                            }

                        }
                    }

                    if (pseudoServerInsert.Length > 0)
                    {
                        pseudoServerInsert.Length--;
                        pseudoServerInsert.Append(";");
                    }
                    if (addressInsert.Length > 0)
                    {
                        addressInsert.Length--;
                        addressInsert.Append(";");
                    }

                    if (insertBuilder.Length > 0)
                    {
                        insertBuilder.Length--;
                        insertBuilder.Append(";");
                    }

                    command.Append(insertBuilder).Append(pseudoServerInsert).Append(addressInsert);

                    if (serverContentInsert.Length > 0)
                    {
                        serverContentInsert.Length--;
                        command.Append(serverContentInsert.Append(";"));
                    }

                    if (defaultCategories.Length > 0)
                    {
                        defaultCategories.Length--;
                        command.Append(defaultCategories.Append(";"));
                    }

                    command.Append("COMMIT;");

                    using (SQLiteConnection connection1 = EstablishSQLiteDatabaseConnection(GetPropertyValue("locallocation")))
                    {
                        if (connection1 != null)
                        {
                            SQLiteCommand cmd1 = new SQLiteCommand(command.ToString(), connection1);
                            connection1.Open();

                            cmd1.ExecuteNonQuery();
                        }
                    }
                    #endregion
                }
                catch (Exception e)
                {
                    //throw;
                    if (e is UnauthorizedAccessException)
                        ShowElevationDialog();
                    else
                        Log("[ERROR] Synchronization", e.ToString());
                }
                finally
                {
                    insertedNestedEntities.Clear();
                    savedIDToTempRowID.Clear();
                }
            }

            return unresolved;
        }

        public static async Task<Dictionary<PseudoEntity, PseudoEntity>> SynchronizeWithServerAsync(string user, string password, string database = "URLManagerDatabase", string server = null, int port = 3306, SynchronizationScale scale = SynchronizationScale.Full)
        {
            Dictionary<PseudoEntity, PseudoEntity> conflicts = await queue.Enqueue(() => Task.Run(() => SynchronizeWithServer(user, password, database, server, port, scale)));

            SetPropertyValue("lastsynchronization", "\"" + DateTime.Now.ToString() + "\"");
            SaveSettings();

            return conflicts;
        }

        public static void UpdateRemotePseudoEntity(string user, string password, string database = "URLManagerDatabase", string server = null, int port = 3306, PseudoEntity pe = null)
        {
            if (pe != null)
            {
                using (MySqlConnection connection = EstablishMySQLDatabaseConnection(user, password, database, server, port))
                {
                    if (connection != null)
                    {
                        StringBuilder updateBuilder = new StringBuilder();
                        updateBuilder.Append("UPDATE changeJournual SET `LastModified` = '").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:s")).Append("' WHERE `ServerID`=").Append(pe.server.rowID).Append(";");
                        updateBuilder.Append("UPDATE servers SET `FQDN` = '").Append(SecurityElement.Escape(pe.server.fqdn)).Append("', `Category` = '").Append(SecurityElement.Escape(pe.server.category)).Append("',`Desc`= '").Append(SecurityElement.Escape(pe.server.desc)).Append("', `Type` = ").Append((int)pe.type).Append(" WHERE `rowid` = ").Append(pe.server.rowID).Append(";");
                        updateBuilder.Append("UPDATE pseudoServers SET `ModificationDetector` = ").Append((int)ModificationDetector.Null).Append(", `CustomBackgroundColor` = ").Append(pe.usesFill ? "'" + pe.customBackgroundColor + "'" : "NULL").Append(", `CustomBorderColor` = ").Append(pe.usesBorder ? "'" + pe.customBorderColor + "'" : "NULL").Append(", `CustomTextColor` = ").Append(pe.usesText ? "'" + pe.customTextColor + "'" : "NULL").Append(" WHERE `ServerID` = ").Append(pe.server.rowID).Append(";");

                        updateBuilder.Append("DELETE FROM addresses WHERE `ServerID` = ").Append(pe.server.rowID).Append(";");
                        ProtocolAddress pa;
                        for (int j = 0; j < pe.server.protocolAddresses.Count; j++)
                        {
                            if (j == 0)
                                updateBuilder.Append("INSERT INTO addresses (`Protocol`, `TCP`, `Address`, `Port`, `AdditionalCMDParameters`, `ServerID`) VALUES ");
                            pa = pe.server.protocolAddresses[j];
                            updateBuilder.Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(",'").Append(SecurityElement.Escape(pa.hostname)).Append("',").Append(pa.port).Append(",'").Append(SecurityElement.Escape(pa.parameters)).Append("',").Append(pe.server.rowID).Append("),");
                        }

                        if (updateBuilder.Length > 0)
                        {
                            updateBuilder.Length--;
                            updateBuilder.Append(";");
                        }

                        if (pe.type > EntityType.VirtualServer)
                        {
                            updateBuilder.Append("DELETE FROM serverContents WHERE `ParentServerID` = ").Append(pe.server.rowID).Append(";");

                            PseudoWrappingEntity pwe = (PseudoWrappingEntity)pe;
                            bool insertPresent = false;
                            for (int j = 0; j < pwe.computersInCluster.Count; j++)
                            {
                                pe = pwe.computersInCluster[j];

                                if (!insertPresent)
                                {
                                    updateBuilder.Append("INSERT INTO serverContents (`ServerID`, `ParentServerID`) VALUES ");
                                    insertPresent = true;
                                }

                                updateBuilder.Append("(").Append(pe.server.rowID).Append(", ").Append(pwe.server.rowID).Append("),");
                            }

                            if (updateBuilder.Length > 0)
                            {
                                updateBuilder.Length--;
                                updateBuilder.Append(";");
                            }
                        }


                        MySqlCommand msc = connection.CreateCommand();
                        msc.CommandText = updateBuilder.ToString();
                        connection.Open();
                        msc.ExecuteNonQuery();
                    }
                }
            }
        }

        public static async Task UpdateRemotePseudoEntityAsync(string user, string password, string database = "URLManagerDatabase", string server = null, int port = 3306, PseudoEntity e = null)
        {
            if (e != null && !string.IsNullOrEmpty(database?.Trim()) && !string.IsNullOrEmpty(server?.Trim()))
                await queue.Enqueue(() => Task.Run(() => UpdateRemotePseudoEntity(user, password, database, server, port, e.DeepCopy())));
        }

        #region Extension Methods

        public static List<ProtocolAddress> DeepCopy(this List<ProtocolAddress> sourceList)
        {
            List<ProtocolAddress> lpa = new List<ProtocolAddress>();

            ProtocolAddress pa;
            foreach (ProtocolAddress p in sourceList)
            {
                pa = new ProtocolAddress(p.protocol, p.hostname, p.port);
                pa.parameters = p.parameters;
                pa.rowID = p.rowID;
                pa.isTCP = p.isTCP;
                lpa.Add(pa);
            }

            return lpa;
        }

        public static ProtocolAddress[] DeepCopy(this ProtocolAddress[] sourceList)
        {
            ProtocolAddress[] lpa = new ProtocolAddress[sourceList.Length];

            ProtocolAddress pa;
            int i = 0;
            foreach (ProtocolAddress p in sourceList)
            {
                pa = new ProtocolAddress(p.protocol, p.hostname, p.port);
                pa.parameters = p.parameters;
                pa.rowID = p.rowID;
                pa.isTCP = p.isTCP;
                lpa[i] = pa;
                i++;
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

        
        public static void AddRange<T>(this ObservableCollection<T> oc, List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
                oc.Add(list[i]);
        }
        #endregion
    }
}
