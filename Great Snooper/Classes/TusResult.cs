using GreatSnooper.Model;

namespace GreatSnooper.Classes
{
    class TusResult
    {
        #region Enums
        public enum TusStates { OK, TUSError, UserError, ConnectionError }
        #endregion

        #region Properties
        public TusStates TusState { get; private set; }
        public string Nickname { get; private set; }
        public string[] Rows { get; private set; }
        #endregion

        public TusResult(TusStates tusState, string nickname = null, string[] rows = null)
        {
            this.Nickname = nickname;
            this.TusState = tusState;
            this.Rows = rows;
        }
    }
}
