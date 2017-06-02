using System;
using GreatSnooper.Helpers;

namespace GreatSnooper.UserCommands
{
    public class SetSettingCommand : UserCommand
    {
        public SetSettingCommand()
            : base("set")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            try
            {
                string settingName = text;
                string value = string.Empty;
                int spacePos = text.IndexOf(' ');
                if (spacePos != -1)
                {
                    settingName = text.Substring(0, spacePos).Trim();
                    value = text.Substring(spacePos + 1).Trim();
                }

                if (SettingsHelper.Exists(settingName))
                {
                    var type = SettingsHelper.Load(settingName).GetType();
                    SettingsHelper.Save(settingName, Convert.ChangeType(value, type));
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }
    }
}
