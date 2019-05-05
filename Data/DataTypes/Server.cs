using System.Collections.Generic;
using System.ComponentModel;
using URLServerManagerModern.Utilities;

namespace URLServerManagerModern.Data.DataTypes
{
    public class Server : INotifyPropertyChanged
    {
        private List<ProtocolAddress> _protocolAddresses;
        public List<ProtocolAddress> protocolAddresses { get { return _protocolAddresses; } set { _protocolAddresses = value; OnPropertyChanged("protocolAddress"); } }

        public long rowID = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _fqdn, _category, _desc;
        public string fqdn { get { return _fqdn; } set { _fqdn = value; OnPropertyChanged("fqdn"); } } //fully qualified domain name
        public string category { get { return _category; } set { _category = value; OnPropertyChanged("category"); } }
        public string desc { get { return _desc; } set { _desc = value; OnPropertyChanged("desc"); } }

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

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
