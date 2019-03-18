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
    
    internal class CSVExporter
    {
        internal CSVExporter() { }


        internal void Export(string filePath)
        {
            try
            {
                List<PseudoEntity> entities;
                PseudoEntity entity;
                filePath = filePath.Substring(0, filePath.Length - 4);
                using (CSVWritter servers = new CSVWritter(filePath + "-servers.csv"))
                using (CSVWritter addresses = new CSVWritter(filePath + "-addresses.csv"))
                using (CSVWritter serverContents = new CSVWritter(filePath + "-serverContents.csv"))
                {
                    int offset = 0;
                    do
                    {
                        entities = Utilities.LoadServers(offset, 20);
                        for (int i = 0; i < entities.Count; i++)
                        {
                            entity = entities[i];

                            servers.WriteEntry(entity.server.rowID, entity.type, entity.server.fqdn, entity.server.category, entity.server.desc, entity.modDetect.ToString(), entity.usesFill ? entity.customBackgroundColor : null, entity.usesBorder ? entity.customBorderColor : null, entity.usesText ? entity.customTextColor : null);

                            ProtocolAddress pa;
                            for (int j = 0; j < entity.server.protocolAddresses.Count; j++)
                            {
                                pa = entity.server.protocolAddresses[j];

                                addresses.WriteEntry(pa.protocol, pa.isTCP, pa.hostname, pa.port, pa.parameters, entity.server.rowID);
                            }

                            PseudoWrappingEntity pwe;
                            if ((pwe = entity as PseudoWrappingEntity) != null)
                            {
                                for (int j = 0; j < pwe.computersInCluster.Count; j++)
                                {
                                    entity = pwe.computersInCluster[j];
                                    serverContents.WriteEntry(entity.server.rowID, pwe.server.rowID);
                                }
                            }
                        }

                        offset += 20;
                    } while (entities.Count >= 20);

                    if (DataHolder.categoryColors.Count > 0)
                    {
                        using (CSVWritter defaultCategories = new CSVWritter(filePath + "-defaultCategories.csv"))
                        {
                            foreach (KeyValuePair<string, CategoryColorAssociation> pair in DataHolder.categoryColors)
                                defaultCategories.WriteEntry(pair.Key, pair.Value.fillColor, pair.Value.borderColor, pair.Value.textColor);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //throw;
                if (e is UnauthorizedAccessException)
                    Utilities.ShowElevationDialog();
                else
                    Utilities.Log("[ERROR] Exporting to CSV", e.ToString());
            }
        }
    }  
}