using System;
using GreatSnooper.Helpers;

namespace GreatSnooper.UserCommands
{
    public class GetSettingCommand : UserCommand
    {
        public GetSettingCommand()
            : base("get")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            try
            {
                if (text.Length > 0)
                {
                    if (SettingsHelper.Exists(text))
                    {
                        sender.AddMessage(GlobalManager.SystemUser, SettingsHelper.Load(text).ToString(), MessageSettings.SystemMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }
    }
}
