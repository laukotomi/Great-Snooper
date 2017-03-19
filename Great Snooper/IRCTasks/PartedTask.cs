namespace GreatSnooper.IRCTasks
{
    using System;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.ViewModel;

    public class PartedTask : IRCTask
    {
        public PartedTask(IRCCommunicator server, string channelHash, string clientName, string message)
            : base(server)
        {
            this.ChannelHash = channelHash;
            this.ClientName = clientName;
            this.Message = (message == string.Empty)
                           ? Localizations.GSLocalization.Instance.PartMessage
                           : string.Format(Localizations.GSLocalization.Instance.PartMessage2, message);
        }

        public string ChannelHash
        {
            get;
            private set;
        }

        public string ClientName
        {
            get;
            private set;
        }

        public string Message
        {
            get;
            private set;
        }

        public override void DoTask(MainViewModel mvm)
        {
            AbstractChannelViewModel temp;
            if (!_server.Channels.TryGetValue(this.ChannelHash, out temp) || !temp.Joined)
            {
                return;
            }

            ChannelViewModel chvm = temp as ChannelViewModel;
            if (chvm == null)
            {
                return;
            }

            // This can reagate for force PART - this was the old way to PART a channel (was waiting for a PART message from the server as an answer for the PART command sent by the client)
            if (this.ClientName.Equals(_server.User.Name, StringComparison.OrdinalIgnoreCase))
            {
                chvm.LeaveChannelCommand.Execute(this.Message);
            }
            else
            {
                User u;
                if (_server.Users.TryGetValue(this.ClientName, out u))
                {
                    chvm.AddMessage(u, this.Message, MessageSettings.PartMessage);
                    chvm.RemoveUser(u);

                    if (u.ChannelCollection.Channels.Count == 0)
                    {
                        u.OnlineStatus = User.Status.Unknown;
                    }
                }
            }
        }
    }
}