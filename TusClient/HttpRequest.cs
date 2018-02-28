namespace Tus.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class HttpRequest
    {
        public HttpRequest(string url)
        {
            this.Url = url;
            this.Method = "GET";
            this.Headers = new Dictionary<string, string>();
            this.BodyBytes = new byte[0];
        }

        public delegate void UploadingEvent(long bytesTransferred, long bytesTotal);

        public delegate void DownloadingEvent(long bytesTransferred, long bytesTotal);

        public event UploadingEvent Uploading;

        public event DownloadingEvent Downloading;

        public string Url { get; set; }

        public string Method { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public byte[] BodyBytes { get; set; }

        public CancellationToken CancelToken { get; set; }

        public string BodyText
        {
            get { return System.Text.Encoding.UTF8.GetString(this.BodyBytes); }
            set { this.BodyBytes = System.Text.Encoding.UTF8.GetBytes(value); }
        }

        public void AddHeader(string k, string v)
        {
            this.Headers[k] = v;
        }

        public void FireUploading(long bytesTransferred, long bytesTotal)
        {
            this.Uploading?.Invoke(bytesTransferred, bytesTotal);
        }

        public void FireDownloading(long bytesTransferred, long bytesTotal)
        {
            this.Downloading?.Invoke(bytesTransferred, bytesTotal);
        }
    }
}
