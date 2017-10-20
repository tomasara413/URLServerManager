using System.Collections.Generic;
using URLServerManager.Datatypes;

namespace URLServerManager
{
    public static class DataHolder
    {
        public static string configFile;
        public static string localFile;
        public static string remoteFile;

        public static int serverMaxId;

        public static Dictionary<string, int> protocolToPort = new Dictionary<string, int>();
        public static List<protocolProgramAssociation> protocolToProgram = new List<protocolProgramAssociation>();
        public static List<property> configProperties = new List<property>();

        public static void initializeBasicProtocolDictionary()
        {
            protocolToPort.Add("ssh", 22);
            protocolToPort.Add("telnet", 23);
            protocolToPort.Add("rdp", 3389);
            protocolToPort.Add("http", 80);
            protocolToPort.Add("https", 443);
        }

        public static void initializeBasicAssociationsList()
        {

        }

        public static void initializeConfigProperties()
        {
            configProperties.Add(new property("localfile", null));
            configProperties.Add(new property("remotefile", null));
            configProperties.Add(new property("associations", null));
        }
    }
}
