using System;
using System.Net;

namespace MySnooper
{
    public class WebDownload : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = 15000; // 10 seconds
            }
            return request;
        }
    }
}
