namespace GreatSnooper.EventArguments
{
    using System;

    using GreatSnooper.Classes;

    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionStateEventArgs(AbstractCommunicator.ConnectionStates state)
        {
            State = state;
        }

        public AbstractCommunicator.ConnectionStates State
        {
            get;
            private set;
        }
    }
}