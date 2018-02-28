namespace Tus.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    public class TusException : WebException
    {
        public TusException(TusException ex, string msg)
            : base(string.Format("{0}{1}", msg, ex.Message), ex, ex.Status, ex.Response)
        {
            this.OriginalException = ex;

            this.Statuscode = ex.Statuscode;
            this.StatusDescription = ex.StatusDescription;
            this.ResponseContent = ex.ResponseContent;
        }

        public TusException(OperationCanceledException ex)
            : base(ex.Message, ex, WebExceptionStatus.RequestCanceled, null)
        {
            this.OriginalException = null;
        }

        public TusException(WebException ex, string msg = "")
            : base(string.Format("{0}{1}", msg, ex.Message), ex, ex.Status, ex.Response)
        {
            this.OriginalException = ex;

            HttpWebResponse webresp = (HttpWebResponse)ex.Response;

            if (webresp != null)
            {
                this.Statuscode = webresp.StatusCode;
                this.StatusDescription = webresp.StatusDescription;

                StreamReader readerS = new StreamReader(webresp.GetResponseStream());

                dynamic resp = readerS.ReadToEnd();

                readerS.Close();

                this.ResponseContent = resp;
            }
        }

        public string ResponseContent { get; set; }

        public HttpStatusCode Statuscode { get; set; }

        public string StatusDescription { get; set; }

        public WebException OriginalException { get; set; }

        public string FullMessage
        {
            get
            {
                var bits = new List<string>();
                if (this.Response != null)
                {
                    bits.Add(string.Format("URL:{0}", this.Response.ResponseUri));
                }

                bits.Add(this.Message);
                if (this.Statuscode != HttpStatusCode.OK)
                {
                    bits.Add(string.Format("{0}:{1}", this.Statuscode, this.StatusDescription));
                }

                if (!string.IsNullOrEmpty(this.ResponseContent))
                {
                    bits.Add(this.ResponseContent);
                }

                return string.Join(Environment.NewLine, bits.ToArray());
            }
        }
    }
}
