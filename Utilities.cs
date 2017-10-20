using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using URLServerManager.Datatypes;
using System.IO;
using System.Net;

namespace URLServerManager
{
    public static class Utilities
    {
        public static string escapeQuotationSpaces(string original)
        {
            string returnString = "";

            bool those = false;

            foreach (char c in original)
            {
                if (c.Equals('"') && !those)
                {
                    those = true;
                }
                else if (c.Equals('"') && those)
                {
                    those = false;
                }

                if (those)
                {
                    returnString += c;
                }
                else
                {
                    if (!char.IsWhiteSpace(c))
                    {
                        if (c.Equals(','))
                            returnString += '|'; //this is not valid sign for dir name
                        else
                            returnString += c;
                    }
                }
            }
            return returnString;
        }

        public static string[] splitOnProperty(string original)
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
                {
                    returnString.Add(original.Substring(i, delimiters[i]));
                }
                returnString.Add(original.Substring(delimiters[i] + 1, (delimiters[i + 1] - delimiters[i]) - 1));
            }

            return returnString.ToArray();
        }

        public static string getPropertyValue(string propertyName)
        {
            foreach (property p in DataHolder.configProperties)
            {
                if (p.propertyName == propertyName)
                {
                    if (p.propertyValue == null)
                    {
                        Debug.WriteLine("DataHolder: " + p.propertyValue);
                        return "";
                    }
                    if (p.propertyValue.Contains("\""))
                        return p.propertyValue.Substring(1, p.propertyValue.Length - 2);
                    return p.propertyValue;
                }
            }
            return "Invalid property name.";
        }

        public static void setPropertyValue(string propertyName, string propertyValue)
        {
            foreach (property p in DataHolder.configProperties)
            {
                if (p.propertyName == propertyName)
                {
                    p.propertyValue = propertyValue;
                    break;
                }
            }
        }

        public static bool doesProtocolHaveAssociation(string protocol)
        {
            foreach (protocolProgramAssociation ppa in DataHolder.protocolToProgram)
            {
                if (ppa.protocol == protocol)
                {
                    return true;
                }
            }
            return false;
        }

        public static protocolProgramAssociation getAssociation(string protocol)
        {
            foreach (protocolProgramAssociation ppa in DataHolder.protocolToProgram)
            {
                if (ppa.protocol == protocol)
                {
                    return ppa;
                }
            }
            return null;
        }

        public static void saveSettings(MainWindow mw)
        {
            //load config and look after these properties
            try
            {
                List<string> properties = new List<string>(splitOnProperty(escapeQuotationSpaces(string.Join("", File.ReadAllLines(DataHolder.configFile)))));
                int count = properties.Count;
                foreach (property p in DataHolder.configProperties)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string[] pair = properties[i].Split(new char[] { '=' }, 2);
                        if (pair[0] == p.propertyName)
                        {
                            pair[1] = p.propertyValue;
                            Debug.WriteLine(pair[0] + "=" + pair[1]);
                            p.isGenerated = true;
                            properties[i] = pair[0] + "=" + pair[1];
                        }
                    }
                    if (!p.isGenerated)
                    {
                        properties.Add(p.propertyName + "=" + p.propertyValue);
                    }
                }

                StreamWriter writer = new StreamWriter(DataHolder.configFile, false, Encoding.UTF8);
                //Debug.WriteLine("writer: " + string.Join("," + Environment.NewLine, properties));
                writer.Write(string.Join("," + Environment.NewLine, properties));
                writer.Close();
                writer.Dispose();

                DataHolder.localFile = getPropertyValue("localfile");
                DataHolder.remoteFile = getPropertyValue("remotefile");

                mw.loadXML();
            }
            catch(Exception e)
            {
                log("[ERROR] saving config", e.ToString());
            }
        }

        public static void log (string errortype, string e)
        {
            string directory = Directory.GetCurrentDirectory() + "\\logs\\";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            StreamWriter sw = new StreamWriter(directory + "\\log-" + DateTime.Today.ToString("dd.MM.yyyy") + ".log", true, Encoding.UTF8);
            sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "::" + errortype +": " + e);
            sw.Close();
            sw.Dispose();
        }


        public static bool resolveHostname(string hostNameOrAddress, int millisecond_time_out)
        {
            ResolveState ioContext = new ResolveState(hostNameOrAddress);
            IAsyncResult result = Dns.BeginGetHostEntry(ioContext.HostName, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(millisecond_time_out), true);
            if (!success)
            {
                ioContext.Result = ResolveType.Timeout;
            }
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
    }
}
