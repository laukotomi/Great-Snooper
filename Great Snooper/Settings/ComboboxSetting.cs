namespace GreatSnooper.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    class ComboboxSetting : AbstractSetting
    {
        private Action<object> selectionChanged;
        private object _selectedItem;

        public ComboboxSetting(string text, IEnumerable<object> items, object selectedItem, DataTemplate template, Action<object> selectionChanged)
            : base(string.Empty, text)
        {
            this.Items = items;
            this._selectedItem = selectedItem;
            this.Template = template;
            this.selectionChanged = selectionChanged;
        }

        public IEnumerable<object> Items
        {
            get;
            private set;
        }

        public object SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    this.selectionChanged(value);
                }
            }
        }

        public DataTemplate Template
        {
            get;
            private set;
        }
    }
}