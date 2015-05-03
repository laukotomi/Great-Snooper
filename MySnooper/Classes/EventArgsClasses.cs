using System;
using System.Collections.Generic;

namespace MySnooper
{
    public class BoolEventArgs : EventArgs
    {
        public bool Argument { get; private set; }

        public BoolEventArgs(bool argument)
        {
            Argument = argument;
        }
    }

    public class GameHostedEventArgs : EventArgs
    {
        public string Parameters { get; private set; }
        public bool Arguments { get; private set; }

        public GameHostedEventArgs(string arguments, bool exitSnooper)
        {
            Parameters = arguments;
            Arguments = exitSnooper;
        }
    }

    public class ConnectionStateEventArgs : EventArgs
    {
        public IRCCommunicator.ConnectionStates State { get; private set; }

        public ConnectionStateEventArgs(IRCCommunicator.ConnectionStates state)
        {
            State = state;
        }
    }

    public class LookForTheseEventArgs : EventArgs
    {
        //Dictionary<string, string> leagues, bool spam
        public Dictionary<string, string> Leagues { get; private set; }
        public bool Spam { get; private set; }

        public LookForTheseEventArgs(Dictionary<string, string> leagues, bool spam)
        {
            Leagues = leagues;
            Spam = spam;
        }
    }

    public class StringEventArgs : EventArgs
    {
        public string Argument { get; private set; }

        public StringEventArgs(string argument)
        {
            Argument = argument;
        }
    }

    public class NotificatorEventArgs : EventArgs
    {
        public List<NotificatorClass> NotificatorList { get; private set; }

        public NotificatorEventArgs(List<NotificatorClass> notificatorList)
        {
            NotificatorList = notificatorList;
        }
    }

    public class SettingChangedEventArgs : EventArgs
    {
        //  string settingName, SettingChangedType type
        public string SettingName { get; private set; }
        public SettingChangedType Type { get; private set; }
        public object UserObject { get; private set; }

        public SettingChangedEventArgs(string settingName, SettingChangedType type, object userObject = null)
        {
            SettingName = settingName;
            Type = type;
            UserObject = userObject;
        }
    }
}
