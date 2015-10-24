using System;
using System.ComponentModel;
using System.Windows;

namespace GreatSnooper.Helpers
{
    public static class ExtensionMethods
    {
        public static void AddValueChanged(this DependencyProperty property, object sourceObject, EventHandler handler)
        {
            var dpd = DependencyPropertyDescriptor.FromProperty(property, property.OwnerType);
            dpd.AddValueChanged(sourceObject, handler);
        }
    }
}
