using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;

namespace URLServerManagerModern.Utilities.IO
{
    internal abstract class Importer
    {
        internal abstract void Import(string filePath);

        protected Dictionary<long, long> savedIDToTempRowID = new Dictionary<long, long>();
        protected List<PseudoWrappingEntity> insertedNestedEntities = new List<PseudoWrappingEntity>();
        protected void AppendEntityToCommand(StringBuilder command, StringBuilder pseudoServerInsert, StringBuilder addressInsert, ref long tempRowID, PseudoEntity pe)
        {
            if (pe.type > EntityType.VirtualServer)
                insertedNestedEntities.Add(pe as PseudoWrappingEntity);

            if (tempRowID == 0)
            {
                pseudoServerInsert.Append("INSERT INTO pseudoServers ('ModificationDetector', 'CustomBackgroundColor', 'CustomBorderColor', 'CustomTextColor', 'ServerID') VALUES ");
                addressInsert.Append("INSERT INTO addresses ('Protocol', 'TCP', 'Address', 'Port', 'AdditionalCMDParameters', 'ServerID') VALUES ");
            }
            command.Append("INSERT INTO servers (FQDN, Category, Desc, Type) VALUES ").Append("('").Append(SecurityElement.Escape(pe.server.fqdn)).Append("','").Append(SecurityElement.Escape(pe.server.category)).Append("','").Append(SecurityElement.Escape(pe.server.desc)).Append("',").Append((int)pe.type).Append(");");
            command.Append("INSERT INTO rowids (tempRowID, realRowID) VALUES ").Append("(").Append(tempRowID).Append(",(SELECT last_insert_rowid()));");
            pseudoServerInsert.Append("('").Append((int)ModificationDetector.New).Append("', ").Append(pe.usesFill ? "'" + pe.customBackgroundColor + "'" : "NULL").Append(", ").Append(pe.usesBorder ? "'" + pe.customBorderColor + "'" : "NULL").Append(", ").Append(pe.usesText ? "'" + pe.customTextColor + "'" : "NULL").Append(", (SELECT realRowID FROM rowids WHERE tempRowID = ").Append(tempRowID).Append(" LIMIT 1)),");
            ProtocolAddress pa;
            for (int i = 0; i < pe.server.protocolAddresses.Count; i++)
            {
                pa = pe.server.protocolAddresses[i];
                addressInsert.Append("('").Append(SecurityElement.Escape(pa.protocol)).Append("',").Append(pa.isTCP ? "1" : "0").Append(", '").Append(SecurityElement.Escape(pa.hostname)).Append("', ").Append(pa.port).Append(", '").Append(SecurityElement.Escape(pa.parameters)).Append("', (SELECT realRowID FROM rowids WHERE tempRowID = ").Append(tempRowID).Append(" LIMIT 1)),");
            }

            savedIDToTempRowID.Add(pe.server.rowID, tempRowID);

            tempRowID++;
        }
    }
}
