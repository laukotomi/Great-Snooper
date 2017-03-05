namespace GreatSnooper.Helpers
{
    using System;
    using System.ComponentModel;
    using System.Windows;

    public static class ExtensionMethods
    {
        public static void AddValueChanged(this DependencyProperty property, object sourceObject, EventHandler handler)
        {
            var dpd = DependencyPropertyDescriptor.FromProperty(property, property.OwnerType);
            dpd.AddValueChanged(sourceObject, handler);
        }
    }
}