using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using URLServerManagerModern.Data.DataTypes;

namespace URLServerManagerModern.Utilities
{
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((Status)value == Status.Untested)
                return Properties.Resources.Untested;

            if ((Status)value == 0)
                return Properties.Resources.Ok;

            StringBuilder sb = new StringBuilder();

            if (((Status)value & Status.AddressUnreachable) != 0)
                sb.Append(Properties.Resources.AddressUnreachable).Append(Environment.NewLine);

            if (((Status)value & Status.DNSEntryNotFound) != 0)
                sb.Append(Properties.Resources.DNSEntryNotFound).Append(Environment.NewLine);

            if (((Status)value & Status.PortNotResponding) != 0)
                sb.Append(Properties.Resources.PortNotResponding);

            return sb.ToString().Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
