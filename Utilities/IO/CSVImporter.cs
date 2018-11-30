using Newtonsoft.Json;
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
    public class CSVImporter
    {
        internal CSVImporter() { }

        Dictionary<long, long> savedIDToTempRowID = new Dictionary<long, long>();

        /**
         * <summary>
         * Imports csv into the database, requires multiple csvs (servers.csv (servers + pseudoServers), addresses.csv, severContents.csv and defaultCategories.csv)
         * </summary>
         **/
        public void Import(string serversFile, string addressesFile, string serverContentsFile, string defaultCategoriesFile)
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
                string[] entry;
                using (CSVReader cr1 = new CSVReader(serversFile))
                using (CSVReader cr2 = new CSVReader(addressesFile))
                {
                    command.Append("BEGIN;");
                    command.Append("PRAGMA temp_store = 2;");
                    command.Append("CREATE TEMP TABLE IF NOT EXISTS rowids (tempRowID INTEGER PRIMARY KEY NOT NULL, realRowID INTEGER NOT NULL) WITHOUT ROWID;");

                    long tempRowID = 0;
                    PseudoServer s;
                    while ((entry = cr1.ReadEntry()).Length > 0)
                    {
                        if ((s = ReadEntity(entry)) != null)
                        {
                            if (tempRowID == 0)
                            {
                                pseudoServerInsert.Append("INSERT INTO pseudoServers ('ModificationDetector', 'CustomBackgroundColor', 'CustomBorderColor', 'CustomTextColor', 'ServerID') VALUES ");
                                addressInsert.Append("INSERT INTO addresses ('Protocol', 'Address', 'Port', 'AdditionalCMDParameters', 'ServerID') VALUES ");
                            }
                            command.Append("INSERT INTO servers (FQDN, Category, Desc, Type) VALUES ").Append("('").Append(SecurityElement.Escape(s.server.fqdn)).Append("','").Append(SecurityElement.Escape(s.server.category)).Append("','").Append(SecurityElement.Escape(s.server.desc)).Append("',").Append((int)s.type).Append(");");
                            command.Append("INSERT INTO rowids (tempRowID, realRowID) VALUES ").Append("(").Append(tempRowID).Append(",(SELECT last_insert_rowid()));");
                            pseudoServerInsert.Append("('").Append(s.modDetect).Append("', ").Append(s.usesFill ? "'" + s.customBackgroundColor + "'" : "NULL").Append(", ").Append(s.usesBorder ? "'" + s.customBorderColor + "'" : "NULL").Append(", ").Append(s.usesText ? "'" + s.customTextColor + "'" : "NULL").Append(", (SELECT realRowID FROM rowids WHERE tempRowID = ").Append(tempRowID).Append(" LIMIT 1)),");

                            savedIDToTempRowID.Add(s.server.rowID, tempRowID);

                            tempRowID++;
                        }
                    }

                    ProtocolAddress pa;
                    while ((entry = cr2.ReadEntry()).Length > 0)
                    {
                        if ((pa = ReadAddress(entry)) != null)
                            addressInsert.Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("', '").Append(SecurityElement.Escape(pa.address)).Append("', ").Append(pa.port).Append(", '").Append(SecurityElement.Escape(pa.parameters)).Append("', (SELECT realRowID FROM rowids WHERE tempRowID = ").Append(savedIDToTempRowID[pa.rowID]).Append(" LIMIT 1)),");
                    }
                }

                if (File.Exists(serverContentsFile))
                {
                    using (CSVReader cr = new CSVReader(serverContentsFile))
                    {
                        while ((entry = cr.ReadEntry()).Length > 0)
                        {
                            if (serverContentInsert.Length == 0)
                                serverContentInsert.Append("INSERT INTO serverContents (ServerID, ParentServerID) VALUES ");

                            serverContentInsert.Append("((SELECT realRowID FROM rowids WHERE tempRowID = ").Append(savedIDToTempRowID[long.Parse(entry[0])]).Append(" LIMIT 1), (SELECT realRowID FROM rowids WHERE tempRowID = ").Append(savedIDToTempRowID[long.Parse(entry[1])]).Append(" LIMIT 1)),");
                        }
                    }
                }

                if (File.Exists(defaultCategoriesFile))
                {
                    using (CSVReader cr = new CSVReader(defaultCategoriesFile))
                    {
                        while ((entry = cr.ReadEntry()).Length > 0)
                        {
                            if (defaultCategories.Length == 0)
                                defaultCategories.Append("INSERT INTO defaultCategories (Category, CustomBackgroundColor, CustomBorderColor, CustomTextColor) VALUES ");

                            defaultCategories.Append("('").Append(SecurityElement.Escape(entry[0])).Append("','").Append(entry[1]).Append("','").Append(entry[2]).Append("','").Append(entry[3]).Append("'),");
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

                if (serverContentInsert.Length > 0)
                {
                    serverContentInsert.Length--;
                    command.Append(serverContentInsert.Append(";"));
                }

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
                    Utilities.Log("[ERROR] Importing from CSV", e.ToString());
            }
            finally
            {
                savedIDToTempRowID.Clear();
            }
        }

        /**
         * <summary>
         * Returns a psuedo server which is not a full representation of imported data
         * </summary>
         **/
        private PseudoServer ReadEntity(string[] entryData)
        {
            if (entryData.Length == 9)
            {
                PseudoServer s = new PseudoServer((ModificationDetector)Enum.Parse(typeof(ModificationDetector), entryData[5]), new Server(entryData[2], entryData[3], entryData[4]));
                s.server.rowID = long.Parse(entryData[0]);

                s.customBackgroundColor = entryData[6];
                s.customBorderColor = entryData[7];
                s.customTextColor = entryData[8];

                s.type = (EntityType)Enum.Parse(typeof(EntityType), entryData[1]);

                s.usesFill = !string.IsNullOrEmpty(s.customBackgroundColor?.Trim());
                s.usesBorder = !string.IsNullOrEmpty(s.customBorderColor?.Trim());
                s.usesText = !string.IsNullOrEmpty(s.customTextColor?.Trim());

                return s;
            }
            return null;
        }

        private ProtocolAddress ReadAddress(string[] entryData)
        {
            if (entryData.Length == 5)
            {
                ProtocolAddress a = new ProtocolAddress(entryData[0],entryData[1],int.Parse(entryData[2]));
                a.parameters = entryData[3];
                a.rowID = long.Parse(entryData[4]);

                return a;
            }
            return null;
        }
    }
}
