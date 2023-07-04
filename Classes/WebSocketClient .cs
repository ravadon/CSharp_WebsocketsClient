using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketsClient.Classes
{
    public class WebSocketCloseEventArgs : EventArgs
    {
        public WebSocketCloseStatus CloseStatus { get; }
        public string CloseStatusDescription { get; }

        public WebSocketCloseEventArgs(WebSocketCloseStatus closeStatus, string closeStatusDescription)
        {
            CloseStatus = closeStatus;
            CloseStatusDescription = closeStatusDescription;
        }
    }
    internal class WebSocketClient : IDisposable
    {
        public readonly ClientWebSocket _clientWebSocket;
        private readonly CancellationToken _cancellationToken;
        private const int ReceiveBufferSize = 1024;
        private bool bClientInitiatedClose = false;

        public event EventHandler Connected;
        public event EventHandler<string> MessageSent;
        public event EventHandler<ArraySegment<byte>> RawMessageReceived;
        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<WebSocketCloseEventArgs> Closed;
        public event EventHandler<Exception> ConnectionError;

        public bool IsConnected => _clientWebSocket.State == WebSocketState.Open;


        public WebSocketClient()
        {
            _clientWebSocket = new ClientWebSocket();
            _cancellationToken = CancellationToken.None;
        }

        public async Task ConnectAsync(string uri)
        {
            try
            {
                await _clientWebSocket.ConnectAsync(new Uri(uri), _cancellationToken);
                Connected?.Invoke(this, EventArgs.Empty);
                await StartListeningAsync();
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, ex);
            }
        }

        private async Task StartListeningAsync()
        {
            var receiveBuffer = new ArraySegment<byte>(new byte[ReceiveBufferSize]);

            try
            {
                while (_clientWebSocket.State == WebSocketState.Open)
                {
                    var received = await _clientWebSocket.ReceiveAsync(receiveBuffer, _cancellationToken);

                    if (received.MessageType == WebSocketMessageType.Text)
                    {
                        RawMessageReceived?.Invoke(this, receiveBuffer);
                        string message = Encoding.UTF8.GetString(receiveBuffer.Array, 0, received.Count);
                        TextMessageReceived?.Invoke(this, message);
                    }
                    else if (received.MessageType == WebSocketMessageType.Close)
                    {
                        await _clientWebSocket.CloseAsync(received.CloseStatus.Value, received.CloseStatusDescription, _cancellationToken);
                        if (!bClientInitiatedClose)
                        {
                            Closed?.Invoke(this, new WebSocketCloseEventArgs(received.CloseStatus.Value, received.CloseStatusDescription));
                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, ex);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            var messageBuffer = Encoding.UTF8.GetBytes(message);
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, _cancellationToken);
            MessageSent?.Invoke(this, message);
        }

        public async Task SendRawMessageAsync(ArraySegment<byte> messageBuffer, WebSocketMessageType messageType)
        {
            await _clientWebSocket.SendAsync(messageBuffer, messageType, true, _cancellationToken);
        }

        public async Task CloseAsync()
        {
            if (_clientWebSocket.State != WebSocketState.Closed && _clientWebSocket.State != WebSocketState.Aborted)
            {
                var closeStatus = WebSocketCloseStatus.NormalClosure;
                var closeDescription = "Client initiated close";
                bClientInitiatedClose = true;
                await _clientWebSocket.CloseAsync(closeStatus, closeDescription, _cancellationToken);
                Closed?.Invoke(this, new WebSocketCloseEventArgs(closeStatus, closeDescription));
            }
        }

        public void Dispose()
        {
            _clientWebSocket.Dispose();
        }
    }
}
