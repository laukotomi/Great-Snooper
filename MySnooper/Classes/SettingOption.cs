using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MySnooper
{
    public class SettingOption
    {
        public enum SettingType { Bool, Sound, Text };
        private string Name;

        public SettingOption(Grid grid, string name, string text, SettingType type = SettingType.Bool)
        {
            Name = name;

            int row = grid.RowDefinitions.Count;
            object value = Properties.Settings.Default.GetType().GetProperty(Name).GetValue(Properties.Settings.Default, null);

            grid.RowDefinitions.Add(new RowDefinition() { Height = System.Windows.GridLength.Auto });

            // <Label Grid.Column="0" Grid.Row="1" Content="Auto login at startup:"></Label>
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            // <CheckBox Name="AutoLogin" Grid.Column="1" Grid.Row="1" HorizontalAlignment="Left" IsEnabled="False" Click="ShowLoginScreenChanged"></CheckBox>
            CheckBox cb = new CheckBox();
            cb.IsChecked = (bool)value;
            cb.Click += BoolHandler;
            Grid.SetRow(cb, row);
            Grid.SetColumn(cb, 1);
            grid.Children.Add(cb);
        }

        private void BoolHandler(object sender, System.Windows.RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb.IsChecked.HasValue)
            {
                Properties.Settings.Default.GetType().GetProperty(Name).SetValue(Properties.Settings.Default, cb.IsChecked.Value, null);
                Properties.Settings.Default.Save();
            }
        }
    }
}
