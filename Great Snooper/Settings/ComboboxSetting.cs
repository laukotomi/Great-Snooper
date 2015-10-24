using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace GreatSnooper.Settings
{
    class ComboboxSetting : AbstractSetting
    {
        #region Members
        private Action<object> selectionChanged;
        private object _selectedItem;
        #endregion

        #region Properties
        public DataTemplate Template { get; private set; }
        public IEnumerable<object> Items { get; private set; }
        public object SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    this.selectionChanged(value);
                }
            }
        }
        #endregion

        public ComboboxSetting(string text, IEnumerable<object> items, object selectedItem, DataTemplate template, Action<object> selectionChanged)
            : base("", text)
        {
            this.Items = items;
            this._selectedItem = selectedItem;
            this.Template = template;
            this.selectionChanged = selectionChanged;
        }
    }
}
