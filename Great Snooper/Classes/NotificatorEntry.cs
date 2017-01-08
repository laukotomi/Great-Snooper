using GreatSnooper.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GreatSnooper.Classes
{
    public class NotificatorEntry
    {
        private static Regex notificatorRow = new Regex(@"\{\{(?<channels>[^\,]*)\,?(?<wait>\d*)\}\}$", RegexOptions.Compiled);

        private Regex regex;

        public HashSet<string> ChannelNames { get; set; }
        public TimeSpan WaitTime { get; set; }
        public Dictionary<string, DateTime> LastBeepTimes { get; set; }

        public NotificatorEntry(string word)
        {
            Match m = notificatorRow.Match(word);
            if (m.Success)
            {
                this.regex = RegexService.GenerateRegex(word.Substring(0, m.Index));
                this.ChannelNames = new HashSet<string>(m.Groups["channels"].Value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Distinct(), StringComparer.OrdinalIgnoreCase);
                int seconds = m.Groups["wait"].Length > 0 ? Convert.ToInt32(m.Groups["wait"].Value) : 0;
                this.WaitTime = new TimeSpan(0, 0, seconds);
            }
            else
            {
                this.regex = RegexService.GenerateRegex(word);
                this.ChannelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                this.WaitTime = new TimeSpan();
            }
            this.LastBeepTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        }

        private bool CanMatch(string userName, string channelName)
        {
            if (this.WaitTime.Ticks > 0)
            {
                DateTime lastBeep;
                if (this.LastBeepTimes.TryGetValue(userName, out lastBeep) && DateTime.Now - lastBeep < this.WaitTime)
                {
                    return false;
                }
            }
            if (this.ChannelNames.Count > 0 && !this.ChannelNames.Contains(channelName))
            {
                return false;
            }

            return true;
        }

        public bool IsMatch(string word, string userName, string channelName)
        {
            bool isMatch = this.CanMatch(userName, channelName) && this.regex.IsMatch(word);
            if (isMatch)
            {
                this.LastBeepTimes[userName] = DateTime.Now;
            }

            return isMatch;
        }

        public MatchCollection Matches(string word, string userName, string channelName)
        {
            if (this.CanMatch(userName, channelName))
            {
                MatchCollection matches = this.regex.Matches(word);
                if (matches.Count > 0)
                {
                    this.LastBeepTimes[userName] = DateTime.Now;
                }
                return matches;
            }
            return null;
        }
    }
}
