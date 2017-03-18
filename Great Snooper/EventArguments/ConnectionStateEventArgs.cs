namespace GreatSnooper.EventArguments
{
    using System;

    using GreatSnooper.IRC;

    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionStateEventArgs(IRCCommunicator.ConnectionStates state)
        {
            State = state;
        }

        public IRCCommunicator.ConnectionStates State
        {
            get;
            private set;
        }
    }
}