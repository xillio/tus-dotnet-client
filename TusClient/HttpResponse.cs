namespace Tus.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    public class HttpResponse
    {
        public HttpResponse()
        {
            this.Headers = new Dictionary<string, string>();
        }

        public byte[] ResponseBytes
        {
            get;
            set;
        }

        public string ResponseString
        {
            get
            {
                return System.Text.Encoding.UTF8.GetString(this.ResponseBytes);
            }
        }

        public HttpStatusCode StatusCode
        {
            get;
            set;
        }

        public Dictionary<string, string> Headers
        {
            get;
            set;
        }
    }
}
