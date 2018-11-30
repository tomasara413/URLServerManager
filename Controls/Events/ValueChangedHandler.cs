using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace URLServerManagerModern.Controls.Events
{
    public delegate void ValueChangedHandler(object sender, ValueChangedEventArgs e);

    public class ValueChangedEventArgs : RoutedEventArgs
    {
        public ValueChangedEventArgs(RoutedEvent id, object OldValue, object NewValue) : base(id)
        {
            this.OldValue = OldValue;
            this.NewValue = NewValue;
        }

        public object OldValue { get; }
        public object NewValue { get; }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ValueChangedHandler handler = (ValueChangedHandler)genericHandler;
            handler(genericTarget, this);
        }
    }
}
