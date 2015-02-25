using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Windows.Documents;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SortedDictionary<string, List<string>> versionDescriptions = new SortedDictionary<string, List<string>>();
        int divide = 0;
        BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            string updaterFile = "Updater3.exe";
            if (System.AppDomain.CurrentDomain.FriendlyName != updaterFile)
            {
                if (File.Exists(updaterFile))
                    File.Delete(updaterFile);
                File.Copy(System.AppDomain.CurrentDomain.FriendlyName, updaterFile);

                System.Diagnostics.Process p = new System.Diagnostics.Process();
                if (Environment.OSVersion.Version.Major >= 6) // Vista or higher (to get admin rights).. on xp this causes fail!
                {
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.Verb = "runas";
                }
                p.StartInfo.FileName = updaterFile;
                p.Start();
                this.Close();
                return;
            }

            string version = "1.3.1";
            if (File.Exists(Path.GetFullPath("Great Snooper.exe")))
            {
                version = getGSVersion();
            }

            InitializeComponent();

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += Update;
            worker.ProgressChanged += Report;
            worker.RunWorkerCompleted += Complete;
            worker.RunWorkerAsync(version);
        }

        private void Complete(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;

            if (e.Cancelled)
            {
                this.Close();
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show(this, "The update process failed! :(", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
            else
            {
                Status.Text = "Update completed. Please restart Great Snooper!";
                Progress.Value = 100;
            }
        }

        private string getGSVersion()
        {
            Version v = System.Reflection.AssemblyName.GetAssemblyName("Great Snooper.exe").Version;
            if (v.Build == 0)
            {
                return v.Major.ToString() + "." + v.Minor.ToString();
            }
            return v.Major.ToString() + "." + v.Minor.ToString() + "." + v.Build.ToString();
        }

        private void Report(object sender, ProgressChangedEventArgs e)
        {
            switch(e.ProgressPercentage)
            {
                case 0:
                    Status.Text = "Downloading Versions.xml";
                    break;

                case 1:
                    MessageBox.Show(this, "Failed to load Versions.xml! The updater will close itself.", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    break;
                    
                case 2:
                    Status.Text = "Processing Versions.xml";
                    Progress.Value = 10;
                    break;

                case 4:
                    Status.Text = "Generating the modification list";
                    Progress.Value = 20;

                    foreach (KeyValuePair<string, List<string>> item in versionDescriptions)
                    {
                        Paragraph p = new Paragraph();
                        p.FontWeight = FontWeights.Bold;
                        p.TextDecorations = TextDecorations.Underline;
                        p.Inlines.Add("Version " + item.Key + " changes:");
                        DisplayDescriptions.Blocks.Add(p);

                        foreach (string description in item.Value)
                        {
                            Paragraph p2 = new Paragraph();
                            p2.Inlines.Add(" - " + description);
                            DisplayDescriptions.Blocks.Add(p2);
                        }
                    }
                    break;

                case 6:
                    Status.Text = "Making changes..";
                    divide = 80 / ((int)e.UserState);
                    break;

                case 7:
                    Progress.Value += divide;
                    break;
            }
        }

        private void Update(object sender, DoWorkEventArgs e)
        {
            string version = (string)(e.Argument);

            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            worker.ReportProgress(0);

            SortedDictionary<string, List<Change>> versionChanges = new SortedDictionary<string, List<Change>>();

            try
            {
                // Download the content of the Versions.xml
                using (WebClient client = new WebClient() { Proxy = null })
                using (StringReader reader = new StringReader(client.DownloadString("https://www.dropbox.com/s/fk3ox7fx0i0w8me/Versions.xml?dl=1")))
                using (XmlReader xml = XmlReader.Create(reader))
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    worker.ReportProgress(2);

                    // Load versionChanges
                    while (xml.ReadToFollowing("version"))
                    {
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            // Read version
                            inner.ReadToFollowing("ver");
                            inner.MoveToFirstAttribute();
                            string ver = inner.Value;
                            if (Math.Sign(version.CompareTo(ver)) != -1)
                                continue; // we need only versions which are greater than the current one


                            // Read changes
                            List<Change> changes = new List<Change>();
                            inner.ReadToFollowing("changes");
                            using (XmlReader changesReader = inner.ReadSubtree())
                            {
                                while (changesReader.ReadToFollowing("change"))
                                {
                                    changesReader.MoveToFirstAttribute();
                                    string command = changesReader.Value;

                                    changesReader.MoveToNextAttribute();
                                    string type = changesReader.Value;

                                    changesReader.MoveToNextAttribute();
                                    string arg = changesReader.Value;

                                    string url = string.Empty;
                                    if (command == "create" || command == "update")
                                    {
                                        changesReader.MoveToNextAttribute();
                                        url = changesReader.Value;
                                    }

                                    changes.Add(new Change(command, type, arg, url));
                                }
                            }
                            versionChanges.Add(ver, changes);


                            // Read change descriptions
                            List<string> descriptions = new List<string>();
                            bool asd = inner.ReadToFollowing("descriptions");
                            using (XmlReader descrReader = inner.ReadSubtree())
                            {
                                while (descrReader.ReadToFollowing("description"))
                                {
                                    descrReader.MoveToFirstAttribute();
                                    descriptions.Add(descrReader.Value);
                                }
                            }
                            versionDescriptions.Add(ver, descriptions);
                        }
                    }
                }
            }
            catch (Exception)
            {
                worker.ReportProgress(1);
                return;
            }


            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            // Generate the final list about what should be updated, deleted etc.
            worker.ReportProgress(4);
            Dictionary<string, Change> realChanges = new Dictionary<string, Change>();

            foreach (KeyValuePair<string, List<Change>> item in versionChanges)
            {
                foreach (Change change in item.Value)
                {
                    if (!realChanges.ContainsKey(change.arg))
                    {
                        realChanges.Add(change.arg, change);
                    }
                    else
                    {
                        realChanges[change.arg] = change;
                    }
                }
            }

            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            worker.ReportProgress(6, realChanges.Count + 1);

            using (WebClient client = new WebClient() { Proxy = null })
            {
                foreach (KeyValuePair<string, Change> item in realChanges)
                {
                    if (item.Value.arg.Contains("../") || item.Value.arg.Contains(":"))
                        continue;

                    string path = Path.GetFullPath(item.Value.arg);

                    if (item.Value.command == "delete")
                    {
                        switch (item.Value.type)
                        {
                            case "directory":
                                if (Directory.Exists(path))
                                    Directory.Delete(path, true);
                                break;

                            case "file":
                                if (File.Exists(path))
                                    File.Delete(path);
                                break;
                        }
                    }
                    else if (item.Value.command == "create" || item.Value.command == "update")
                    {
                        switch (item.Value.type)
                        {
                            case "directory":
                                if (!Directory.Exists(path))
                                    Directory.CreateDirectory(path);
                                break;

                            case "file":
                                if (File.Exists(path))
                                    File.Delete(path);
                                client.DownloadFile(item.Value.url, path);
                                break;
                        }
                    }

                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    worker.ReportProgress(7);
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (worker.IsBusy)
            {
                worker.CancelAsync();
                e.Cancel = true;
                return;
            }
        }
    }
}
