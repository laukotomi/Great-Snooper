using GreatSnooper.Helpers;
using System;
using System.Net;
using System.Net.Cache;

namespace GreatSnooper.Classes
{
    public class WebDownload : WebClient
    {
        public WebDownload()
            : base()
        {
            this.Proxy = null;
            this.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
                request.Timeout = GlobalManager.WebRequestTimeout;
            return request;
        }
    }
}
