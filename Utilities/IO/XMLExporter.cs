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
    internal class XMLExporter
    {
        XmlWriterSettings settings;
        internal XMLExporter()
        {
            settings = new XmlWriterSettings();
            settings.CloseOutput = true;
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.IndentChars = "\t";
            settings.NewLineChars = Environment.NewLine;
        }

        internal void Export(string filePath)
        {
            try
            {
                List<PseudoEntity> entities;
                PseudoEntity entity;

                using (XmlWriter xw = XmlWriter.Create(new FileStream(filePath, FileMode.Create), settings))
                {
                    xw.WriteStartDocument();
                    xw.WriteStartElement("URLManagerServerStructure");
                    xw.WriteStartElement("Servers");
                    int offset = 0;
                    do
                    {
                        entities = Utilities.LoadServers(offset, 20);
                        for (int i = 0; i < entities.Count; i++)
                        {
                            entity = entities[i];
                            xw.WriteStartElement(entity.type.ToString());
                            xw.WriteAttributeString("id", entity.server.rowID.ToString());
                            xw.WriteAttributeString("flag", entity.modDetect.ToString());

                            xw.WriteStartElement("FQDN");
                            xw.WriteValue(entity.server.fqdn);
                            xw.WriteEndElement();

                            xw.WriteStartElement("Category");
                            xw.WriteValue(entity.server.category);
                            xw.WriteEndElement();

                            xw.WriteStartElement("Description");
                            xw.WriteValue(entity.server.desc);
                            xw.WriteEndElement();

                            if (entity.usesFill)
                            {
                                xw.WriteStartElement("BackgroundColor");
                                xw.WriteValue(entity.customBackgroundColor);
                                xw.WriteEndElement();
                            }

                            if (entity.usesBorder)
                            {
                                xw.WriteStartElement("BorderColor");
                                xw.WriteValue(entity.customBorderColor);
                                xw.WriteEndElement();
                            }

                            if (entity.usesText)
                            {
                                xw.WriteStartElement("TextColor");
                                xw.WriteValue(entity.customTextColor);
                                xw.WriteEndElement();
                            }

                            xw.WriteStartElement("Addresses");
                            ProtocolAddress pa;
                            for (int j = 0; j < entity.server.protocolAddresses.Count; j++)
                            {
                                pa = entity.server.protocolAddresses[j];
                                xw.WriteStartElement("Address");
                                xw.WriteAttributeString("protocol", pa.protocol);
                                xw.WriteAttributeString("isTCP", pa.isTCP.ToString());
                                xw.WriteAttributeString("port", pa.port.ToString());

                                xw.WriteStartElement("EndPoint");
                                xw.WriteValue(pa.hostname);
                                xw.WriteEndElement();

                                xw.WriteStartElement("AdditionalCMDParameters");
                                xw.WriteValue(pa.parameters);
                                xw.WriteEndElement();

                                xw.WriteEndElement();
                            }
                            xw.WriteEndElement();

                            
                            PseudoWrappingEntity pwe;
                            if ((pwe = entity as PseudoWrappingEntity) != null)
                            {
                                xw.WriteStartElement("Servers");
                                for (int j = 0; j < pwe.computersInCluster.Count; j++)
                                {
                                    entity = pwe.computersInCluster[j];
                                    xw.WriteStartElement("Server");
                                    xw.WriteAttributeString("id", entity.server.rowID.ToString());
                                    xw.WriteEndElement();
                                }
                                xw.WriteEndElement();
                            }

                            xw.WriteEndElement();
                        }

                        offset += 20;
                    } while (entities.Count >= 20);

                    xw.WriteEndElement();

                    if (DataHolder.categoryColors.Count > 0)
                    {
                        xw.WriteStartElement("DefaultCategories");
                        foreach (KeyValuePair<string, CategoryColorAssociation> pair in DataHolder.categoryColors)
                        {
                            xw.WriteStartElement("CategoryColorAssociation");

                            xw.WriteStartElement("Category");
                            xw.WriteValue(pair.Value.category);
                            xw.WriteEndElement();

                            xw.WriteStartElement("BackgroundColor");
                            xw.WriteValue(pair.Value.fillColor);
                            xw.WriteEndElement();

                            xw.WriteStartElement("BorderColor");
                            xw.WriteValue(pair.Value.borderColor);
                            xw.WriteEndElement();

                            xw.WriteStartElement("TextColor");
                            xw.WriteValue(pair.Value.textColor);
                            xw.WriteEndElement();

                            xw.WriteEndElement();
                        }
                        xw.WriteEndElement();
                    }

                    xw.WriteEndElement();
                    xw.WriteEndDocument();
                }
            }
            catch (Exception e)
            {
                throw;
                if (e is UnauthorizedAccessException)
                    Utilities.ShowElevationDialog();
                else
                    Utilities.Log("[ERROR] Exporting to XML", e.ToString());
            }
        }
    }
}
