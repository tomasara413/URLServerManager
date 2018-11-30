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

    public class ProtocolAddress
    {
        public long rowID = -1;
        public string address { get; set; }
        public int port { get; set; }
        public string protocol { get; set; }
        public string parameters { get; set; }
        public Status status { get; set; }

        public ProtocolAddress()
        {
            status = Status.Untested;
        }

        public ProtocolAddress(string protocol, string address, int port) : this()
        {
            this.address = address;
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
            return a != null && address == a.address && port == a.port && protocol == a.protocol && parameters == a.parameters;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
