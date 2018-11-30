using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using URLServerManagerModern.Data.DataTypes;
using URLServerManagerModern.Data.DataTypes.Pseudo;

namespace URLServerManagerModern.Data
{
    public static class DataHolder
    {
        public const int LIMIT = 20;

        public static string configFile;

        public static double fontSize
        {
            get
            {
                string s = Utilities.Utilities.GetPropertyValue("fontsize");
                double parsed;
                if (s != null && double.TryParse(s,out parsed))
                    return parsed * (4.0 / 3.0);
                else
                    return 8.5 * (4.0 / 3.0);
            }
            set
            {
                Utilities.Utilities.SetPropertyValue("fontsize", value.ToString());
            }
        }

        public static Dictionary<string, int> protocolToPort = new Dictionary<string, int>();
        public static List<Program> programs = new List<Program>();
        public static Dictionary<string, string> configProperties = new Dictionary<string, string>();
        public static Dictionary<string, CategoryColorAssociation> categoryColors = new Dictionary<string, CategoryColorAssociation>();
        public static int InitialProtocolToPortPoolSize { get { return initialProtcolToPortPool; } }
        private static int initialProtcolToPortPool;

        public static void InitializeBasicProtocolDictionary()
        {
            protocolToPort.Add("ftp", 21);
            protocolToPort.Add("ssh", 22);
            protocolToPort.Add("telnet", 23);
            protocolToPort.Add("rdp", 3389);
            protocolToPort.Add("http", 80);
            protocolToPort.Add("https", 443);
            initialProtcolToPortPool = protocolToPort.Count;
        }

        public static void InitializeBasicAssociationsList()
        {

        }

        public static void InitializeConfigProperties()
        {
            configProperties.Add("localfile", null);
            configProperties.Add("remotefile", null);
            configProperties.Add("fontsize", "8.5");
            configProperties.Add("showmodificationindicators", "false");
            configProperties.Add("allowportavailabilitydiagnostics", "true");
            configProperties.Add("portavailabilitytimeout", "30000");
            configProperties.Add("portavailabilityrenew", "200000");
            configProperties.Add("associations", null);
            configProperties.Add("portassociations", null);
            configProperties.Add("language", null);
        }
    }
}
