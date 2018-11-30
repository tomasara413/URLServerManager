using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using URLServerManagerModern.Data.DataTypes.Pseudo;
using URLServerManagerModern.Utilities;

//TODO: Refactor to some more appropriate namespace
namespace URLServerManagerModern.Data.DataTypes
{
    public class ResolveState
    {
        readonly string _hostName;
        public string HostName => _hostName;

        public ResolveType Result { get; set; } = ResolveType.Pending;

        public ResolveState(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException(nameof(hostName));
            _hostName = hostName;
        }
    }

    public enum ResolveType
    {
        Pending,
        Completed,
        InvalidHost,
        Timeout
    }
}
