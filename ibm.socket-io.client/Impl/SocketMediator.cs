using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net;
using System.Linq;
using IBM.SocketIO.Factories;
using IBM.Webclient;

namespace IBM.SocketIO.Impl
{
    public class SocketMediator : ISocketMediator
    {
        #region Private Members

        private IClientSocket websocketClient = null;
        private const string PROBE_RESPONSE_REGEX = @"{(.*?)}";
        private const string RESPONSE_REGEX = @"\[{(.*?)}\]";
        private readonly string probeUrl = "&transport=polling&t=";
        private readonly string websocketQueryParam = "&transport=websocket";
        private string baseWsUrl = null;
        private string baseHttpUrl = null;
        private string roomPath = null;
        private IClientSocketFactory socketFactory = null;
        private IHttpClientFactory httpFactory = null;

        /// <summary>
        /// Used to check connection every configured seconds
        /// </summary>
        System.Timers.Timer keepaliveTimer = null;

        #endregion

        #region Ctor

        public SocketMediator(string baseUrl)
        {
            var uri = new Uri(baseUrl);
            var host = uri.GetLeftPart(UriPartial.Authority);
            this.baseHttpUrl = "http" + host.TrimStart('w', 's') + "/socket.io/?EIO=3";
            this.baseWsUrl = uri.GetLeftPart(UriPartial.Authority) + "/socket.io/?EIO=3";
            this.roomPath = uri.AbsolutePath;

            this.keepaliveTimer = new System.Timers.Timer();
            this.keepaliveTimer.AutoReset = false;
            this.keepaliveTimer.Elapsed += KeepaliveTimer_Elapsed;
        }

        #endregion

        #region Private Properties

        private bool ClientConnectionMade { get; set; }

        private string SocketEndpointUrl { get; set; }

        private bool Disposed { get; set; }

        #endregion

        #region Public methods

        public async Task CloseAsync()
        {
            await this.websocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                "Shutting down", CancellationToken.None);
            this.OnConnectionClosed();
        }

