using System.Collections.Generic;
using System.ComponentModel;
using URLServerManagerModern.Utilities;

namespace URLServerManagerModern.Data.DataTypes
{
    public class Server
    {
        public List<ProtocolAddress> protocolAddresses { get; set; }

        public long rowID = -1;

        public string fqdn { get; set; } //fully qualified domain name
        public string category { get; set; }
        public string desc { get; set; }

        public Server()
        {
            protocolAddresses = new List<ProtocolAddress>();
        }

        public Server(string fqdn, string category, string desc) : this()
        {
            this.fqdn = fqdn;
            this.category = category;
            this.desc = desc;
        }

        public ProtocolAddress AddIP(string protocol, string ip, int port)
        {
            ProtocolAddress pa = new ProtocolAddress(protocol, ip, port);
            protocolAddresses.Add(pa);
            return pa;
        }

        public override bool Equals(object obj)
        {
            Server s = obj as Server;
            if (s != null)
            {
                if (fqdn == s.fqdn && category == s.category && desc == s.desc)
                {
                    if (protocolAddresses.Count != s.protocolAddresses.Count)
                        return false;

                    for (int i = 0; i < protocolAddresses.Count; i++)
                    {
                        if (!protocolAddresses[i].Equals(s.protocolAddresses[i]))
                            return false;
                    }
                    return true;
                }
                //Debug.WriteLine("se fail:" + (fqdn == s.fqdn && cathegory == s.cathegory && desc == s.desc));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public Server DeepCopy()
        {
            Server s = new Server(fqdn, category, desc);
            s.protocolAddresses = protocolAddresses.DeepCopy();
            s.rowID = rowID;
            return s;
        }
    }
}
