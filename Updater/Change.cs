using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater
{
    class Change
    {
        public string command { get; private set; }
        public string type { get; private set; }
        public string arg { get; private set; }
        public string url { get; private set; }

        public Change(string command, string type, string arg, string url)
        {
            this.command = command;
            this.type = type;
            this.arg = arg;
            this.url = url;
        }
    }
}