        public async Task InitConnection(IHttpClientFactory factory, IClientSocketFactory socketFactory)
        {
            //Retained for reconnect later on if needed
            this.socketFactory = socketFactory;
            this.httpFactory = factory;

            // 1. Establish probe handshake

            #region Socket Handshake

            string sessionId = null;
            var probeTURl = this.baseHttpUrl + this.probeUrl
                + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            var websocketKey = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();

            string initialProbeResponse = null;
            var httpClient = factory.CreateHttpClient();

            using (var initialProbeRequest = new HttpRequestMessage(HttpMethod.Get, probeTURl.ToString()))
            {
                try
                {
                    using (var response = await httpClient.SendAsync(initialProbeRequest, CancellationToken.None))
                    {
                        response.EnsureSuccessStatusCode();
                        initialProbeResponse = await response.Content.ReadAsStringAsync();
                        var regex = new Regex(PROBE_RESPONSE_REGEX);
                        var match = regex.Match(initialProbeResponse);

                        var initialProbeJson = JObject.Parse(match.Groups[0].Value);

                        sessionId = initialProbeJson.GetValue("sid").ToString();
                        this.keepaliveTimer.Interval = initialProbeJson.GetValue("pingInterval").ToObject<int>();

                        probeTURl = this.baseHttpUrl + this.probeUrl
                            + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
                            + $"&sid={sessionId}";
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    throw ex;
                }
            }

            using (var roomRequestMessage = new HttpRequestMessage(HttpMethod.Post, probeTURl))
            {
                var roomRequest = $"15:40{this.roomPath},";
                roomRequestMessage.Content = new StringContent(roomRequest);
                //secondaryProbeRequest.Headers.Add("Content-type", "text/plain;charset=UTF-8");
                roomRequestMessage.Headers.Add("Accept", "*/*");
                roomRequestMessage.Headers.Add("DNT", "1");
                roomRequestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3642.0 Safari/537.36");
                roomRequestMessage.Headers.Add("Cookie", $"io={sessionId}");

                try
                {
                    using (var response = await httpClient.SendAsync(roomRequestMessage, CancellationToken.None))
                    {
                        response.EnsureSuccessStatusCode();
                        var ok = await response.Content.ReadAsStringAsync();
                        Debug.Assert(ok.Equals("ok"));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    throw ex;
                }
            }

            using (var protocolUpgradeRequest = new HttpRequestMessage(HttpMethod.Get, probeTURl))
            {
                protocolUpgradeRequest.Headers.Add("DNT", "1");
                protocolUpgradeRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3642.0 Safari/537.36");
                protocolUpgradeRequest.Headers.Add("Cookie", $"io={sessionId}");
                protocolUpgradeRequest.Headers.Add("Connection", "Upgrade");
                protocolUpgradeRequest.Headers.Add("Pragma", "no-cache");
                protocolUpgradeRequest.Headers.Add("Cache-control", "no-cache");
                protocolUpgradeRequest.Headers.Add("Upgrade", "websocket");
                protocolUpgradeRequest.Headers.Add("Sec-Websocket-Version", "13");
                protocolUpgradeRequest.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                protocolUpgradeRequest.Headers.Add("Sec-Websocket-Key", websocketKey);
                protocolUpgradeRequest.Headers.Add("Sec-Websocket-Extensions", "permessage-deflate; client_max_window_bits");

                try
                {
                    using (var response = await httpClient.SendAsync(protocolUpgradeRequest, CancellationToken.None))
                    {
                        if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
                        {
                            throw new HttpRequestException(
                                "Did not correctly receive protocol switch from server!");
                        }

                        var accept = response.Headers.GetValues("Sec-Websocket-Accept");

                        if (!accept.Any())
                        {
                            throw new HttpRequestException("Did not get Sec-Websocket-Accept header!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    throw ex;
                }
            }

            using (var finalHandshake = new HttpRequestMessage(HttpMethod.Get, probeTURl))
            {
                finalHandshake.Headers.Add("Accept", "*/*");
                finalHandshake.Headers.Add("DNT", "1");
                finalHandshake.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3642.0 Safari/537.36");
                finalHandshake.Headers.Add("Cookie", $"io={sessionId}");
                finalHandshake.Headers.Add("Accept-Encoding", "gzip, deflate, br");

                try
                {
                    using (var response = await httpClient.SendAsync(finalHandshake, CancellationToken.None))
                    {
                        response.EnsureSuccessStatusCode();
                        var finalResponse = await response.Content.ReadAsStringAsync();

                        if (!finalResponse.Equals($"15:40{this.roomPath},"))
                        {
                            throw new HttpRequestException($"Final handshake {finalResponse} response was not expected!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw ex;
                }
            }

            #endregion

            // 2. Upgrade connection and send ping

            this.SocketEndpointUrl = this.baseWsUrl
                            + websocketQueryParam
                            + $"&sid={sessionId}";

            this.websocketClient = socketFactory.CreateSocketClient();

            try
            {
                await this.websocketClient.ConnectAsync(new Uri(this.SocketEndpointUrl), CancellationToken.None);
            }
            catch (Exception connectException)
            {
                Console.Error.WriteLine(connectException);
                throw connectException;
            }

            await SendConnectionProbe();
            this.keepaliveTimer.Start();
        }

        /// <summary>
        /// Emit the specified eventName and callback.
        /// </summary>
        /// <returns>The emit.</returns>
        /// <param name="eventName">The name of the event to emit</param>
        /// <param name="callback">The callback to call when complete</param>
        public async Task Emit(string eventName, Action<string> callback)
        {
            if(!this.ClientConnectionMade)
            {
                await Task.Delay(5000); //In case we're reconnecting
            }

            string rootEvent = null;

            using (var writer = new StringWriter())
            {
                var array = new JArray(eventName, null);
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, array);
                rootEvent = writer.ToString();
            }

            var fullEvent = $"42{this.roomPath},0{rootEvent}";

            var eventBytes = Encoding.UTF8.GetBytes(fullEvent);
            var requestSegment = new ArraySegment<byte>(eventBytes);

            await this.websocketClient.SendAsync(requestSegment, WebSocketMessageType.Text, true, CancellationToken.None);
            byte[] result = await this.ReceiveAsync(CancellationToken.None);

            var resultAsString = Encoding.UTF8.GetString(result);
            var resultPrefix = $"43{this.roomPath},0[";

            resultAsString = resultAsString.Substring(resultPrefix.Length);
            resultAsString = resultAsString.Remove(resultAsString.Length - 1, 1);
            callback(resultAsString);
        }

        #endregion

        #region Protected Methods

        protected virtual void OnConnectionClosed()
        {
            this.ClientConnectionMade = false;
            this.keepaliveTimer.Stop();
        }

        #endregion

        #region Private Methods

        private async Task SendConnectionProbe(bool keepaliveProbe = false)
        {
            var probeEvent = Encoding.UTF8.GetBytes("2probe");
            var requestSegment = new ArraySegment<byte>(probeEvent);
            await this.websocketClient.SendAsync(requestSegment, WebSocketMessageType.Text, true, CancellationToken.None);

            string probeResponse = null;

            if(!keepaliveProbe)
            {
                probeResponse = "3probe";
            }
            else
            {
                probeResponse = "3";
            }

            byte[] result = await this.ReceiveAsync(CancellationToken.None);

            var actualProbeResponse = Encoding.UTF8.GetString(result);
            if (!probeResponse.Equals(actualProbeResponse))
            {
                throw new HttpRequestException("2probe response was invalid!");
            }

            var connectionUpgrade = "5";
            var upgradeSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(connectionUpgrade));
            await this.websocketClient.SendAsync(upgradeSegment, WebSocketMessageType.Text, true, CancellationToken.None);
            this.ClientConnectionMade = true;
        }

        private async Task<byte[]> ReceiveAsync(CancellationToken token)
        {
            using (var stream = new MemoryStream())
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new Byte[8192]);
                WebSocketReceiveResult result = null;

                do
                {
                    result = await this.websocketClient.ReceiveAsync(buffer, CancellationToken.None);
                    stream.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                stream.Seek(0, SeekOrigin.Begin);

                return stream.ToArray();
            }
        }

        private async Task PollConnection()
        {
            try
            {
                Console.WriteLine("Polling connection status");
                await this.SendConnectionProbe(true);
                Console.WriteLine("Client connected; no work to do");
            }
            catch (Exception)
            {
                Console.WriteLine("Client disconnected; attempting to reconnect");

                if (this.ClientConnectionMade)
                {
                    this.ClientConnectionMade = false;
                }

                try
                {
                    await this.websocketClient.CloseAsync(WebSocketCloseStatus.EndpointUnavailable,
                    "Client was disconnected", CancellationToken.None);
                }
                catch { }


                Console.WriteLine("Attempting to reconnect client");

                try
                {
                    await this.InitConnection(this.httpFactory, this.socketFactory);
                    Console.WriteLine("Client reconnected");
                }
                catch (Exception) { }
            }
        }

        async void KeepaliveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Timer elapsed");

            //Timer events are queued so another event could run after disposing
            if(this.Disposed)
            {
                return;
            }

            await this.PollConnection();
            this.keepaliveTimer.Start();
        }

        public void Dispose()
        {
            this.keepaliveTimer.Stop();
            this.keepaliveTimer.Dispose();
            this.websocketClient?.Dispose();

            this.Disposed = true;
        }

        #endregion
    }
}
