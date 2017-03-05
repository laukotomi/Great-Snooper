namespace GreatSnooper.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;

    using GalaSoft.MvvmLight;

    using GreatSnooper.Helpers;
    using GreatSnooper.Model;

    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        private Regex groupListRegex = new Regex(@"^Group(\d+)List$", RegexOptions.Compiled);
        private Regex groupSoundRegex = new Regex(@"^Group(\d+)Sound$", RegexOptions.Compiled);

        private void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            Match m;
            m = groupSoundRegex.Match(e.PropertyName);
            if (m.Success)
            {
                UserGroups.Groups["Group" + m.Groups[1].Value].Sound = null;
                return;
            }

            m = groupListRegex.Match(e.PropertyName);
            if (m.Success)
            {
                string[] userList = SettingsHelper.Load<string>("Group" + m.Groups[1].Value + "List").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var group = UserGroups.Groups["Group" + m.Groups[1].Value];
                foreach (var user in group.Users.Except(userList))
                {
                    foreach (var server in this.Servers)
                    {
                        User u;
                        if (server.Users.TryGetValue(user, out u))
                        {
                            UserGroups.AddOrRemoveUser(u, null);
                            break;
                        }
                    }
                }

                foreach (string user in userList.Except(group.Users))
                {
                    foreach (var server in this.Servers)
                    {
                        User u;
                        if (server.Users.TryGetValue(user, out u))
                        {
                            UserGroups.AddOrRemoveUser(u, group);
                            break;
                        }
                    }
                }
                return;
            }

            switch (e.PropertyName)
            {
            case "Group0":
            case "Group1":
            case "Group2":
            case "Group3":
            case "Group4":
            case "Group5":
            case "Group6":
                var group = UserGroups.Groups[e.PropertyName];
                group.ReloadData();
                foreach (var server in this.Servers)
                {
                    foreach (var chvm in server.Channels)
                    {
                        if (chvm.Value is ChannelViewModel)
                        {
                            ((ChannelViewModel)chvm.Value).RegenerateGroupsMenu = true;
                            if (chvm.Value.Joined)
                            {
                                chvm.Value.LoadMessages(GlobalManager.MaxMessagesDisplayed, true);
                            }
                        }
                    }
                }
                break;

            case "CultureName":
                foreach (var server in this.Servers)
                {
                    foreach (var chvm in server.Channels)
                    {
                        if (chvm.Value is ChannelViewModel)
                        {
                            ((ChannelViewModel)chvm.Value).RegenerateGroupsMenu = true;
                        }
                    }
                }
                break;

            case "ShowWormsChannel":
                if (Properties.Settings.Default.ShowWormsChannel)
                {
                    new ChannelViewModel(this, this.GameSurge, "#worms", "A place for hardcore wormers");
                }
                else
                {
                    var chvm = (ChannelViewModel)this.GameSurge.Channels["#worms"];
                    this.CloseChannel(chvm);
                }
                break;

            case "WaExe":
                RaisePropertyChanged("ShowWAExe1");
                break;

            case "WaExe2":
                RaisePropertyChanged("ShowWAExe2");
                break;

            case "BatLogo":
                RaisePropertyChanged("BatLogo");
                break;

            case "HiddenChannels":
                GlobalManager.HiddenChannels = new HashSet<string>(
                    Properties.Settings.Default.HiddenChannels.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    GlobalManager.CIStringComparer);
                foreach (var server in this.Servers)
                {
                    foreach (var chvm in server.Channels)
                    {
                        if (this._allChannels.Any(x => x.Name.Equals(chvm.Key, StringComparison.OrdinalIgnoreCase)) == false && GlobalManager.HiddenChannels.Contains(chvm.Key) == false)
                        {
                            this._channelTabControl1.Channels.Add(chvm.Value);
                        }
                    }
                }
                break;

            case "PMBeep":
            case "HBeep":
            case "LeagueFoundBeep":
            case "LeagueFailBeep":
            case "NotificatorSound":
                Sounds.ReloadSound(e.PropertyName);
                break;

            case "ChannelMessageStyle":
            case "JoinMessageStyle":
            case "PartMessageStyle":
            case "QuitMessageStyle":
            case "SystemMessageStyle":
            case "ActionMessageStyle":
            case "UserMessageStyle":
            case "NoticeMessageStyle":
            case "MessageTimeStyle":
            case "HyperLinkStyle":
            case "LeagueFoundMessageStyle":
            case "ShowBannedMessages":
                for (int i = 0; i < this.Servers.Length; i++)
                {
                    foreach (var item in this.Servers[i].Channels)
                    {
                        if (item.Value.Joined)
                        {
                            item.Value.LoadMessages(GlobalManager.MaxMessagesDisplayed, true);
                        }
                    }
                }
                break;

            case "ShowBannedUsers":
                for (int i = 0; i < this.Servers.Length; i++)
                {
                    foreach (var item in this.Servers[i].Channels)
                    {
                        if (item.Value is ChannelViewModel && ((ChannelViewModel)item.Value).UserListDG != null)
                        {
                            ((ChannelViewModel)item.Value).UserListDG.SetUserListDGView();
                        }
                    }
                }
                break;

            case "ShowInfoColumn":
                for (int i = 0; i < this.Servers.Length; i++)
                {
                    foreach (var item in this.Servers[i].Channels)
                    {
                        if (item.Value is ChannelViewModel && ((ChannelViewModel)item.Value).UserListDG != null)
                        {
                            ((ChannelViewModel)item.Value).UserListDG.Columns[4].Visibility = (Properties.Settings.Default.ShowInfoColumn) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                        }
                    }
                }
                break;

            case "ItalicForGSUsers":
                for (int i = 0; i < this.Servers.Length; i++)
                {
                    foreach (var item in this.Servers[i].Users)
                    {
                        if (item.Value.UsingGreatSnooper && item.Value.Channels.Count > 0)
                        {
                            item.Value.RaisePropertyChangedPublic("UsingGreatSnooperItalic");
                        }
                    }
                }
                break;
            }
        }
    }
}