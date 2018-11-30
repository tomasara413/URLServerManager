namespace URLServerManagerModern.Data.DataTypes
{
    public class ProtocolArgumentAssociation
    {
        public string protocol { get; set; }
        public string cmdArguments { get; set; }

        public ProtocolArgumentAssociation(string p, string args)
        {
            protocol = p;
            cmdArguments = args;
        }

        //Normally we would want a deep copy, however there are no data types that are copied by reference
        public ProtocolArgumentAssociation Copy()
        {
            return (ProtocolArgumentAssociation)MemberwiseClone();
        }

        public bool Equals(ProtocolArgumentAssociation obj)
        {
            return obj.cmdArguments == cmdArguments && obj.protocol == protocol;
        }
    }
}
