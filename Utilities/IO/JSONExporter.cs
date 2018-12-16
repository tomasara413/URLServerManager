using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using URLServerManagerModern.Data;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;

namespace URLServerManagerModern.Utilities.IO
{
    internal class JSONExporter
    {
        internal JSONExporter() { }

        internal void Export(string filePath)
        {
            try
            {
                List<PseudoEntity> entities;
                PseudoEntity entity;
                using (JsonTextWriter jw = new JsonTextWriter(new StreamWriter(new FileStream(filePath, FileMode.Create), Encoding.UTF8)))
                {
                    jw.CloseOutput = true;
                    jw.Formatting = Newtonsoft.Json.Formatting.Indented;
                    jw.Indentation = 1;
                    jw.IndentChar = '\t';
                    jw.WriteStartObject();
                    jw.WritePropertyName("Servers");
                    jw.WriteStartArray();
                    int offset = 0;
                    do
                    {
                        entities = Utilities.LoadServers(offset, 20);
                        for (int i = 0; i < entities.Count; i++)
                        {
                            entity = entities[i];
                            jw.WriteStartObject();
                            jw.WritePropertyName("Type");
                            jw.WriteValue(entity.type.ToString());

                            jw.WritePropertyName("id");
                            jw.WriteValue(entity.server.rowID.ToString());

                            jw.WritePropertyName("flag");
                            jw.WriteValue(entity.modDetect.ToString());


                            jw.WritePropertyName("FQDN");
                            jw.WriteValue(entity.server.fqdn);

                            jw.WritePropertyName("Category");
                            jw.WriteValue(entity.server.category);

                            jw.WritePropertyName("Description");
                            jw.WriteValue(entity.server.desc);

                            if (entity.usesFill)
                            {
                                jw.WritePropertyName("BackgroundColor");
                                jw.WriteValue(entity.customBackgroundColor);
                            }

                            if (entity.usesBorder)
                            {
                                jw.WritePropertyName("BorderColor");
                                jw.WriteValue(entity.customBorderColor);
                            }

                            if (entity.usesText)
                            {
                                jw.WritePropertyName("TextColor");
                                jw.WriteValue(entity.customTextColor);
                            }

                            jw.WritePropertyName("Addresses");
                            jw.WriteStartArray();
                            ProtocolAddress pa;
                            for (int j = 0; j < entity.server.protocolAddresses.Count; j++)
                            {
                                pa = entity.server.protocolAddresses[j];
                                jw.WriteStartObject();
                                jw.WritePropertyName("protocol");
                                jw.WriteValue(pa.protocol);
                                jw.WritePropertyName("port");
                                jw.WriteValue(pa.port);

                                jw.WritePropertyName("EndPoint");
                                jw.WriteValue(pa.hostname);

                                jw.WritePropertyName("AdditionalCMDParameters");
                                jw.WriteValue(pa.parameters);

                                jw.WriteEndObject();
                            }
                            jw.WriteEndArray();


                            PseudoWrappingEntity pwe;
                            if ((pwe = entity as PseudoWrappingEntity) != null)
                            {
                                jw.WritePropertyName("Servers");
                                jw.WriteStartArray();
                                for (int j = 0; j < pwe.computersInCluster.Count; j++)
                                {
                                    entity = pwe.computersInCluster[j];
                                    jw.WriteStartObject();
                                    jw.WritePropertyName("id");
                                    jw.WriteValue(entity.server.rowID.ToString());
                                    jw.WriteEndObject();
                                }
                                jw.WriteEndArray();
                            }

                            jw.WriteEndObject();
                        }

                        offset += 20;
                    } while (entities.Count >= 20);

                    jw.WriteEndArray();

                    if (DataHolder.categoryColors.Count > 0)
                    {
                        jw.WritePropertyName("DefaultCategories");
                        jw.WriteStartArray();
                        foreach (KeyValuePair<string, CategoryColorAssociation> pair in DataHolder.categoryColors)
                        {
                            jw.WriteStartObject();
                            jw.WritePropertyName("Category");
                            jw.WriteValue(pair.Value.category);
                            jw.WritePropertyName("BackgroundColor");
                            jw.WriteValue(pair.Value.fillColor);
                            jw.WritePropertyName("BorderColor");
                            jw.WriteValue(pair.Value.borderColor);
                            jw.WritePropertyName("TextColor");
                            jw.WriteValue(pair.Value.textColor);
                            jw.WriteEndObject();
                        }
                        jw.WriteEndArray();
                    }

                    jw.WriteEndObject();
                }
            }
            catch (Exception e)
            {
                throw;
                if (e is UnauthorizedAccessException)
                    Utilities.ShowElevationDialog();
                else
                    Utilities.Log("[ERROR] Exporting to JSON", e.ToString());
            }
        }
    }  
}