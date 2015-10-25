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
                if (GlobalManager.HiddenChannels.Contains(item.Key))
                    continue;

                var chvm = new ChannelViewModel(mvm, this.Sender, item.Key, item.Value);

                if (GlobalManager.AutoJoinList.Contains(item.Key))
                    chvm.JoinCommand.Execute(null);
            }

            // Join GameSurge channels automatically
            foreach (string channel in GlobalManager.AutoJoinList)
            {
                if (ChannelList.ContainsKey(channel) == false && channel.StartsWith("#") && channel.Equals("#worms", StringComparison.OrdinalIgnoreCase) == false)
                {
                    new ChannelViewModel(mvm, mvm.GameSurge, channel, "");
                    mvm.GameSurge.JoinChannelList.Add(channel);
                }
            }

            if (Properties.Settings.Default.ShowWormsChannel)
            {
                var worms = new ChannelViewModel(mvm, mvm.GameSurge, "#worms", "A place for hardcore wormers");
                if (GlobalManager.AutoJoinList.Contains(worms.Name))
                    mvm.GameSurge.JoinChannelList.Add(worms.Name);
            }

            if (mvm.GameSurge.JoinChannelList.Count > 0)
                mvm.GameSurge.Connect();
        }
    }
}
