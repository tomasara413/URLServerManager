using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace URLServerManager.Datatypes
{
    public class server
    {
        public List<protocolAddress> protocolAddresses { get; set; }
        
        public string fqdn { get; set; } //fully qualified domain name
        public string cathegory { get; set; }
        public string desc { get; set; }
        public bool selected = false;
        public Brush localDetect { get; set; }

        public server(string fqdn, string cathegory, string desc)
        {
            protocolAddresses = new List<protocolAddress>();
            this.fqdn = fqdn;
            this.cathegory = cathegory;
            this.desc = desc;         
        }

        public protocolAddress addIP(string protocol, string ip, int port)
        {
            protocolAddress pa = new protocolAddress(protocol, ip, port);
            protocolAddresses.Add(pa);
            return pa;
        }

        public List<protocolAddress> getAddresses()
        {
            List<protocolAddress> lpa = new List<protocolAddress>();

            foreach (protocolAddress p in protocolAddresses)
                lpa.Add(new protocolAddress(p.protocol,p.ip,p.port));

            return lpa;
        }

        public bool isEqualTo(server s)
        {
            if (fqdn == s.fqdn && cathegory == s.cathegory && desc == s.desc)
            {
                if (protocolAddresses.Count != s.protocolAddresses.Count)
                {
                    Debug.WriteLine("se fail 1:" + (protocolAddresses.Count != s.protocolAddresses.Count));
                    return false;
                }
                for (int i = 0; i < protocolAddresses.Count; i++)
                {
                    if (!protocolAddresses[i].isEqualTo(s.protocolAddresses[i]))
                    {
                        Debug.WriteLine("pa fail");
                        return false;
                    }
                }
                return true;
            }
            Debug.WriteLine("se fail:" + (fqdn == s.fqdn && cathegory == s.cathegory && desc == s.desc));
            return false;
        }
    }

    public class protocolAddress
    {
        //public IPEndPoint ip { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public string protocol { get; set; }
        public string parameters { get; set; }

        public protocolAddress(string p, string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            protocol = p;
        }

        public void setParameters(string parameters)
        {
            this.parameters = parameters;
        }

        public bool isEqualTo(protocolAddress a)
        {
            Debug.WriteLine("pa:" + ip + " " + a.ip + " port: " + port + " " + a.port + " prot: " + protocol + " " + a.protocol + " params: " + parameters + " " + a.parameters);
            return (ip == a.ip && port == a.port && protocol == a.protocol && parameters == a.parameters);
        }
    }

    public class property
    {
        public bool isGenerated = false;
        public string propertyName;
        public string propertyValue;

        public property(string name, string value)
        {
            propertyName = name;
            propertyValue = value;
        }
    }

    public class protocolProgramAssociation
    {
        public string protocol
        {
            get;
            set;
        }
        public string filePath
        {
            get;
            set;
        }
        public string cmdArguments
        {
            get;
            set;
        }

        public protocolProgramAssociation(string p, string path)
        {
            protocol = p;
            filePath = path;
        }
    }

    public class ResolveState
    {
        public ResolveState(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException(nameof(hostName));
            _hostName = hostName;
        }

        readonly string _hostName;

        public ResolveType Result { get; set; } = ResolveType.Pending;

        public string HostName => _hostName;

    }

    public enum ResolveType
    {
        Pending,
        Completed,
        InvalidHost,
        Timeout
    }
}
