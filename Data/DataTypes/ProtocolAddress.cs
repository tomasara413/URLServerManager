using System.ComponentModel;

namespace URLServerManagerModern.Data.DataTypes
{
    public enum Status
    {
        Untested = -1,
        Ok = 0,
        DNSEntryNotFound = 1,
        AddressUnreachable = 2,
        PortNotResponding = 4
    }

    public class ProtocolAddress : INotifyPropertyChanged
    {
        public long rowID = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool isTCP { get; set; }
        public string hostname { get; set; }
        public int port { get; set; }
        public string protocol { get; set; }
        public string parameters { get; set; }
        private Status _status;
        public Status status { get { return _status; } set { _status = value; OnPropertyChanged("status"); } }

        public ProtocolAddress()
        {
            status = Status.Untested;
            isTCP = true;
        }

        public ProtocolAddress(string protocol, string hostname, int port) : this()
        {
            this.hostname = hostname;
            this.port = port;
            this.protocol = protocol;
        }

        public void SetParameters(string parameters)
        {
            this.parameters = parameters;
        }

        public override bool Equals(object obj)
        {
            ProtocolAddress a = obj as ProtocolAddress;
            return a != null && hostname == a.hostname && port == a.port && protocol == a.protocol && parameters == a.parameters && isTCP == a.isTCP;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
