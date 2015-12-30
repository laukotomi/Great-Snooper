using GalaSoft.MvvmLight;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Services;
using GreatSnooper.Settings;
using GreatSnooper.Validators;
using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace GreatSnooper.ViewModel
{
    public class SettingsViewModel : ViewModelBase, IDisposable
    {
        #region Members
        private Regex groupNameRegex = new Regex("^Group[0-9]$");
        #endregion

        #region Properties
        public string Version { get; private set; }
        public IMetroDialogService DialogService { get; set; }
        public List<AbstractSetting> GeneralSettings { get; private set; }
        public List<AbstractSetting> NetworkSettings { get; private set; }
        public List<AbstractSetting> WindowSettings { get; private set; }
        public List<AbstractSetting> UserGroupSettings { get; private set; }
        public List<AbstractSetting> NotificationSettings { get; private set; }
        public List<AbstractSetting> WormsSettings { get; private set; }
        public List<AbstractSetting> MsgSettings { get; private set; }
        public List<AbstractSetting> SoundSettings { get; private set; }
        public ObservableCollection<AbstractSetting> GroupSoundSettings { get; private set; }
        #endregion

        public SettingsViewModel()
        {
            Version = string.Format(Localizations.GSLocalization.Instance.VersionText, App.GetVersion());
            Properties.Settings.Default.PropertyChanged += Default_PropertyChanged;
        }

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (groupNameRegex.IsMatch(e.PropertyName))
                this.LoadGroupSounds();
        }

        public void LoadSettings()
        {
            // Load languages
            var languages = new List<LanguageData>();
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            foreach (CultureInfo culture in cultures)
            {
                try
                {
                    if (culture.Name.Length > 0)
                    {
                        Localizations.GSLocalization loc = Localizations.GSLocalization.Instance;
                        ResourceSet rs = loc.RM.GetResourceSet(culture, true, false);
                        if (rs != null)
                        {
                            string languageEnName = loc.RM.GetString("LanguageEnName", culture);
                            string languageName = loc.RM.GetString("LanguageName", culture);
                            string countryCode = loc.RM.GetString("CountryCode", culture);
                            string cultureName = loc.RM.GetString("CultureName", culture);
                            if (languages.Where(x => x.CultureName == cultureName).Any() == false)
                                languages.Add(new LanguageData(languageEnName, languageName, countryCode, cultureName));
                        }
                    }
                }
                catch (CultureNotFoundException) { }
            }

            var selectedLanguage = languages.Where(x => x.CultureName == Properties.Settings.Default.CultureName).FirstOrDefault();

            // Load accents
            var accentColors = ThemeManager.Accents
                                .Select(a => new AccentColorMenuData(a.Name, a.Resources["AccentColorBrush"] as Brush))
                                .ToList();

            var selectedAccent = accentColors.Where(x => x.Name == Properties.Settings.Default.AccentName).FirstOrDefault();


            this.GeneralSettings = new List<AbstractSetting>();
            this.GeneralSettings.Add(new ComboboxSetting(Localizations.GSLocalization.Instance.LanguageText, languages, selectedLanguage, (DataTemplate)this.DialogService.GetView().TryFindResource("LanguageSelectorTemplate"), new Action<object>((x) => {
                if (x == null)
                    return;
                
                try
                {
                    var language = (LanguageData)x;
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(language.CultureName);
                    Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

                    Localizations.GSLocalization.Instance.CultureChanged();

                    foreach (var userGroup in UserGroups.Groups)
                        userGroup.Value.ReloadData();

                    Properties.Settings.Default.CultureName = language.CultureName;
                    Properties.Settings.Default.Save();

                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.LanguageChangedText);
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }
            })));
            this.GeneralSettings.Add(new ComboboxSetting(Localizations.GSLocalization.Instance.ThemeText, accentColors, selectedAccent, (DataTemplate)this.DialogService.GetView().TryFindResource("AccentSelectorTemplate"), new Action<object>((x) =>
            {
                if (x == null)
                    return;

                try
                {
                    var accentData = (AccentColorMenuData)x;
                    var theme = ThemeManager.DetectAppStyle(Application.Current);
                    var accent = ThemeManager.GetAccent(accentData.Name);
                    ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);

                    Properties.Settings.Default.AccentName = accent.Name;
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }
            })));
            this.GeneralSettings.Add(new BoolSetting("AutoLogIn", Localizations.GSLocalization.Instance.AutoLogInSettingText));
            this.GeneralSettings.Add(new BoolSetting("MessageJoinedGame", Localizations.GSLocalization.Instance.MessageJoinedGameText));
            this.GeneralSettings.Add(new BoolSetting("MarkAway", Localizations.GSLocalization.Instance.MarkAwayText));
            this.GeneralSettings.Add(new StringSetting("AwayText", Localizations.GSLocalization.Instance.AwayMessageText, Validator.WormNetTextValidator, this.DialogService));
            this.GeneralSettings.Add(new BoolSetting("DeleteLogs", Localizations.GSLocalization.Instance.DeleteLogsText));
            this.GeneralSettings.Add(new TextListSetting("HiddenChannels", Localizations.GSLocalization.Instance.HiddenChannelsText, Localizations.GSLocalization.Instance.HiddenChannelsText, this.DialogService, Validator.NotEmptyValidator));
            this.GeneralSettings.Add(new StringSetting("QuitMessagee", Localizations.GSLocalization.Instance.QuitMessageText, Validator.GSVersionValidator, this.DialogService));
            this.GeneralSettings.Add(new WAExeSetting("WaExe", Localizations.GSLocalization.Instance.WAExeText));
            this.GeneralSettings.Add(new WAExeSetting("WaExe2", Localizations.GSLocalization.Instance.WAExeText2));
            this.GeneralSettings.Add(new ExportImportSettings(this.DialogService));

            this.NetworkSettings = new List<AbstractSetting>();
            this.NetworkSettings.Add(new BoolSetting("LoadCommonSettings", Localizations.GSLocalization.Instance.LoadCommonSettingsText));
            this.NetworkSettings.Add(new BoolSetting("LoadTUSAccounts", Localizations.GSLocalization.Instance.LoadTUSUsersText));
            this.NetworkSettings.Add(new BoolSetting("LoadGames", Localizations.GSLocalization.Instance.LoadGamesText));
            this.NetworkSettings.Add(new BoolSetting("LoadChannelScheme", Localizations.GSLocalization.Instance.LoadChannelSchemeText));
            this.NetworkSettings.Add(new BoolSetting("UseWhoMessages", Localizations.GSLocalization.Instance.UseWhoMessagesText));
            this.NetworkSettings.Add(new BoolSetting("LoadOnlyIfWindowActive", Localizations.GSLocalization.Instance.LoadGamesTUSActiveWindow));

            this.WindowSettings = new List<AbstractSetting>();
            this.WindowSettings.Add(new BoolSetting("ShowBannedUsers", Localizations.GSLocalization.Instance.ShowBannedUsersText));
            this.WindowSettings.Add(new BoolSetting("ShowBannedMessages", Localizations.GSLocalization.Instance.ShowBannedMessagesText));
            this.WindowSettings.Add(new BoolSetting("ShowInfoColumn", Localizations.GSLocalization.Instance.ShowInfoColumnText));
            this.WindowSettings.Add(new BoolSetting("CloseToTray", Localizations.GSLocalization.Instance.CloseToTrayText));
            this.WindowSettings.Add(new BoolSetting("EnergySaveModeGame", Localizations.GSLocalization.Instance.EnergySaveModeGameText));
            this.WindowSettings.Add(new BoolSetting("EnergySaveModeWin", Localizations.GSLocalization.Instance.EnergySaveModeWinText));
            this.WindowSettings.Add(new BoolSetting("WAHighPriority", Localizations.GSLocalization.Instance.WaHighPriorityText));
            this.WindowSettings.Add(new BoolSetting("ItalicForGSUsers", Localizations.GSLocalization.Instance.ItalicForGSUsers));

            this.UserGroupSettings = new List<AbstractSetting>();
            foreach (var item in UserGroups.Groups)
                this.UserGroupSettings.Add(new UserGroupSetting(item.Value, Validator.NotEmptyValidator, this.DialogService));

            this.NotificationSettings = new List<AbstractSetting>();
            this.NotificationSettings.Add(new BoolSetting("TrayNotifications", Localizations.GSLocalization.Instance.TrayNotificationsText));
            this.NotificationSettings.Add(new BoolSetting("TrayFlashing", Localizations.GSLocalization.Instance.TrayFlashingText));
            this.NotificationSettings.Add(new BoolSetting("AskNotificatorOff", Localizations.GSLocalization.Instance.AskNotificatorOffText));
            this.NotificationSettings.Add(new BoolSetting("AskLeagueSearcherOff", Localizations.GSLocalization.Instance.AskLeagueSOffText));

            this.WormsSettings = new List<AbstractSetting>();
            this.WormsSettings.Add(new BoolSetting("ShowWormsChannel", Localizations.GSLocalization.Instance.ShowWormsText));
            this.WormsSettings.Add(new StringSetting("WormsNick", Localizations.GSLocalization.Instance.WormsNickLabel, Validator.NickNameValidator, this.DialogService));
            this.WormsSettings.Add(new BoolSetting("ChangeWormsNick", Localizations.GSLocalization.Instance.ChangeWormsNickText));

            this.MsgSettings = new List<AbstractSetting>();
            this.MsgSettings.Add(new BoolSetting("MessageTime", Localizations.GSLocalization.Instance.MessageTimeText));
            this.MsgSettings.Add(new StyleSetting("UserMessageStyle", Localizations.GSLocalization.Instance.UserMessageStyleText, MessageSettings.UserMessage, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("ChannelMessageStyle", Localizations.GSLocalization.Instance.ChannelMessageStyleText, MessageSettings.ChannelMessage, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("JoinMessageStyle", Localizations.GSLocalization.Instance.JoinMessageStyleText, MessageSettings.JoinMessage, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("PartMessageStyle", Localizations.GSLocalization.Instance.PartMessageStyleText, MessageSettings.PartMessage, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("QuitMessageStyle", Localizations.GSLocalization.Instance.QuitMessageStyleText, MessageSettings.QuitMessage, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("ActionMessageStyle", Localizations.GSLocalization.Instance.ActionMessageStyleText, MessageSettings.ActionMessage, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("NoticeMessageStyle", Localizations.GSLocalization.Instance.NoticeMessageStyleText, MessageSettings.NoticeMessage, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("OfflineMessageStyle", Localizations.GSLocalization.Instance.SystemMessageStyleText, MessageSettings.SystemMessage, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("MessageTimeStyle", Localizations.GSLocalization.Instance.MessageTimeStyleText, MessageSettings.MessageTimeStyle, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("HyperLinkStyle", Localizations.GSLocalization.Instance.HyperlinksStyleText, MessageSettings.HyperLinkStyle, this.DialogService));
            this.MsgSettings.Add(new StyleSetting("LeagueFoundMessageStyle", Localizations.GSLocalization.Instance.FoundStyleText, MessageSettings.LeagueFoundMessage, this.DialogService));

            this.SoundSettings = new List<AbstractSetting>();
            this.SoundSettings.Add(new SoundSetting("PMBeep", Localizations.GSLocalization.Instance.PMBeepSettingText));
            this.SoundSettings.Add(new SoundSetting("HBeep", Localizations.GSLocalization.Instance.HighlightSettingText));
            this.SoundSettings.Add(new SoundSetting("LeagueFoundBeep", Localizations.GSLocalization.Instance.LeagueSoundSettingText));
            this.SoundSettings.Add(new SoundSetting("LeagueFailBeep", Localizations.GSLocalization.Instance.LeagueFailSettingText));
            this.SoundSettings.Add(new SoundSetting("NotificatorSound", Localizations.GSLocalization.Instance.NotifSoundSettingText));

            this.GroupSoundSettings = new ObservableCollection<AbstractSetting>();
            this.LoadGroupSounds();

            this.DialogService.GetView().UpdateLayout();
        }

        private void LoadGroupSounds()
        {
            this.GroupSoundSettings.Clear();
            foreach (var item in UserGroups.Groups)
                this.GroupSoundSettings.Add(new SoundSetting(item.Value.SettingName + "Sound", string.Format(Localizations.GSLocalization.Instance.GroupSoundSettingText, item.Value.Name)));
        }

        #region IDisposable
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (disposing)
                Properties.Settings.Default.PropertyChanged -= Default_PropertyChanged;
        }

        ~SettingsViewModel()
        {
            Dispose(false);
        }
        #endregion
    }
}
