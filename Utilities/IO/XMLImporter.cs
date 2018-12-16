using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;

namespace URLServerManagerModern.Utilities.IO
{
    internal class XMLImporter
    {
        XmlReaderSettings settings;
        internal XMLImporter()
        {
            settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            settings.CloseInput = true;
        }


        Dictionary<long, long> savedIDToTempRowID = new Dictionary<long, long>();
        List<PseudoWrappingEntity> insertedNestedEntities = new List<PseudoWrappingEntity>();

        internal void Import(string filePath)
        {
            StringBuilder command = new StringBuilder();
            //Inserting values in bulk cannot be separated while inserting servers and rowids if I want to use temporary insert table
            //StringBuilder serverInsert = new StringBuilder();
            //StringBuilder rowidInsert = new StringBuilder();
            StringBuilder defaultCategories = new StringBuilder();
            StringBuilder pseudoServerInsert = new StringBuilder();
            StringBuilder addressInsert = new StringBuilder();
            StringBuilder serverContentInsert = new StringBuilder();

            try
            {
                using (XmlReader xr = XmlReader.Create(new FileStream(filePath, FileMode.Open), settings))
                {
                    PseudoEntity pe;
                    CategoryColorAssociation cca;

                    command.Append("BEGIN;");
                    command.Append("PRAGMA temp_store = 2;");
                    command.Append("CREATE TEMP TABLE IF NOT EXISTS rowids (tempRowID INTEGER PRIMARY KEY NOT NULL, realRowID INTEGER NOT NULL) WITHOUT ROWID;");

                    xr.MoveToContent();
                    xr.Read();
                    long tempRowID = 0;
                    bool defaultColors = false;
                    while (xr.ReadState == ReadState.Interactive)
                    {
                        if (xr.IsStartElement() && !xr.IsEmptyElement)
                            defaultColors = xr.Name?.ToLower() == "defaultcategories";
                        else if (defaultColors)
                            defaultColors = xr.Name?.ToLower() != "defaultcategories";

                        if (!defaultColors)
                        {
                            if ((pe = ReadEntity(xr)) != null)
                            {
                                if (pe.type > EntityType.VirtualServer)
                                    insertedNestedEntities.Add(pe as PseudoWrappingEntity);

                                if (tempRowID == 0)
                                {
                                    pseudoServerInsert.Append("INSERT INTO pseudoServers ('ModificationDetector', 'CustomBackgroundColor', 'CustomBorderColor', 'CustomTextColor', 'ServerID') VALUES ");
                                    addressInsert.Append("INSERT INTO addresses ('Protocol', 'Address', 'Port', 'AdditionalCMDParameters', 'ServerID') VALUES ");
                                }
                                command.Append("INSERT INTO servers (FQDN, Category, Desc, Type) VALUES ").Append("('").Append(SecurityElement.Escape(pe.server.fqdn)).Append("','").Append(SecurityElement.Escape(pe.server.category)).Append("','").Append(SecurityElement.Escape(pe.server.desc)).Append("',").Append((int)pe.type).Append(");");
                                command.Append("INSERT INTO rowids (tempRowID, realRowID) VALUES ").Append("(").Append(tempRowID).Append(",(SELECT last_insert_rowid()));");
                                pseudoServerInsert.Append("('").Append(pe.modDetect).Append("', ").Append(pe.usesFill ? "'" + pe.customBackgroundColor + "'" : "NULL").Append(", ").Append(pe.usesBorder ? "'" + pe.customBorderColor + "'" : "NULL").Append(", ").Append(pe.usesText ? "'" + pe.customTextColor + "'" : "NULL").Append(", (SELECT realRowID FROM rowids WHERE tempRowID = ").Append(tempRowID).Append(" LIMIT 1)),");
                                ProtocolAddress pa;
                                for (int i = 0; i < pe.server.protocolAddresses.Count; i++)
                                {
                                    pa = pe.server.protocolAddresses[i];
                                    addressInsert.Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("', '").Append(SecurityElement.Escape(pa.hostname)).Append("', ").Append(pa.port).Append(", '").Append(SecurityElement.Escape(pa.parameters)).Append("', (SELECT realRowID FROM rowids WHERE tempRowID = ").Append(tempRowID).Append(" LIMIT 1)),");
                                }

                                savedIDToTempRowID.Add(pe.server.rowID, tempRowID);

                                tempRowID++;
                            }
                        }
                        else
                        {
                            if ((cca = ReadAssociation(xr)) != null)
                            {
                                if (defaultCategories.Length == 0)
                                    defaultCategories.Append("INSERT INTO defaultCategories (Category, CustomBackgroundColor, CustomBorderColor, CustomTextColor) VALUES ");

                                defaultCategories.Append("('").Append(SecurityElement.Escape(cca.category)).Append("','").Append(cca.fillColor).Append("','").Append(cca.borderColor).Append("','").Append(cca.textColor).Append("'),");
                            }

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
                        if (!insertPresent && pwe.computersInCluster.Count > 0)
                        {
                            insertPresent = true;
                            serverContentInsert.Append("INSERT INTO serverContents (ServerID, ParentServerID) VALUES ");
                        }

                        PseudoEntity pe1;
                        for (int j = 0; j < pwe.computersInCluster.Count; j++)
                        {
                            pe1 = pwe.computersInCluster[j];
                            serverContentInsert.Append("((SELECT realRowID FROM rowids WHERE tempRowID = ").Append(savedIDToTempRowID[pe1.server.rowID]).Append(" LIMIT 1), (SELECT realRowID FROM rowids WHERE tempRowID = ").Append(savedIDToTempRowID[pwe.server.rowID]).Append(" LIMIT 1))");

                            if (j < pwe.computersInCluster.Count - 1 || i < insertedNestedEntities.Count - 1)
                                serverContentInsert.Append(",");
                            else
                                serverContentInsert.Append(";");
                        }
                    }
                }

                if (pseudoServerInsert.Length > 0)
                    pseudoServerInsert.Length--;
                if (addressInsert.Length > 0)
                    addressInsert.Length--;

                command.Append(pseudoServerInsert.Append(";")).Append(addressInsert.Append(";"));

                if (defaultCategories.Length > 0)
                {
                    defaultCategories.Length--;
                    command.Append(defaultCategories.Append(";"));
                }

                command.Append(serverContentInsert);

                command.Append("DROP TABLE rowids;");
                command.Append("COMMIT;");


                using (SQLiteConnection connection = Utilities.EstablishDatabaseConnection(Utilities.GetPropertyValue("localfile")))
                {
                    if (connection != null)
                    {
                        SQLiteCommand cmd = new SQLiteCommand(command.ToString(), connection);
                        connection.Open();

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                throw;
                if (e is UnauthorizedAccessException)
                    Utilities.ShowElevationDialog();
                else
                    Utilities.Log("[ERROR] Importing from XML", e.ToString());
            }
            finally
            {
                insertedNestedEntities.Clear();
                savedIDToTempRowID.Clear();
            }
        }

        private PseudoEntity ReadEntity(XmlReader xr)
        {
            PseudoEntity pe = null;
            ProtocolAddress a;
            if (xr.IsStartElement())
            {
                switch (xr.Name?.ToLower())
                {
                    case "server":
                        pe = new PseudoServer(ModificationDetector.Null, new Server());
                        goto case "loadEntity";
                    case "virtualserver":
                        pe = new PseudoServer(ModificationDetector.Null, new Server());
                        pe.type = EntityType.VirtualServer;
                        goto case "loadEntity";
                    case "virtualizationserver":
                        pe = new PseudoWrappingEntity(ModificationDetector.Null, new Server());
                        pe.type = EntityType.VirtualizationServer;
                        goto case "loadEntity";
                    case "cluster":
                        pe = new PseudoWrappingEntity(ModificationDetector.Null, new Server());
                        pe.type = EntityType.Cluster;
                        goto case "loadEntity";
                    //no need to check for null pe since xr name can only be lowercase
                    case "loadEntity":
                        if (xr.HasAttributes)
                        {
                            pe.server.rowID = long.Parse(xr.GetAttribute("id"));
                            string moddetect = xr.GetAttribute("flag");
                            if (moddetect != null)
                                moddetect = moddetect.ToLower();
                            if (moddetect == "add")
                                pe.modDetect = ModificationDetector.New;
                            else if (moddetect == "edit")
                                pe.modDetect = ModificationDetector.Modified;
                            else if (moddetect == "rem")
                                pe.modDetect = ModificationDetector.Removed;
                            else
                                pe.modDetect = ModificationDetector.Null;
                        }
                        else
                            pe.modDetect = ModificationDetector.Null;
                        xr.Read();
                        while (xr.ReadState == ReadState.Interactive)
                        {
                            if (xr.IsStartElement() && !xr.IsEmptyElement)
                            {
                                switch (xr.Name?.ToLower())
                                {
                                    case "fqdn":
                                        pe.server.fqdn = xr.ReadElementContentAsString();
                                        break;
                                    case "category":
                                        pe.server.category = xr.ReadElementContentAsString();
                                        break;
                                    case "description":
                                        pe.server.desc = xr.ReadElementContentAsString();
                                        break;
                                    case "backgroundcolor":
                                        pe.customBackgroundColor = xr.ReadElementContentAsString();
                                        pe.usesFill = true;
                                        break;
                                    case "bordercolor":
                                        pe.customBorderColor = xr.ReadElementContentAsString();
                                        pe.usesBorder = true;
                                        break;
                                    case "textcolor":
                                        pe.customTextColor = xr.ReadElementContentAsString();
                                        pe.usesText = true;
                                        break;
                                    case "addresses":
                                        xr.Read();
                                        while (xr.ReadState == ReadState.Interactive)
                                        {
                                            if (xr.IsStartElement() && !xr.IsEmptyElement)
                                            {
                                                switch (xr.Name?.ToLower())
                                                {
                                                    case "address":
                                                        a = new ProtocolAddress();
                                                        int port;
                                                        if (int.TryParse(xr.GetAttribute("port"), out port))
                                                            a.port = port;
                                                        a.protocol = xr.GetAttribute("protocol");
                                                        xr.Read();
                                                        while (xr.ReadState == ReadState.Interactive)
                                                        {
                                                            if (xr.IsStartElement())
                                                            {
                                                                switch (xr.Name?.ToLower())
                                                                {
                                                                    //legacy convert
                                                                    case "ipv4endpoint":
                                                                    case "ipendpoint":
                                                                        string[] ip = xr.ReadElementContentAsString().Split(':');
                                                                        a.hostname = ip[0];
                                                                        a.port = int.Parse(ip[1]);
                                                                        break;
                                                                    case "ipv6endpoint":
                                                                        ip = xr.ReadElementContentAsString().Split(']');
                                                                        a.hostname = ip[0];
                                                                        a.port = int.Parse(ip[1]);
                                                                        break;
                                                                    //new
                                                                    case "endpoint":
                                                                        a.hostname = xr.ReadElementContentAsString();
                                                                        break;
                                                                    case "additionalcmdparameters":
                                                                        a.parameters = xr.ReadElementContentAsString();
                                                                        break;
                                                                    default:
                                                                        xr.Read();
                                                                        break;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (xr.Name?.ToLower() == "address")
                                                                {
                                                                    pe.server.protocolAddresses.Add(a);
                                                                    xr.Read();
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        xr.Read();
                                                        break;
                                                }
                                            }
                                            else if (xr.Name?.ToLower() == "addresses")
                                            {
                                                xr.Read();
                                                break;
                                            }
                                            else
                                                xr.Read();
                                        }
                                        break;
                                    case "servers":
                                        xr.Read();
                                        while (xr.ReadState == ReadState.Interactive)
                                        {
                                            if (xr.IsStartElement())
                                            {
                                                switch (xr.Name?.ToLower())
                                                {
                                                    case "server":
                                                        PseudoWrappingEntity pwe = pe as PseudoWrappingEntity;
                                                        if (xr.HasAttributes && pwe != null)
                                                        {
                                                            Server s = new Server();
                                                            s.rowID = long.Parse(xr.GetAttribute("id"));
                                                            pwe.computersInCluster.Add(new PseudoServer(ModificationDetector.Null, s));
                                                        }
                                                        goto default;
                                                    default:
                                                        xr.Read();
                                                        break;
                                                }
                                            }
                                            else if (xr.Name?.ToLower() == "servers")
                                            {
                                                xr.Read();
                                                break;
                                            }
                                            else
                                                xr.Read();
                                        }
                                        break;
                                    default:
                                        xr.Read();
                                        break;
                                }
                            }
                            else
                            {

                                switch (xr.Name?.ToLower())
                                {
                                    case "server":
                                    case "virtualserver":
                                    case "virtualizationserver":
                                    case "cluster":
                                        xr.Read();
                                        return pe;
                                    default:
                                        xr.Read();
                                        break;
                                }

                                
                            }
                        }
                        break;
                    default:
                        xr.Read();
                        break;
                }
            }
            else
                xr.Read();

            return null;
        }

        private CategoryColorAssociation ReadAssociation(XmlReader xr)
        {
            string category = null, BCC = null, BC = null, TC = null;
            CategoryColorAssociation cca = null;

            xr.Read();
            if (xr.IsStartElement())
            {
                switch (xr.Name?.ToLower())
                {
                    default:
                        xr.Read();
                        break;
                    case "categorycolorassociation":
                        xr.Read();
                        while (xr.ReadState == ReadState.Interactive)
                        {
                            if (xr.IsStartElement() && !xr.IsEmptyElement)
                            {
                                switch (xr.Name?.ToLower())
                                {
                                    case "category":
                                        category = xr.ReadElementContentAsString();
                                        break;
                                    case "backgroundcolor":
                                        BCC = xr.ReadElementContentAsString();
                                        break;
                                    case "bordercolor":
                                        BC = xr.ReadElementContentAsString();
                                        break;
                                    case "textcolor":
                                        TC = xr.ReadElementContentAsString();
                                        break;
                                }
                            }
                            else if (xr.Name?.ToLower() == "categorycolorassociation")
                            {
                                xr.Read();
                                break;
                            }
                        }
                        break;
                }
            }


            if (!string.IsNullOrEmpty(category?.Trim()))
                cca = new CategoryColorAssociation(category, BCC, BC, TC);
            return cca;
        }
    }
}
