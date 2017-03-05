namespace GreatSnooper.Classes
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Cache;

    using GreatSnooper.Helpers;

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
            {
                request.Timeout = GlobalManager.WebRequestTimeout;
            }
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            Debug.WriteLine("REQUEST: " + request.Method + " " + request.RequestUri);
            return response;
        }
    }
}