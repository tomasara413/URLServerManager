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
            Status status = (Status)value;
            if (status == Status.Untested)
                return Properties.Resources.Untested;

            if (status == 0)
                return Properties.Resources.Ok;

            StringBuilder sb = new StringBuilder();

            if ((status & Status.AddressUnreachable) != 0)
                sb.Append(Properties.Resources.AddressUnreachable).Append(Environment.NewLine);

            if ((status & Status.DNSEntryNotFound) != 0)
                sb.Append(Properties.Resources.DNSEntryNotFound).Append(Environment.NewLine);

            if ((status & Status.PortNotResponding) != 0)
                sb.Append(Properties.Resources.PortNotResponding);

            return sb.ToString().Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DetailsViewSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)values[1] ? ((double)values[0] / 4) : 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
