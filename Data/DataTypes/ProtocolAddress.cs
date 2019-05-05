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

        private bool _isTCP;
        public bool isTCP { get { return _isTCP; } set { _isTCP = value; OnPropertyChanged("isTCP"); } }
        public string hostname { get { return _hostname; } set { _hostname = value; OnPropertyChanged("hostname"); } }
        private int _port;
        public int port { get { return _port; } set { _port = value; OnPropertyChanged("port"); } }
        private string _protocol, _parameters, _hostname;
        public string protocol { get { return _protocol; } set { _protocol = value; OnPropertyChanged("protocols"); } }
        public string parameters { get { return _parameters; } set { _parameters= value; OnPropertyChanged("parameters"); } }
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
