using NDesk.Options;

namespace Hoster
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            var parser = new OptionSet();
            parser.Add("settings=", (v) => options.SettingsPath = v);
            parser.Add("server=", (v) => options.ServerAddress = v);
            parser.Add("waexe=", (v) => options.GameExePath = v);
            parser.Add("nick=", (v) => options.NickName = v);
            parser.Add("hostname=", (v) => options.HostName = v);
            parser.Add("password=", (v) => options.PassWord = v);
            parser.Add("channel=", (v) => options.ChannelName = v);
            parser.Add("scheme=", (v) => options.ChannelScheme = v);
            parser.Add("location=", (v) => options.Location = v);
            parser.Add("cc=", (v) => options.CC = v);
            parser.Add("wormnat", (v) => options.UseWormNat = v != null);
            parser.Add("priority", (v) => options.SetHighPriority = v != null);
            parser.Add("port:", (v) =>
            {
                int port;
                if (int.TryParse(v, out port))
                {
                    options.Port = port;
                }
            });
            parser.Add("ip:", (v) => options.IP = v);

            try
            {
                parser.Parse(args);
                WormNat prg = new WormNat(options);
                prg.Start();
            }
            catch { }
        }
    }
}
