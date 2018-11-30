using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace URLServerManagerModern.Windows
{
    [Obsolete("No need for this", true)]
    internal interface ISubSettingsWindow
    {
        bool PendingCancelation { get; set; }
    }
}
