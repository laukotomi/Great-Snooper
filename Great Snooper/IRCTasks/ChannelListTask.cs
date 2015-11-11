using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.ViewModel;
using System;
using System.Linq;
using System.Collections.Generic;

namespace GreatSnooper.IRCTasks
{
    public class ChannelListTask : IRCTask
    {
        public SortedDictionary<string, string> ChannelList { get; private set; }

        public ChannelListTask(AbstractCommunicator sender, SortedDictionary<string, string> channelList)
        {
            this.Sender = sender;
            this.ChannelList = channelList;
        }

        public override void DoTask(MainViewModel mvm)
        {
            foreach (var item in ChannelList)
            {
                var chvm = new ChannelViewModel(mvm, this.Sender, item.Key, item.Value);

                if (GlobalManager.AutoJoinList.ContainsKey(item.Key))
                {
                    chvm.Password = GlobalManager.AutoJoinList[item.Key];
                    chvm.JoinCommand.Execute(null);
                }
            }

            // Join GameSurge channels automatically
            foreach (var item in GlobalManager.AutoJoinList)
            {
                if (ChannelList.ContainsKey(item.Key) == false && item.Key.StartsWith("#") && item.Key.Equals("#worms", StringComparison.OrdinalIgnoreCase) == false)
                {
                    var chvm = new ChannelViewModel(mvm, mvm.GameSurge, item.Key, "");
                    chvm.Password = GlobalManager.AutoJoinList[item.Key];
                    mvm.GameSurge.JoinChannelList.Add(item.Key);
                }
            }

            if (Properties.Settings.Default.ShowWormsChannel)
            {
                var worms = new ChannelViewModel(mvm, mvm.GameSurge, "#worms", "A place for hardcore wormers");
                if (GlobalManager.AutoJoinList.ContainsKey(worms.Name))
                    mvm.GameSurge.JoinChannelList.Add(worms.Name);
            }

            if (mvm.GameSurge.JoinChannelList.Count > 0)
                mvm.GameSurge.Connect();
        }
    }
}
