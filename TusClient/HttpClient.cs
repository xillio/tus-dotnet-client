namespace Tus.Sdk
{
    using System;
    using System.IO;
    using System.Net;

    public class HttpClient
    {
        public HttpResponse PerformRequest(HttpRequest request)
        {
            try
            {
                HttpWebRequest webRequest;
                byte[] buffer;
                long contentlength;
                using (var instream = new MemoryStream(request.BodyBytes))
                {
                    webRequest = (HttpWebRequest)WebRequest.Create(request.Url);
                    webRequest.AutomaticDecompression = DecompressionMethods.GZip;

                    webRequest.Timeout = System.Threading.Timeout.Infinite;
                    webRequest.ReadWriteTimeout = System.Threading.Timeout.Infinite;
                    webRequest.Method = request.Method;
                    webRequest.KeepAlive = false;

                    ServicePoint currentServicePoint = webRequest.ServicePoint;
                    currentServicePoint.Expect100Continue = false;

                    // SEND
                    request.FireUploading(0, 0);
                    buffer = new byte[4096];
                    contentlength = 0;
                    int byteswritten = 0;
                    long totalbyteswritten = 0;

                    contentlength = (long)instream.Length;
                    webRequest.AllowWriteStreamBuffering = false;
                    webRequest.ContentLength = instream.Length;

                    foreach (var header in request.Headers)
                    {
                        switch (header.Key)
                        {
                            case "Content-Length":
                                webRequest.ContentLength = long.Parse(header.Value);
                                break;
                            case "Content-Type":
                                webRequest.ContentType = header.Value;
                                break;
                            default:
                                webRequest.Headers.Add(header.Key, header.Value);
                                break;
                        }
                    }

                    if (request.BodyBytes.Length > 0)
                    {
                        using (System.IO.Stream requestStream = webRequest.GetRequestStream())
                        {
                            instream.Seek(0, SeekOrigin.Begin);
                            byteswritten = instream.Read(buffer, 0, buffer.Length);

                            while (byteswritten > 0)
                            {
                                totalbyteswritten += byteswritten;

                                request.FireUploading(totalbyteswritten, contentlength);

                                requestStream.Write(buffer, 0, byteswritten);

                                byteswritten = instream.Read(buffer, 0, buffer.Length);

                                request.CancelToken.ThrowIfCancellationRequested();
                            }
                        }
                    }
                }

                request.FireDownloading(0, 0);

                HttpWebResponse httpResponse = (HttpWebResponse)webRequest.GetResponse();

                contentlength = (long)httpResponse.ContentLength;

                buffer = new byte[16 * 1024];
                HttpResponse response;
                using (var outstream = new MemoryStream())
                {
                    using (Stream responseStream = httpResponse.GetResponseStream())
                    {
                        int bytesread = responseStream.Read(buffer, 0, buffer.Length);
                        long totalbytesread = 0;

                        while (bytesread > 0)
                        {
                            totalbytesread += bytesread;
                            request.FireDownloading(totalbytesread, contentlength);
                            outstream.Write(buffer, 0, bytesread);
                            bytesread = responseStream.Read(buffer, 0, buffer.Length);
                            request.CancelToken.ThrowIfCancellationRequested();
                        }
                    }

                    response = new HttpResponse
                    {
                        ResponseBytes = outstream.ToArray(),
                        StatusCode = httpResponse.StatusCode
                    };
                }

                foreach (string headerName in httpResponse.Headers.Keys)
                {
                    response.Headers[headerName] = httpResponse.Headers[headerName];
                }

                return response;
            }
            catch (OperationCanceledException cancelEx)
            {
                throw new TusException(cancelEx);
            }
            catch (WebException ex)
            {
                throw new TusException(ex);
            }
        }
    }
}