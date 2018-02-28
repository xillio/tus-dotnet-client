namespace Tus.Sdk
{
    using System;

    public class TusServerInfo
    {
        public string Version { get; set; }

        public string SupportedVersions { get; set; }

        public string Extensions { get; set; }

        public long MaxSize { get; set; }

        public bool SupportsDelete
        {
            get { return this.Extensions.Contains("termination"); }
        }
    }
}
