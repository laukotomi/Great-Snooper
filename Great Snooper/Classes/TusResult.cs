namespace GreatSnooper.Classes
{
    class TusResult
    {
        public TusResult(TusStates tusState, string nickname = null, string[] rows = null)
        {
            this.Nickname = nickname;
            this.TusState = tusState;
            this.Rows = rows;
        }

        public enum TusStates
        {
            OK, TUSError, UserError, ConnectionError
        }

        public string Nickname
        {
            get;
            private set;
        }

        public string[] Rows
        {
            get;
            private set;
        }

        public TusStates TusState
        {
            get;
            private set;
        }
    }
}