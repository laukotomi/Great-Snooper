using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace GreatSnooper.Localizations
{
    public class GSLocalization : INotifyPropertyChanged
    {
        #region Singleton and PropertyChanged
        private static GSLocalization instance;
        public static GSLocalization Instance
        {
            get
            {
                if (instance == null)
                    instance = new GSLocalization();
                return instance;
            }
        }

        private GSLocalization() { }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void CultureChanged()
        {
            var props = this.GetType().GetProperties().Where(x => x.PropertyType == typeof(string));
            foreach (var prop in props)
                RaisePropertyChanged(prop.Name);
        }
        #endregion

        #region ResourceManager
        private readonly ResourceManager _rm = new ResourceManager("GreatSnooper.Localizations.Localization", Assembly.GetExecutingAssembly());
        public ResourceManager RM { get { return _rm; } }
        #endregion


        /// <summary>
        ///   Looks up a localized string similar to About.
        /// </summary>
        public string AboutText
        {
            get
            {
                return _rm.GetString("AboutText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to About WormNat 2: If you host with the game, then it will create the server on your computer and it will wait for players to join on a specified port (usually 17011). But most of the routers and firewall programs don&apos;t allow incoming connections, so nobody can join your game. You will need to enable port forwarding on your router and/or configure your firewall if you use one in order to let players join your game. WormNat2 uses a technique, that you can host games without incoming connections so your host wi [rest of string was truncated]&quot;;.
        /// </summary>
        public string AboutWormNat2Text
        {
            get
            {
                return _rm.GetString("AboutWormNat2Text");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Action message using &apos;&gt;&apos; character:.
        /// </summary>
        public string ActionMessageGTText
        {
            get
            {
                return _rm.GetString("ActionMessageGTText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Action message style:.
        /// </summary>
        public string ActionMessageStyleText
        {
            get
            {
                return _rm.GetString("ActionMessageStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Add to ignore list.
        /// </summary>
        public string AddIgnoreText
        {
            get
            {
                return _rm.GetString("AddIgnoreText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Add to conversation.
        /// </summary>
        public string AddToConvText
        {
            get
            {
                return _rm.GetString("AddToConvText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Add a new user to the list...
        /// </summary>
        public string AddUserToList
        {
            get
            {
                return _rm.GetString("AddUserToList");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Ask if I would like to turn off the league searcher when I host or join a game:.
        /// </summary>
        public string AskLeagueSOffText
        {
            get
            {
                return _rm.GetString("AskLeagueSOffText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Ask if I would like to turn off the notificator when I host or join a game:.
        /// </summary>
        public string AskNotificatorOffText
        {
            get
            {
                return _rm.GetString("AskNotificatorOffText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Auto join this channel.
        /// </summary>
        public string AutoJoinText
        {
            get
            {
                return _rm.GetString("AutoJoinText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Auto login at startup:.
        /// </summary>
        public string AutoLogInSettingText
        {
            get
            {
                return _rm.GetString("AutoLogInSettingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Auto login:.
        /// </summary>
        public string AutoLoginText
        {
            get
            {
                return _rm.GetString("AutoLoginText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Set away.
        /// </summary>
        public string AwayButtonAway
        {
            get
            {
                return _rm.GetString("AwayButtonAway");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Set back.
        /// </summary>
        public string AwayButtonBack
        {
            get
            {
                return _rm.GetString("AwayButtonBack");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Away text:.
        /// </summary>
        public string AwayLabel
        {
            get
            {
                return _rm.GetString("AwayLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Away Manager.
        /// </summary>
        public string AwayManagerTitle
        {
            get
            {
                return _rm.GetString("AwayManagerTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Away manager.
        /// </summary>
        public string AwayManagerTooltip
        {
            get
            {
                return _rm.GetString("AwayManagerTooltip");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to You are away: {0}.
        /// </summary>
        public string AwayManagerTooltipAway
        {
            get
            {
                return _rm.GetString("AwayManagerTooltipAway");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to is away: {0}.
        /// </summary>
        public string AwayMessageFormat
        {
            get
            {
                return _rm.GetString("AwayMessageFormat");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Away message:.
        /// </summary>
        public string AwayMessageText
        {
            get
            {
                return _rm.GetString("AwayMessageText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Back message:.
        /// </summary>
        public string BackMessageText
        {
            get
            {
                return _rm.GetString("BackMessageText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Bold:.
        /// </summary>
        public string BoldLabel
        {
            get
            {
                return _rm.GetString("BoldLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Browse.
        /// </summary>
        public string BrowseButtonContent
        {
            get
            {
                return _rm.GetString("BrowseButtonContent");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Friends.
        /// </summary>
        public string BuddiesGroupText
        {
            get
            {
                return _rm.GetString("BuddiesGroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Change.
        /// </summary>
        public string ChangeButtonText
        {
            get
            {
                return _rm.GetString("ChangeButtonText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Set #worms nick to WormNet nick at startup:.
        /// </summary>
        public string ChangeWormsNickText
        {
            get
            {
                return _rm.GetString("ChangeWormsNickText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Style of other users&apos; messages:.
        /// </summary>
        public string ChannelMessageStyleText
        {
            get
            {
                return _rm.GetString("ChannelMessageStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to You must join a channel first!.
        /// </summary>
        public string ChannelOfflineText
        {
            get
            {
                return _rm.GetString("ChannelOfflineText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Chat mode on / off.
        /// </summary>
        public string ChatModeTooltip
        {
            get
            {
                return _rm.GetString("ChatModeTooltip");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Chat with this user.
        /// </summary>
        public string ChatText
        {
            get
            {
                return _rm.GetString("ChatText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Your clan can contain only characters from the English alphabet and numbers..
        /// </summary>
        public string ClanHasBadChar
        {
            get
            {
                return _rm.GetString("ClanHasBadChar");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Clan.
        /// </summary>
        public string ClanHeaderLabel
        {
            get
            {
                return _rm.GetString("ClanHeaderLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Optional. If you do not have clan you can leave this empty..
        /// </summary>
        public string ClanInfoText
        {
            get
            {
                return _rm.GetString("ClanInfoText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Clanmates.
        /// </summary>
        public string ClanMatesGroupText
        {
            get
            {
                return _rm.GetString("ClanMatesGroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Clan:.
        /// </summary>
        public string ClanText
        {
            get
            {
                return _rm.GetString("ClanText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Close.
        /// </summary>
        public string CloseChatText
        {
            get
            {
                return _rm.GetString("CloseChatText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Close.
        /// </summary>
        public string CloseText
        {
            get
            {
                return _rm.GetString("CloseText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Exit button minimizes the snooper to tray:.
        /// </summary>
        public string CloseToTrayText
        {
            get
            {
                return _rm.GetString("CloseToTrayText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Failed to load the common settings! You won&apos;t be allowed to spam for league games (but you can still look for them). If this problem doesn&apos;t go away then the snooper may need to be updated..
        /// </summary>
        public string CommonSettingFailText
        {
            get
            {
                return _rm.GetString("CommonSettingFailText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Connecting.
        /// </summary>
        public string ConnectingText
        {
            get
            {
                return _rm.GetString("ConnectingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to You can contact me at http://www.tus-wa.com/profile/Tomi/ using private message.
        /// </summary>
        public string ContactText
        {
            get
            {
                return _rm.GetString("ContactText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} has added {1} to the conversation..
        /// </summary>
        public string ConversationAdded
        {
            get
            {
                return _rm.GetString("ConversationAdded");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to You have been removed from this conversation..
        /// </summary>
        public string ConversationKick
        {
            get
            {
                return _rm.GetString("ConversationKick");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} has left the conversation..
        /// </summary>
        public string ConversationLeave
        {
            get
            {
                return _rm.GetString("ConversationLeave");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} has removed {1} from the conversation..
        /// </summary>
        public string ConversationRemoved
        {
            get
            {
                return _rm.GetString("ConversationRemoved");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to GB.
        /// </summary>
        public string CountryCode
        {
            get
            {
                return _rm.GetString("CountryCode");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to C..
        /// </summary>
        public string CountryHeaderLabel
        {
            get
            {
                return _rm.GetString("CountryHeaderLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Country:.
        /// </summary>
        public string CountryLabel
        {
            get
            {
                return _rm.GetString("CountryLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Create game.
        /// </summary>
        public string CreateGameText
        {
            get
            {
                return _rm.GetString("CreateGameText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to en-GB.
        /// </summary>
        public string CultureName
        {
            get
            {
                return _rm.GetString("CultureName");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Default.
        /// </summary>
        public string DefaultText
        {
            get
            {
                return _rm.GetString("DefaultText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Delete channel logs older than 30 days at startup:.
        /// </summary>
        public string DeleteLogsText
        {
            get
            {
                return _rm.GetString("DeleteLogsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Edit.
        /// </summary>
        public string EditText
        {
            get
            {
                return _rm.GetString("EditText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to This text cannot be empty!.
        /// </summary>
        public string EmptyErrorMessage
        {
            get
            {
                return _rm.GetString("EmptyErrorMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Enabled.
        /// </summary>
        public string EnabledText
        {
            get
            {
                return _rm.GetString("EnabledText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Energy save mode while playing:.
        /// </summary>
        public string EnergySaveModeGameText
        {
            get
            {
                return _rm.GetString("EnergySaveModeGameText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Energy save mode when snooper is hidden:.
        /// </summary>
        public string EnergySaveModeWinText
        {
            get
            {
                return _rm.GetString("EnergySaveModeWinText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Enter this channel.
        /// </summary>
        public string EnterChannelText
        {
            get
            {
                return _rm.GetString("EnterChannelText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Enter text here...
        /// </summary>
        public string EnterTextText
        {
            get
            {
                return _rm.GetString("EnterTextText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Error.
        /// </summary>
        public string ErrorText
        {
            get
            {
                return _rm.GetString("ErrorText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Example.
        /// </summary>
        public string ExampleLabel
        {
            get
            {
                return _rm.GetString("ExampleLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Exit snooper after hosting?.
        /// </summary>
        public string ExitAfterHosting
        {
            get
            {
                return _rm.GetString("ExitAfterHosting");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Exit.
        /// </summary>
        public string ExitText
        {
            get
            {
                return _rm.GetString("ExitText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Export settings.
        /// </summary>
        public string ExportSettingsButtonContent
        {
            get
            {
                return _rm.GetString("ExportSettingsButtonContent");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Filter...
        /// </summary>
        public string FilterText
        {
            get
            {
                return _rm.GetString("FilterText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Font Chooser.
        /// </summary>
        public string FontChooserTitle
        {
            get
            {
                return _rm.GetString("FontChooserTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Found text style:.
        /// </summary>
        public string FoundStyleText
        {
            get
            {
                return _rm.GetString("FoundStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Game name:.
        /// </summary>
        public string GameNameLabel
        {
            get
            {
                return _rm.GetString("GameNameLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Password can contain only characters from the English alphabet!.
        /// </summary>
        public string GamePassBadText
        {
            get
            {
                return _rm.GetString("GamePassBadText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Games.
        /// </summary>
        public string GamesText
        {
            get
            {
                return _rm.GetString("GamesText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to General sounds.
        /// </summary>
        public string GeneralSoundsText
        {
            get
            {
                return _rm.GetString("GeneralSoundsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to General.
        /// </summary>
        public string GeneralText
        {
            get
            {
                return _rm.GetString("GeneralText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} players.
        /// </summary>
        public string GroupPlayersEditTitle
        {
            get
            {
                return _rm.GetString("GroupPlayersEditTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to A player from {0} comes online:.
        /// </summary>
        public string GroupSoundSettingText
        {
            get
            {
                return _rm.GetString("GroupSoundSettingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Group sounds.
        /// </summary>
        public string GroupSoundsText
        {
            get
            {
                return _rm.GetString("GroupSoundsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Group.
        /// </summary>
        public string GroupText
        {
            get
            {
                return _rm.GetString("GroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Great Snooper is used by {0} user(s)! {1}.
        /// </summary>
        public string GSCheckText
        {
            get
            {
                return _rm.GetString("GSCheckText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Great Snooper check.
        /// </summary>
        public string GSCheckTitle
        {
            get
            {
                return _rm.GetString("GSCheckTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Your nickname is already used by somebody on this server. You can choose another nickname in Settings..
        /// </summary>
        public string GSNickInUseText
        {
            get
            {
                return _rm.GetString("GSNickInUseText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} is now known as {1}..
        /// </summary>
        public string GSNicknameChange
        {
            get
            {
                return _rm.GetString("GSNicknameChange");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The selected nickname is already in use!.
        /// </summary>
        public string GSNicknameInUse
        {
            get
            {
                return _rm.GetString("GSNicknameInUse");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to has left the server ({0})..
        /// </summary>
        public string GSQuitWMessage
        {
            get
            {
                return _rm.GetString("GSQuitWMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to has left the server..
        /// </summary>
        public string GSQuitWOMessage
        {
            get
            {
                return _rm.GetString("GSQuitWOMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Great Snooper is still running here..
        /// </summary>
        public string GSRunningTaskbar
        {
            get
            {
                return _rm.GetString("GSRunningTaskbar");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to No trolling please!.
        /// </summary>
        public string GSVersionTrolling
        {
            get
            {
                return _rm.GetString("GSVersionTrolling");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to #worms.
        /// </summary>
        public string GSWormsText
        {
            get
            {
                return _rm.GetString("GSWormsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Hidden channels.
        /// </summary>
        public string HiddenChannelsText
        {
            get
            {
                return _rm.GetString("HiddenChannelsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Hide channel.
        /// </summary>
        public string HideChannelText
        {
            get
            {
                return _rm.GetString("HideChannelText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to When your name appears in chat:.
        /// </summary>
        public string HighlightSettingText
        {
            get
            {
                return _rm.GetString("HighlightSettingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to You have been highlighted in {0}!.
        /// </summary>
        public string HightLightMessage
        {
            get
            {
                return _rm.GetString("HightLightMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Host a game.
        /// </summary>
        public string HostAGameText
        {
            get
            {
                return _rm.GetString("HostAGameText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Failed to create the game on WormNet..
        /// </summary>
        public string HosterCreateGameFail
        {
            get
            {
                return _rm.GetString("HosterCreateGameFail");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Hoster.exe does not exist! You can not host without that file!.
        /// </summary>
        public string HosterExeText
        {
            get
            {
                return _rm.GetString("HosterExeText");
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to get local IP address. Please try hosting using WormNat2!.
        /// </summary>
        public string HosterFailedToGetLocalIP
        {
            get
            {
                return _rm.GetString("HosterFailedToGetLocalIP");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Failed to host a game. Probably you have hosted too many games recently. Please wait a while!.
        /// </summary>
        public string HosterNoGameIDError
        {
            get
            {
                return _rm.GetString("HosterNoGameIDError");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Failed to start Worms: Armageddon..
        /// </summary>
        public string HosterStartGameFail
        {
            get
            {
                return _rm.GetString("HosterStartGameFail");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to An unexpected error happened while trying to host a game..
        /// </summary>
        public string HosterUnknownFail
        {
            get
            {
                return _rm.GetString("HosterUnknownFail");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The communication with WormNat server failed..
        /// </summary>
        public string HosterWormNatError
        {
            get
            {
                return _rm.GetString("HosterWormNatError");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Send hosting action to channel?.
        /// </summary>
        public string HostingAction
        {
            get
            {
                return _rm.GetString("HostingAction");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Password (optional):.
        /// </summary>
        public string HostingPasswordLabel
        {
            get
            {
                return _rm.GetString("HostingPasswordLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Host a game.
        /// </summary>
        public string HostingTitle
        {
            get
            {
                return _rm.GetString("HostingTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Style of hyperlinks:.
        /// </summary>
        public string HyperlinksStyleText
        {
            get
            {
                return _rm.GetString("HyperlinksStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Ignore list.
        /// </summary>
        public string IgnoreListTitle
        {
            get
            {
                return _rm.GetString("IgnoreListTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Info.
        /// </summary>
        public string InfoHeaderLabel
        {
            get
            {
                return _rm.GetString("InfoHeaderLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Import settings.
        /// </summary>
        public string ImportSettingsButtonContent
        {
            get
            {
                return _rm.GetString("ImportSettingsButtonContent");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Information.
        /// </summary>
        public string InformationText
        {
            get
            {
                return _rm.GetString("InformationText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Info: {0}.
        /// </summary>
        public string InfoText
        {
            get
            {
                return _rm.GetString("InfoText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to In game names.
        /// </summary>
        public string InGameNamesLabel
        {
            get
            {
                return _rm.GetString("InGameNamesLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to In hoster names.
        /// </summary>
        public string InHosterNamesLabel
        {
            get
            {
                return _rm.GetString("InHosterNamesLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to In join messages.
        /// </summary>
        public string InJoinMessagesLabel
        {
            get
            {
                return _rm.GetString("InJoinMessagesLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to In messages.
        /// </summary>
        public string InMessagesLabel
        {
            get
            {
                return _rm.GetString("InMessagesLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to In sender names.
        /// </summary>
        public string InSenderNamesLabel
        {
            get
            {
                return _rm.GetString("InSenderNamesLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Invalid value.
        /// </summary>
        public string InvalidValueText
        {
            get
            {
                return _rm.GetString("InvalidValueText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Italic font style for Great Snooper users:
        /// </summary>
        public string ItalicForGSUsers
        {
            get
            {
                return _rm.GetString("ItalicForGSUsers");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Italic:.
        /// </summary>
        public string ItalicLabel
        {
            get
            {
                return _rm.GetString("ItalicLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Join and close snooper.
        /// </summary>
        public string JoinCloseText
        {
            get
            {
                return _rm.GetString("JoinCloseText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Join and close snooper.
        /// </summary>
        public string JoinCloseText2
        {
            get
            {
                return _rm.GetString("JoinCloseText2");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Join this game.
        /// </summary>
        public string JoinGameText
        {
            get
            {
                return _rm.GetString("JoinGameText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Join this game.
        /// </summary>
        public string JoinGameText2
        {
            get
            {
                return _rm.GetString("JoinGameText2");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to joined the channel..
        /// </summary>
        public string JoinMessage
        {
            get
            {
                return _rm.GetString("JoinMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Join message style:.
        /// </summary>
        public string JoinMessageStyleText
        {
            get
            {
                return _rm.GetString("JoinMessageStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Kicked by {0}.
        /// </summary>
        public string KickMessage
        {
            get
            {
                return _rm.GetString("KickMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please restart Great Snooper to apply new language settings everywhere!.
        /// </summary>
        public string LanguageChangedText
        {
            get
            {
                return _rm.GetString("LanguageChangedText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to English.
        /// </summary>
        public string LanguageEnName
        {
            get
            {
                return _rm.GetString("LanguageEnName");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to English.
        /// </summary>
        public string LanguageName
        {
            get
            {
                return _rm.GetString("LanguageName");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Language:.
        /// </summary>
        public string LanguageText
        {
            get
            {
                return _rm.GetString("LanguageText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to .. and without this nice community ;).
        /// </summary>
        public string LastLineText
        {
            get
            {
                return _rm.GetString("LastLineText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to When the snooper stops searching league game:.
        /// </summary>
        public string LeagueFailSettingText
        {
            get
            {
                return _rm.GetString("LeagueFailSettingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to League Game Searcher.
        /// </summary>
        public string LeagueGameTitle
        {
            get
            {
                return _rm.GetString("LeagueGameTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to League players.
        /// </summary>
        public string LeaguePlayersGroupText
        {
            get
            {
                return _rm.GetString("LeaguePlayersGroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Would you like to turn off league searcher?.
        /// </summary>
        public string LeagueSearcherRunningText
        {
            get
            {
                return _rm.GetString("LeagueSearcherRunningText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Search for league game.
        /// </summary>
        public string LeagueSearcherTooltip
        {
            get
            {
                return _rm.GetString("LeagueSearcherTooltip");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Leagues may be still loading, please wait!.
        /// </summary>
        public string LeaguesLoadingText
        {
            get
            {
                return _rm.GetString("LeaguesLoadingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to When the snooper finds a league game:.
        /// </summary>
        public string LeagueSoundSettingText
        {
            get
            {
                return _rm.GetString("LeagueSoundSettingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Leave this channel.
        /// </summary>
        public string LeaveChannelText
        {
            get
            {
                return _rm.GetString("LeaveChannelText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to License: GPLv2.
        /// </summary>
        public string LicenseText
        {
            get
            {
                return _rm.GetString("LicenseText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to To add item to the list enter text into the textbox and press enter. To remove item from the list right click on an item and choose remove..
        /// </summary>
        public string ListEditorInfoText
        {
            get
            {
                return _rm.GetString("ListEditorInfoText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Help.
        /// </summary>
        public string ListEditorInfoTitle
        {
            get
            {
                return _rm.GetString("ListEditorInfoTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Load channel scheme (required for hosting).
        /// </summary>
        public string LoadChannelSchemeText
        {
            get
            {
                return _rm.GetString("LoadChannelSchemeText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Load common settings (required for league searcher, updater and news).
        /// </summary>
        public string LoadCommonSettingsText
        {
            get
            {
                return _rm.GetString("LoadCommonSettingsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Load games.
        /// </summary>
        public string LoadGamesText
        {
            get
            {
                return _rm.GetString("LoadGamesText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Load games and TUS accounts only if window is active:.
        /// </summary>
        public string LoadGamesTUSActiveWindow
        {
            get
            {
                return _rm.GetString("LoadGamesTUSActiveWindow");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Load TUS users.
        /// </summary>
        public string LoadTUSUsersText
        {
            get
            {
                return _rm.GetString("LoadTUSUsersText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Log in.
        /// </summary>
        public string LogInText
        {
            get
            {
                return _rm.GetString("LogInText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Log out.
        /// </summary>
        public string LogOutText
        {
            get
            {
                return _rm.GetString("LogOutText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to This program was written by Tomi.
        /// </summary>
        public string MadeByText
        {
            get
            {
                return _rm.GetString("MadeByText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Mark me away when I host or join a game:.
        /// </summary>
        public string MarkAwayText
        {
            get
            {
                return _rm.GetString("MarkAwayText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Message Color:.
        /// </summary>
        public string MessageColorLabel
        {
            get
            {
                return _rm.GetString("MessageColorLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Send action message to the channel if I join a game:.
        /// </summary>
        public string MessageJoinedGameText
        {
            get
            {
                return _rm.GetString("MessageJoinedGameText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to message.
        /// </summary>
        public string MessageLabel
        {
            get
            {
                return _rm.GetString("MessageLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Message logs.
        /// </summary>
        public string MessageLogsText
        {
            get
            {
                return _rm.GetString("MessageLogsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Messages.
        /// </summary>
        public string MessagesText
        {
            get
            {
                return _rm.GetString("MessagesText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Style of message arrived time:.
        /// </summary>
        public string MessageTimeStyleText
        {
            get
            {
                return _rm.GetString("MessageTimeStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Show the time when the message arrived:.
        /// </summary>
        public string MessageTimeText
        {
            get
            {
                return _rm.GetString("MessageTimeText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Missing value.
        /// </summary>
        public string MissingValueText
        {
            get
            {
                return _rm.GetString("MissingValueText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to More info.
        /// </summary>
        public string MoreInfoText
        {
            get
            {
                return _rm.GetString("MoreInfoText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Sounds on / off.
        /// </summary>
        public string MuteTooltip
        {
            get
            {
                return _rm.GetString("MuteTooltip");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Network.
        /// </summary>
        public string NetworkText
        {
            get
            {
                return _rm.GetString("NetworkText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to News.
        /// </summary>
        public string NewsWindowTitle
        {
            get
            {
                return _rm.GetString("NewsWindowTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to There is a new update available for Great Snooper! Would you like to download it now?.
        /// </summary>
        public string NewVersionText
        {
            get
            {
                return _rm.GetString("NewVersionText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Nick Color:.
        /// </summary>
        public string NickColorLabel
        {
            get
            {
                return _rm.GetString("NickColorLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please enter your nickname!.
        /// </summary>
        public string NickEmptyText
        {
            get
            {
                return _rm.GetString("NickEmptyText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Your nickname contains one or more forbidden characters! Use characters from the English alphabet, numbers, - or `!.
        /// </summary>
        public string NickHasBadChar
        {
            get
            {
                return _rm.GetString("NickHasBadChar");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Nick.
        /// </summary>
        public string NickHeaderLabel
        {
            get
            {
                return _rm.GetString("NickHeaderLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to This nickname is already in use! Please choose an other one! Note: if you lost your internet connection, you may need to wait 1 or 2 minutes until the server releases your broken nickname..
        /// </summary>
        public string NicknameInUseText
        {
            get
            {
                return _rm.GetString("NicknameInUseText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Nickname:.
        /// </summary>
        public string NicknameText
        {
            get
            {
                return _rm.GetString("NicknameText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Your nickname should begin with a character from the English aplhabet or with ` character!.
        /// </summary>
        public string NickStartsBad
        {
            get
            {
                return _rm.GetString("NickStartsBad");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to No group.
        /// </summary>
        public string NoGroupText
        {
            get
            {
                return _rm.GetString("NoGroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please select the league(s) you are looking for!.
        /// </summary>
        public string NoLeaguesSelectedError
        {
            get
            {
                return _rm.GetString("NoLeaguesSelectedError");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to No.
        /// </summary>
        public string NoText
        {
            get
            {
                return _rm.GetString("NoText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Notice message style:.
        /// </summary>
        public string NoticeMessageStyleText
        {
            get
            {
                return _rm.GetString("NoticeMessageStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Notifications.
        /// </summary>
        public string NotificationsText
        {
            get
            {
                return _rm.GetString("NotificationsText");
            }
        }


        /// <summary>
        ///   Looks up a localized string similar to {0} is hosting a game: {1}.
        /// </summary>
        public string NotificatorHelpLabel
        {
            get
            {
                return _rm.GetString("NotificatorHelpLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} is hosting a game: {1}.
        /// </summary>
        public string NotificatorHelpText
        {
            get
            {
                return _rm.GetString("NotificatorHelpText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} is hosting a game: {1}.
        /// </summary>
        public string NotificatorGameText
        {
            get
            {
                return _rm.GetString("NotificatorGameText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Would you like to turn off notificator?.
        /// </summary>
        public string NotificatorRunningText
        {
            get
            {
                return _rm.GetString("NotificatorRunningText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Notificator.
        /// </summary>
        public string NotificatorTooltip
        {
            get
            {
                return _rm.GetString("NotificatorTooltip");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} joined {1}!.
        /// </summary>
        public string NotifOnlineMessage
        {
            get
            {
                return _rm.GetString("NotifOnlineMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to When the notificator finds something:.
        /// </summary>
        public string NotifSoundSettingText
        {
            get
            {
                return _rm.GetString("NotifSoundSettingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} is currently offline..
        /// </summary>
        public string OfflineMessage
        {
            get
            {
                return _rm.GetString("OfflineMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to OK.
        /// </summary>
        public string OKText
        {
            get
            {
                return _rm.GetString("OKText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} is online..
        /// </summary>
        public string OnlineMessage
        {
            get
            {
                return _rm.GetString("OnlineMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Other 1.
        /// </summary>
        public string Other1GroupText
        {
            get
            {
                return _rm.GetString("Other1GroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Other 2.
        /// </summary>
        public string Other2GroupText
        {
            get
            {
                return _rm.GetString("Other2GroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Other 3.
        /// </summary>
        public string Other3GroupText
        {
            get
            {
                return _rm.GetString("Other3GroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Other 4.
        /// </summary>
        public string Other4GroupText
        {
            get
            {
                return _rm.GetString("Other4GroupText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to has left the channel..
        /// </summary>
        public string PartMessage
        {
            get
            {
                return _rm.GetString("PartMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to has left the channel ({0})..
        /// </summary>
        public string PartMessage2
        {
            get
            {
                return _rm.GetString("PartMessage2");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Part message style:.
        /// </summary>
        public string PartMessageStyleText
        {
            get
            {
                return _rm.GetString("PartMessageStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Player:.
        /// </summary>
        public string PlayerLabel
        {
            get
            {
                return _rm.GetString("PlayerLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Player list.
        /// </summary>
        public string PlayersText
        {
            get
            {
                return _rm.GetString("PlayersText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Private message arrived:.
        /// </summary>
        public string PMBeepSettingText
        {
            get
            {
                return _rm.GetString("PMBeepSettingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to is online..
        /// </summary>
        public string PMOnlineMessage
        {
            get
            {
                return _rm.GetString("PMOnlineMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to There may be messages that this user could not receive!.
        /// </summary>
        public string PMPingTimeoutMessage
        {
            get
            {
                return _rm.GetString("PMPingTimeoutMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Polish translation by:.
        /// </summary>
        public string PolishTranslationText
        {
            get
            {
                return _rm.GetString("PolishTranslationText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to ?.
        /// </summary>
        public string QuestionMark
        {
            get
            {
                return _rm.GetString("QuestionMark");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Question.
        /// </summary>
        public string QuestionText
        {
            get
            {
                return _rm.GetString("QuestionText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Quit message style:.
        /// </summary>
        public string QuitMessageStyleText
        {
            get
            {
                return _rm.GetString("QuitMessageStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Quit message:.
        /// </summary>
        public string QuitMessageText
        {
            get
            {
                return _rm.GetString("QuitMessageText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Rank.
        /// </summary>
        public string RankHeaderLabel
        {
            get
            {
                return _rm.GetString("RankHeaderLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Rank:.
        /// </summary>
        public string RankText
        {
            get
            {
                return _rm.GetString("RankText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Great Snooper has reconnected..
        /// </summary>
        public string ReconnectMessage
        {
            get
            {
                return _rm.GetString("ReconnectMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Refresh game list.
        /// </summary>
        public string RefrestGameListText
        {
            get
            {
                return _rm.GetString("RefrestGameListText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Remove from conversation.
        /// </summary>
        public string RemoveFromConvText
        {
            get
            {
                return _rm.GetString("RemoveFromConvText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Remove from ignore list.
        /// </summary>
        public string RemoveIgnoreText
        {
            get
            {
                return _rm.GetString("RemoveIgnoreText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Remove.
        /// </summary>
        public string RemoveText
        {
            get
            {
                return _rm.GetString("RemoveText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Reset settings.
        /// </summary>
        public string ResetSettingsButtonContent
        {
            get
            {
                return _rm.GetString("ResetSettingsButtonContent");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Are you sure to reset settings? All informations will be lost..
        /// </summary>
        public string ResetSettingsConfirm
        {
            get
            {
                return _rm.GetString("ResetSettingsConfirm");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please restart Great Snooper to apply changes!.
        /// </summary>
        public string RestartToApplyChanges
        {
            get
            {
                return _rm.GetString("RestartToApplyChanges");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Russian translation by:.
        /// </summary>
        public string RussianTranslationText
        {
            get
            {
                return _rm.GetString("RussianTranslationText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Save.
        /// </summary>
        public string SaveText
        {
            get
            {
                return _rm.GetString("SaveText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to An error occurred: {0}.
        /// </summary>
        public string ServerErrorMessage
        {
            get
            {
                return _rm.GetString("ServerErrorMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The official server is: wormnet1.team17.com, you can find there all the players. But sometimes it is not accessable due to maintenance. Then you can use an alternative server to play for that short time..
        /// </summary>
        public string ServerInfoText
        {
            get
            {
                return _rm.GetString("ServerInfoText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Servers.
        /// </summary>
        public string ServerListEditorTitle
        {
            get
            {
                return _rm.GetString("ServerListEditorTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please choose a server!.
        /// </summary>
        public string ServerMissingText
        {
            get
            {
                return _rm.GetString("ServerMissingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Server disconnected: {0}.
        /// </summary>
        public string ServerQuitMessage
        {
            get
            {
                return _rm.GetString("ServerQuitMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Server:.
        /// </summary>
        public string ServerText
        {
            get
            {
                return _rm.GetString("ServerText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Settings.
        /// </summary>
        public string SettingsText
        {
            get
            {
                return _rm.GetString("SettingsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Settings.
        /// </summary>
        public string SettingsTitle
        {
            get
            {
                return _rm.GetString("SettingsTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Show messages of banned users in the channels:.
        /// </summary>
        public string ShowBannedMessagesText
        {
            get
            {
                return _rm.GetString("ShowBannedMessagesText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Show banned users in user list:.
        /// </summary>
        public string ShowBannedUsersText
        {
            get
            {
                return _rm.GetString("ShowBannedUsersText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Show information column in user list:.
        /// </summary>
        public string ShowInfoColumnText
        {
            get
            {
                return _rm.GetString("ShowInfoColumnText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Show news.
        /// </summary>
        public string ShowNewsText
        {
            get
            {
                return _rm.GetString("ShowNewsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Show snooper.
        /// </summary>
        public string ShowSnooperText
        {
            get
            {
                return _rm.GetString("ShowSnooperText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Show #worms channel:.
        /// </summary>
        public string ShowWormsText
        {
            get
            {
                return _rm.GetString("ShowWormsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Silent join and close snooper.
        /// </summary>
        public string SilentJoinCloseText
        {
            get
            {
                return _rm.GetString("SilentJoinCloseText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Silent join and close snooper.
        /// </summary>
        public string SilentJoinCloseText2
        {
            get
            {
                return _rm.GetString("SilentJoinCloseText2");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Silent join.
        /// </summary>
        public string SilentJoinText
        {
            get
            {
                return _rm.GetString("SilentJoinText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Silent join.
        /// </summary>
        public string SilentJoinText2
        {
            get
            {
                return _rm.GetString("SilentJoinText2");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Simple login.
        /// </summary>
        public string SimpleLoginText
        {
            get
            {
                return _rm.GetString("SimpleLoginText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Size:.
        /// </summary>
        public string SizeLabel
        {
            get
            {
                return _rm.GetString("SizeLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Sounds.
        /// </summary>
        public string SoundsText
        {
            get
            {
                return _rm.GetString("SoundsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Spam the channel?.
        /// </summary>
        public string SpamChannelText
        {
            get
            {
                return _rm.GetString("SpamChannelText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Great Snooper stopped spamming and searching for league game(s)!.
        /// </summary>
        public string SpamStopMessage
        {
            get
            {
                return _rm.GetString("SpamStopMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Start notificator with snooper.
        /// </summary>
        public string StartNotifWithSnooper
        {
            get
            {
                return _rm.GetString("StartNotifWithSnooper");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Start searching.
        /// </summary>
        public string StartSearchingText
        {
            get
            {
                return _rm.GetString("StartSearchingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Run WA.exe.
        /// </summary>
        public string StartWAExeTooltip1
        {
            get
            {
                return _rm.GetString("StartWAExeTooltip1");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Run alternative WA.exe.
        /// </summary>
        public string StartWAExeTooltip2
        {
            get
            {
                return _rm.GetString("StartWAExeTooltip2");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Stop searching.
        /// </summary>
        public string StopSearchingText
        {
            get
            {
                return _rm.GetString("StopSearchingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Strikethrough:.
        /// </summary>
        public string StrikethroughLabel
        {
            get
            {
                return _rm.GetString("StrikethroughLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to System message style:.
        /// </summary>
        public string SystemMessageStyleText
        {
            get
            {
                return _rm.GetString("SystemMessageStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to System.
        /// </summary>
        public string SystemUserName
        {
            get
            {
                return _rm.GetString("SystemUserName");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to This program would never exist without the help of:.
        /// </summary>
        public string ThanksText
        {
            get
            {
                return _rm.GetString("ThanksText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Theme:.
        /// </summary>
        public string ThemeText
        {
            get
            {
                return _rm.GetString("ThemeText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to {0} has changed the topic of the channel to &quot;{1}&quot;..
        /// </summary>
        public string TopicMessage
        {
            get
            {
                return _rm.GetString("TopicMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Enable tray flashing:.
        /// </summary>
        public string TrayFlashingText
        {
            get
            {
                return _rm.GetString("TrayFlashingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Enable tray balloon messages:.
        /// </summary>
        public string TrayNotificationsText
        {
            get
            {
                return _rm.GetString("TrayNotificationsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Analyzer.
        /// </summary>
        public string TusAnalyzerText
        {
            get
            {
                return _rm.GetString("TusAnalyzerText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The given username or password was incorrent!.
        /// </summary>
        public string TusAuthFailText
        {
            get
            {
                return _rm.GetString("TusAuthFailText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The communication with tus-wa.com has failed. Please try again!.
        /// </summary>
        public string TusCommFailText
        {
            get
            {
                return _rm.GetString("TusCommFailText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Complaints forum.
        /// </summary>
        public string TusComplaintsText
        {
            get
            {
                return _rm.GetString("TusComplaintsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Cups.
        /// </summary>
        public string TusCupsText
        {
            get
            {
                return _rm.GetString("TusCupsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Great Snooper topic.
        /// </summary>
        public string TusGSTopicText
        {
            get
            {
                return _rm.GetString("TusGSTopicText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Home page.
        /// </summary>
        public string TusHomePageText
        {
            get
            {
                return _rm.GetString("TusHomePageText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to An error occoured! Please try again!.
        /// </summary>
        public string TusLoginFailText
        {
            get
            {
                return _rm.GetString("TusLoginFailText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Login failed.
        /// </summary>
        public string TusLoginFailTitle
        {
            get
            {
                return _rm.GetString("TusLoginFailTitle");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to TUS login.
        /// </summary>
        public string TusLoginText
        {
            get
            {
                return _rm.GetString("TusLoginText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please enter your TUS nickname!.
        /// </summary>
        public string TusNickEmptyText
        {
            get
            {
                return _rm.GetString("TusNickEmptyText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to TUS nickname:.
        /// </summary>
        public string TusNicknameText
        {
            get
            {
                return _rm.GetString("TusNicknameText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Please enter your TUS password!.
        /// </summary>
        public string TusPasswordEmptyText
        {
            get
            {
                return _rm.GetString("TusPasswordEmptyText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to TUS password:.
        /// </summary>
        public string TusPasswordText
        {
            get
            {
                return _rm.GetString("TusPasswordText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Private messages.
        /// </summary>
        public string TusPmsText
        {
            get
            {
                return _rm.GetString("TusPmsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Recent games.
        /// </summary>
        public string TusRecentGamesText
        {
            get
            {
                return _rm.GetString("TusRecentGamesText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Report page.
        /// </summary>
        public string TusReportText
        {
            get
            {
                return _rm.GetString("TusReportText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Standings.
        /// </summary>
        public string TusStandingsText
        {
            get
            {
                return _rm.GetString("TusStandingsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Tournaments.
        /// </summary>
        public string TusTournamentsText
        {
            get
            {
                return _rm.GetString("TusTournamentsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Underline:.
        /// </summary>
        public string UnderlineLabel
        {
            get
            {
                return _rm.GetString("UnderlineLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Failed to start the updater! Please try to run it manually from the installation directory of Great Snooper!.
        /// </summary>
        public string UpdaterFailText
        {
            get
            {
                return _rm.GetString("UpdaterFailText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to User groups.
        /// </summary>
        public string UserGroupsText
        {
            get
            {
                return _rm.GetString("UserGroupsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Style of your message:.
        /// </summary>
        public string UserMessageStyleText
        {
            get
            {
                return _rm.GetString("UserMessageStyleText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Users.
        /// </summary>
        public string UsersText
        {
            get
            {
                return _rm.GetString("UsersText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Use snooper rank:.
        /// </summary>
        public string UseSnooperRank
        {
            get
            {
                return _rm.GetString("UseSnooperRank");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to This option is required for TUS login if you would like to use HostingBuddy to host games with the snooper..
        /// </summary>
        public string UseSnooperRankHelp
        {
            get
            {
                return _rm.GetString("UseSnooperRankHelp");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Use WHO messages (required for country, rank, user info).
        /// </summary>
        public string UseWhoMessagesText
        {
            get
            {
                return _rm.GetString("UseWhoMessagesText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Using WormNat2?.
        /// </summary>
        public string UsingWormNat2
        {
            get
            {
                return _rm.GetString("UsingWormNat2");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Version: {0}.
        /// </summary>
        public string VersionText
        {
            get
            {
                return _rm.GetString("VersionText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to View {0}&apos;s profile.
        /// </summary>
        public string ViewTusText
        {
            get
            {
                return _rm.GetString("ViewTusText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to View {0}&apos;s profile.
        /// </summary>
        public string ViewClanProfileText
        {
            get
            {
                return _rm.GetString("ViewClanProfileText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Volume.
        /// </summary>
        public string VolumeTooltip
        {
            get
            {
                return _rm.GetString("VolumeTooltip");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to WA.exe is missing. Please set WA.exe in the Settings!.
        /// </summary>
        public string WAExeMissingText
        {
            get
            {
                return _rm.GetString("WAExeMissingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to WA.exe can not be found! Please set WA.exe in the Settings!.
        /// </summary>
        public string WAExeNotExistsText
        {
            get
            {
                return _rm.GetString("WAExeNotExistsText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Ooops, it seems like Great Snooper could not find your WA.exe! You can not host or join a game without that file. Would you like to locate your WA.exe now? You can do it later in the settings too..
        /// </summary>
        public string WAExeNotFoundText
        {
            get
            {
                return _rm.GetString("WAExeNotFoundText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to WA.exe:.
        /// </summary>
        public string WAExeText
        {
            get
            {
                return _rm.GetString("WAExeText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Alternative WA.exe:.
        /// </summary>
        public string WAExeText2
        {
            get
            {
                return _rm.GetString("WAExeText2");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Set W:A task priority to high:.
        /// </summary>
        public string WaHighPriorityText
        {
            get
            {
                return _rm.GetString("WaHighPriorityText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to You are already in a game!.
        /// </summary>
        public string WAIsRunningText
        {
            get
            {
                return _rm.GetString("WAIsRunningText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Welcome to Great Snooper!.
        /// </summary>
        public string WelcomeMessage
        {
            get
            {
                return _rm.GetString("WelcomeMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Welcome {0}!.
        /// </summary>
        public string WelcomeText
        {
            get
            {
                return _rm.GetString("WelcomeText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to What is TUS login?.
        /// </summary>
        public string WhatIsTusLoginLabel
        {
            get
            {
                return _rm.GetString("WhatIsTusLoginLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to TUS is The Ultimate Site. It is a website for Worms: Armageddon, which has a lot of things for the game, such as: leagues, cups, tournaments, schemes, maps, forum, etc. It also supports snoopers. If you login with your TUS account into the snooper, then your rank will be your TUS league rank, your clan will also be set and you will be marked online on the website. . You can also set a different nickname and a password for snooper login on TUS site in your Account Settings..
        /// </summary>
        public string WhatIsTusLoginText
        {
            get
            {
                return _rm.GetString("WhatIsTusLoginText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Window.
        /// </summary>
        public string WindowText
        {
            get
            {
                return _rm.GetString("WindowText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Could not connect to the server! Check if the server address is correct or try again later (probably maintenance time)!.
        /// </summary>
        public string WNCommFailText
        {
            get
            {
                return _rm.GetString("WNCommFailText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to has left WormNet ({0})..
        /// </summary>
        public string WNQuitWMessage
        {
            get
            {
                return _rm.GetString("WNQuitWMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to has left WormNet..
        /// </summary>
        public string WNQuitWOMessage
        {
            get
            {
                return _rm.GetString("WNQuitWOMessage");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Greetings WormNAT2 user! This is a reminder message to remind you that WormNAT2 is a free service. Using WormNAT2 tunnels all data through a proxy server hosted by the community, thus consuming bandwidth and other resources. Therefore, we would like to ask you to only use WormNAT2 when you have already tried configuring hosting the proper way. Don&apos;t forget that you can find instructions on how to set up hosting here: http://worms2d.info/Hosting.
        /// </summary>
        public string WormNat2GreetingText
        {
            get
            {
                return _rm.GetString("WormNat2GreetingText");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to #worms channel nickname:.
        /// </summary>
        public string WormsNickLabel
        {
            get
            {
                return _rm.GetString("WormsNickLabel");
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Yes.
        /// </summary>
        public string YesText
        {
            get
            {
                return _rm.GetString("YesText");
            }
        }

    }
}
