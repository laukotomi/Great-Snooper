namespace GreatSnooper.IRCTasks
{
    using System;
    using System.Collections.Generic;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.ViewModel;

    public class ChannelListTask : IRCTask
    {
        public ChannelListTask(IRCCommunicator server, SortedDictionary<string, string> channelList)
            : base(server)
        {
            this.ChannelList = channelList;
        }

        public SortedDictionary<string, string> ChannelList
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            foreach (var item in this.ChannelList)
            {
                var chvm = new ChannelViewModel(mvm, _server, item.Key, item.Value);

                if (GlobalManager.AutoJoinList.ContainsKey(item.Key))
                {
                    chvm.Password = GlobalManager.AutoJoinList[item.Key];
                    chvm.JoinCommand.Execute(null);
                }
            }

            // Join GameSurge channels automatically
            foreach (var item in GlobalManager.AutoJoinList)
            {
                if (this.ChannelList.ContainsKey(item.Key) == false && item.Key.StartsWith("#") && item.Key.Equals("#worms", StringComparison.OrdinalIgnoreCase) == false)
                {
                    var chvm = new ChannelViewModel(mvm, mvm.GameSurge, item.Key, string.Empty);
                    chvm.Password = GlobalManager.AutoJoinList[item.Key];
                    mvm.GameSurge.JoinChannelList.Add(item.Key);
                }
            }

            if (Properties.Settings.Default.ShowWormsChannel)
            {
                var worms = new ChannelViewModel(mvm, mvm.GameSurge, "#worms", "A place for hardcore wormers");
                if (GlobalManager.AutoJoinList.ContainsKey(worms.Name))
                {
                    mvm.GameSurge.JoinChannelList.Add(worms.Name);
                }
            }

            if (mvm.GameSurge.JoinChannelList.Count > 0)
            {
                mvm.GameSurge.Connect();
            }
        }
    }
}