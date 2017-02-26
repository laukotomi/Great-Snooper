namespace GreatSnooper.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Xml;

    using GalaSoft.MvvmLight;

    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;

    using MahApps.Metro.Controls.Dialogs;

    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        internal void ContentRendered(object sender, EventArgs e)
        {
            if (this.closing)
            {
                return;
            }

            string latestVersion = string.Empty;
            bool openNews = false;

            loadSettingsTask = Task.Factory.StartNew(() =>
            {
                string settingsXMLPath = GlobalManager.SettingsPath + @"\Settings.xml";

                if (Properties.Settings.Default.LoadCommonSettings)
                {
                    try
                    {
                        string settingsXMLPathTemp = GlobalManager.SettingsPath + @"\SettingsTemp.xml";

                        using (WebDownload webClient = new WebDownload()
                        {
                            Proxy = null
                        })
                        {
                            webClient.DownloadFile("https://www.dropbox.com/s/5h5boog570q1nap/SnooperSettings.xml?dl=1", settingsXMLPathTemp);
                        }

                        // If downloading will fail then leagues won't be loaded. If they would, it could be hacked easily.
                        GlobalManager.SpamAllowed = true;

                        if (File.Exists(settingsXMLPath))
                        {
                            File.Delete(settingsXMLPath);
                        }

                        File.Move(settingsXMLPathTemp, settingsXMLPath);
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Log(ex);
                    }
                }

                if (this.closing)
                    return;

                if (File.Exists(settingsXMLPath))
                {
                    HashSet<string> serverList = new HashSet<string>(
                        Properties.Settings.Default.ServerAddresses.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                        GlobalManager.CIStringComparer);
                    bool updateServers = false;

                    using (XmlReader xml = XmlReader.Create(settingsXMLPath))
                    {
                        xml.ReadToFollowing("servers");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("server"))
                            {
                                inner.MoveToFirstAttribute();
                                string server = inner.Value;
                                if (!serverList.Contains(server))
                                {
                                    serverList.Add(server);
                                    updateServers = true;
                                }
                            }
                        }

                        xml.ReadToFollowing("leagues");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("league"))
                            {
                                inner.MoveToFirstAttribute();
                                string name = inner.Value;
                                inner.MoveToNextAttribute();
                                leagues.Add(new League(name, inner.Value));
                            }
                        }

                        xml.ReadToFollowing("news");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("bbnews"))
                            {
                                Dictionary<string, string> newsThings = new Dictionary<string, string>();
                                inner.MoveToFirstAttribute();
                                newsThings.Add(inner.Name, inner.Value);
                                while (inner.MoveToNextAttribute())
                                {
                                    newsThings.Add(inner.Name, inner.Value);
                                }

                                int id;
                                double fontsize;
                                if (newsThings.ContainsKey("id") && int.TryParse(newsThings["id"], out id) && newsThings.ContainsKey("show")
                                    && newsThings.ContainsKey("background") && newsThings.ContainsKey("textcolor")
                                    && newsThings.ContainsKey("fontsize") && double.TryParse(newsThings["fontsize"], out fontsize) && newsThings.ContainsKey("bbcode"))
                                {
                                    if (newsThings["show"] == "1" && id > Properties.Settings.Default.LastNewsID)
                                    {
                                        openNews = true;
                                    }

                                    newsList.Add(new News(id, newsThings["show"] == "1", newsThings["background"], newsThings["textcolor"], fontsize, newsThings["bbcode"]));
                                }
                            }
                        }

                        xml.ReadToFollowing("version");
                        xml.MoveToFirstAttribute();
                        latestVersion = xml.Value;
                    }

                    if (updateServers)
                    {
                        SettingsHelper.Save("ServerAddresses", serverList);
                    }
                }
                else
                {
                    leagues.Add(new League("TUS - Classic", "TUS"));
                    leagues.Add(new League("Clanner", "Clanner"));
                }
            })
            .ContinueWith((t) =>
            {
                if (this.closing)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.CloseCommand.Execute(null);
                    }));
                    return;
                }

                if (!GlobalManager.SpamAllowed && Properties.Settings.Default.LoadCommonSettings)
                {
                    if (t.IsFaulted)
                    {
                        ErrorLog.Log(t.Exception);
                    }
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.CommonSettingFailText);
                }
                else if (t.IsFaulted)
                {
                    ErrorLog.Log(t.Exception);
                    return;
                }
                else if (Math.Sign(App.GetVersion().CompareTo(latestVersion)) == -1) // we need update only if it is newer than this version
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.NewVersionText,
                                                  MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative, GlobalManager.YesNoDialogSetting, (tt) =>
                    {
                        if (tt.Result == MessageDialogResult.Affirmative)
                        {
                            try
                            {
                                Process p = new Process();
                                if (Environment.OSVersion.Version.Major >= 6) // Vista or higher (to get admin rights).. on xp this causes fail!
                                {
                                    p.StartInfo.UseShellExecute = true;
                                    p.StartInfo.Verb = "runas";
                                }
                                p.StartInfo.FileName = "Updater2.exe";
                                p.Start();
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.CloseCommand.Execute(null);
                                }));
                                return;
                            }
                            catch (Exception ex)
                            {
                                ErrorLog.Log(ex);
                                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.UpdaterFailText);
                            }
                        }
                        else if (openNews)
                            OpenNewsCommand.Execute(null);
                    });
                }
                else if (openNews)
                    OpenNewsCommand.Execute(null);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}