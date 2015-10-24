using GreatSnooper.Classes;
using System;

namespace GreatSnooper.EventArguments
{
    public class ConnectionStateEventArgs : EventArgs
    {
        public AbstractCommunicator.ConnectionStates State { get; private set; }

        public ConnectionStateEventArgs(AbstractCommunicator.ConnectionStates state)
        {
            State = state;
        }
    }
}
