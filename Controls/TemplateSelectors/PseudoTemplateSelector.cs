using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using URLServerManagerModern.Data.DataTypes.Pseudo;

namespace URLServerManagerModern.Controls.TemplateSelectors
{
    public class PseudoTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            while (!((container = VisualTreeHelper.GetParent(container)) is Window));

            if (item is PseudoServer)
                return (container as FrameworkElement).FindResource("pseudoServerTemplate") as DataTemplate;

            if (item is PseudoWrappingEntity)
                return (container as FrameworkElement).FindResource("pseudoWrappingEntityTemplate") as DataTemplate;

            return null;
        }
    }
}
