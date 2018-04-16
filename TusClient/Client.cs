namespace Tus.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;

    public class Client
    {
        private CancellationTokenSource cancelSource = new CancellationTokenSource();

        public Client()
        {
        }

        public delegate void UploadingEvent(long bytesTransferred, long bytesTotal);

        public delegate void DownloadingEvent(long bytesTransferred, long bytesTotal);

        public event UploadingEvent Uploading;

        public event DownloadingEvent Downloading;

        public void Cancel()
        {
            this.cancelSource.Cancel();
        }

        public string Create(string url, System.IO.FileInfo file, Dictionary<string, string> metadata = null)
        {
            if (metadata == null)
            {
                metadata = new Dictionary<string, string>();
            }

            if (!metadata.ContainsKey("filename"))
            {
                metadata["filename"] = file.Name;
            }

            return this.Create(url, file.Length, metadata);
        }

        public string Create(string url, long uploadLength, Dictionary<string, string> metadata = null)
        {
            var requestUri = new Uri(url);
            var client = new HttpClient();
            var request = new HttpRequest(url)
            {
                Method = "POST"
            };
            request.AddHeader("Tus-Resumable", "1.0.0");
            request.AddHeader("Upload-Length", uploadLength.ToString());
            request.AddHeader("Content-Length", "0");

            if (metadata == null)
            {
                metadata = new Dictionary<string, string>();
            }

            var metadatastrings = new List<string>();
            foreach (var meta in metadata)
            {
                string k = meta.Key.Replace(" ", string.Empty).Replace(",", string.Empty);
                string v = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(meta.Value));
                metadatastrings.Add(string.Format("{0} {1}", k, v));
            }

            request.AddHeader("Upload-Metadata", string.Join(",", metadatastrings.ToArray()));

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                if (response.Headers.ContainsKey("Location"))
                {
                    Uri locationUri;
                    if (Uri.TryCreate(response.Headers["Location"], UriKind.RelativeOrAbsolute, out locationUri))
                    {
                        if (!locationUri.IsAbsoluteUri)
                        {
                            locationUri = new Uri(requestUri, locationUri);
                        }
                        return locationUri.ToString();
                    }
                    else
                    {
                        throw new Exception("Invalid Location Header");
                    }
                }
                else
                {
                    throw new Exception("Location Header Missing");
                }
            }
            else
            {
                throw new Exception("CreateFileInServer failed. " + response.ResponseString);
            }
        }

        public void Upload(string url, System.IO.FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                this.Upload(url, fs);
            }
        }

        public void Upload(string url, System.IO.Stream fs)
        {
            var offest = this.GetFileOffset(url);
            var client = new HttpClient();
            System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1Managed();
            int chunkSize = (int)Math.Ceiling(3 * 1024.0 * 1024.0); // 3 mb

            if (offest == fs.Length)
            {
                this.Uploading?.Invoke((long)fs.Length, (long)fs.Length);
            }

            while (offest < fs.Length)
                {
                    fs.Seek(offest, SeekOrigin.Begin);
                    byte[] buffer = new byte[chunkSize];
                    var bytesRead = fs.Read(buffer, 0, chunkSize);

                    Array.Resize(ref buffer, bytesRead);
                    var sha1hash = sha.ComputeHash(buffer);

                    var request = new HttpRequest(url)
                    {
                        CancelToken = this.cancelSource.Token,
                        Method = "PATCH",
                        BodyBytes = buffer
                    };
                    request.AddHeader("Tus-Resumable", "1.0.0");
                    request.AddHeader("Upload-Offset", string.Format("{0}", offest));
                    request.AddHeader("Upload-Checksum", "sha1 " + Convert.ToBase64String(sha1hash));
                    request.AddHeader("Content-Type", "application/offset+octet-stream");

                    request.Uploading += delegate(long bytesTransferred, long bytesTotal)
                    {
                        this.Uploading?.Invoke((long)offest + bytesTransferred, (long)fs.Length);
                    };

                    try
                    {
                        var response = client.PerformRequest(request);

                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            offest += bytesRead;
                        }
                        else
                        {
                            throw new Exception("WriteFileInServer failed. " + response.ResponseString);
                        }
                    }
                    catch (IOException ex)
                    {
                        if (ex.InnerException.GetType() == typeof(System.Net.Sockets.SocketException))
                        {
                            var socketex = (System.Net.Sockets.SocketException)ex.InnerException;
                            if (socketex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
                            {
                                // retry by continuing the while loop but get new offset from server to prevent Conflict error
                                offest = this.GetFileOffset(url);
                            }
                            else
                            {
                                throw socketex;
                            }                            
                        }
                        else
                        {
                            throw;
                        }                        
                    }
                }
        }

        public HttpResponse Download(string url)
        {
            var client = new HttpClient();
            var request = new HttpRequest(url)
            {
                CancelToken = this.cancelSource.Token,
                Method = "GET"
            };

            request.Downloading += delegate(long bytesTransferred, long bytesTotal)
            {
                this.Downloading?.Invoke((long)bytesTransferred, (long)bytesTotal);
            };

            return client.PerformRequest(request);
        }

        public HttpResponse Head(string url)
        {
            var client = new HttpClient();
            var request = new HttpRequest(url)
            {
                Method = "HEAD"
            };
            request.AddHeader("Tus-Resumable", "1.0.0");

            try
            {
                return client.PerformRequest(request);
            }
            catch (TusException ex)
            {
                return new HttpResponse
                {
                    StatusCode = ex.Statuscode
                };
            }
        }

        public TusServerInfo GetServerInfo(string url)
        {
            var client = new HttpClient();
            var request = new HttpRequest(url)
            {
                Method = "OPTIONS"
            };
            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                // Spec says NoContent but tusd gives OK because of browser bugs
                response.Headers.TryGetValue("Tus-Resumable", out string version);
                response.Headers.TryGetValue("Tus-Version", out string supportedVersions);
                response.Headers.TryGetValue("Tus-Extension", out string extensions);

                response.Headers.TryGetValue("Tus-Max-Size", out string maxSize);
                return new TusServerInfo()
                {
                    Version = version,
                    SupportedVersions = supportedVersions,
                    Extensions = extensions,
                    MaxSize = string.IsNullOrEmpty(maxSize) ? long.Parse(maxSize) : 0,
                };
            }
            else
            {
                throw new Exception("getServerInfo failed. " + response.ResponseString);
            }
        }

        public bool Delete(string url)
        {
            var client = new HttpClient();
            var request = new HttpRequest(url)
            {
                Method = "DELETE"
            };
            request.AddHeader("Tus-Resumable", "1.0.0");

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private long GetFileOffset(string url)
        {
            var client = new HttpClient();
            var request = new HttpRequest(url)
            {
                Method = "HEAD"
            };
            request.AddHeader("Tus-Resumable", "1.0.0");

            var response = client.PerformRequest(request);
            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Headers.ContainsKey("Upload-Offset"))
                {
                    return long.Parse(response.Headers["Upload-Offset"]);
                }
                else
                {
                    throw new Exception("Offset Header Missing");
                }
            }
            else
            {
                throw new Exception("getFileOffset failed. " + response.ResponseString);
            }
        }
    } 
} 
