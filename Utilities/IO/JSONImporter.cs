﻿using Newtonsoft.Json;
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
    internal class JSONImporter : Importer
    {
        internal JSONImporter() { }

        internal override void Import(string filePath)
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
                using (JsonTextReader jt = new JsonTextReader(new StreamReader(new FileStream(filePath, FileMode.Open), Encoding.UTF8)))
                {
                    jt.CloseInput = true;
                    PseudoEntity pe;
                    CategoryColorAssociation cca;

                    command.Append("BEGIN;");
                    command.Append("PRAGMA temp_store = 2;");
                    command.Append("CREATE TEMP TABLE IF NOT EXISTS rowids (tempRowID INTEGER PRIMARY KEY NOT NULL, realRowID INTEGER NOT NULL) WITHOUT ROWID;");

                    long tempRowID = 0;
                    bool defaultColors = false;
                    while (jt.Read())
                    {
                        if (jt.TokenType == JsonToken.PropertyName)
                            defaultColors = jt.Value?.ToString().ToLower() == "defaultcategories";
                        else if(defaultColors)
                            defaultColors = jt.Value?.ToString().ToLower() != "defaultcategories";

                        if (!defaultColors)
                        {
                            if ((pe = ReadEntity(jt)) != null)
                                AppendEntityToCommand(command, pseudoServerInsert, addressInsert, ref tempRowID, pe);
                        }
                        else
                        {
                            if ((cca = ReadAssociation(jt)) != null)
                            {
                                if (defaultCategories.Length == 0)
                                    defaultCategories.Append("INSERT OR REPLACE INTO defaultCategories (Category, CustomBackgroundColor, CustomBorderColor, CustomTextColor) VALUES ");

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

                            serverContentInsert.Append(",");
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

                command.Append(pseudoServerInsert).Append(addressInsert);

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


                using (SQLiteConnection connection = Utilities.EstablishSQLiteDatabaseConnection(Utilities.GetPropertyValue("locallocation")))
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
                //throw;
                if (e is UnauthorizedAccessException)
                    Utilities.ShowElevationDialog();
                else
                    Utilities.Log("[ERROR] Importing from JSON", e.ToString());
            }
            finally
            {
                insertedNestedEntities.Clear();
                savedIDToTempRowID.Clear();
            }
        }

        private PseudoEntity ReadEntity(JsonTextReader jt)
        {
            PseudoEntity pe = null;
            ProtocolAddress a = null;
            do
            {
                if (jt.TokenType == JsonToken.PropertyName)
                {
                    if (jt.Value?.ToString().ToLower() == "type")
                    {
                        switch (jt.ReadAsString()?.ToLower())
                        {
                            default:
                                pe = new PseudoServer(ModificationDetector.Null, new Server());
                                break;
                            case "virtualserver":
                                pe = new PseudoServer(ModificationDetector.Null, new Server());
                                pe.type = EntityType.VirtualServer;
                                break;
                            case "virtualizationserver":
                                pe = new PseudoWrappingEntity(ModificationDetector.Null, new Server());
                                pe.type = EntityType.VirtualizationServer;
                                break;
                            case "cluster":
                                pe = new PseudoWrappingEntity(ModificationDetector.Null, new Server());
                                pe.type = EntityType.Cluster;
                                break;
                        }
                    }
                    else if (pe != null)
                    {
                        switch (jt.Value?.ToString().ToLower())
                        {
                            case "id":
                                pe.server.rowID = long.Parse(jt.ReadAsString());
                                break;
                            case "flag":
                                string moddetect = jt.ReadAsString();
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
                                break;
                            case "fqdn":
                                pe.server.fqdn = jt.ReadAsString();
                                break;
                            case "category":
                                pe.server.category = jt.ReadAsString();
                                break;
                            case "description":
                                pe.server.desc = jt.ReadAsString();
                                break;
                            case "backgroundcolor":
                                pe.customBackgroundColor = jt.ReadAsString();
                                pe.usesFill = true;
                                break;
                            case "bordercolor":
                                pe.customBorderColor = jt.ReadAsString();
                                pe.usesBorder = true;
                                break;
                            case "textcolor":
                                pe.customTextColor = jt.ReadAsString();
                                pe.usesText = true;
                                break;
                            case "addresses":
                                while (jt.Read())
                                {
                                    if (jt.TokenType == JsonToken.StartObject)
                                        a = new ProtocolAddress();
                                    else if (jt.TokenType == JsonToken.PropertyName)
                                    {
                                        switch (jt.Value?.ToString().ToLower())
                                        {
                                            case "port":
                                                a.port = (int)jt.ReadAsInt32();
                                                break;
                                            case "istcp":
                                                a.isTCP = (bool)jt.ReadAsBoolean();
                                                break;
                                            case "protocol":
                                                a.protocol = jt.ReadAsString();
                                                break;
                                            case "endpoint":
                                                a.hostname = jt.ReadAsString();
                                                break;
                                            case "additionalcmdparameters":
                                                a.parameters = jt.ReadAsString();
                                                break;
                                        }
                                    }
                                    else if (jt.TokenType == JsonToken.EndObject)
                                    {
                                        if(a != null)
                                            pe.server.protocolAddresses.Add(a);
                                        a = null;
                                    }
                                    else if (jt.TokenType == JsonToken.EndArray)
                                        break;
                                }
                                break;
                            case "servers":
                                PseudoWrappingEntity pwe = pe as PseudoWrappingEntity;
                                while (jt.Read())
                                {
                                    if (jt.TokenType == JsonToken.PropertyName && jt.Value.ToString().ToLower() == "id" && pwe != null)
                                    {
                                        Server s = new Server();
                                        s.rowID = long.Parse(jt.ReadAsString());
                                        pwe.computersInCluster.Add(new PseudoServer(ModificationDetector.Null, s));
                                    }
                                    else if (jt.TokenType == JsonToken.EndArray)
                                        break;
                                }
                                break;
                        }
                    }
                }
                else if (jt.TokenType == JsonToken.EndObject || jt.TokenType == JsonToken.EndArray)
                    return pe;
            } while (jt.Read());
            return null;
        }

        private CategoryColorAssociation ReadAssociation(JsonTextReader jr)
        {
            string category = null, BCC = null, BC = null, TC = null;
            CategoryColorAssociation cca = null;

            do
            {
                if (jr.TokenType == JsonToken.PropertyName)
                {  
                    switch (jr.Value?.ToString().ToLower())
                    {
                        case "category":
                            category = jr.ReadAsString();
                            break;
                        case "backgroundcolor":
                            BCC = jr.ReadAsString();
                            break;
                        case "bordercolor":
                            BC = jr.ReadAsString();
                            break;
                        case "textcolor":
                            TC = jr.ReadAsString();
                            break;
                    }
                }
                else if (jr.TokenType == JsonToken.EndArray || jr.TokenType == JsonToken.EndObject)
                    break;
            } while (jr.Read());

            if (!string.IsNullOrEmpty(category?.Trim()))
                cca = new CategoryColorAssociation(category, BCC, BC, TC);

            return cca;
        }
    }
}
