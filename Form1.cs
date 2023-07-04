using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebsocketsClient.Classes;

namespace WebsocketsClient
{
    public partial class Form1 : Form
    {
        WebSocketClient webSocketClient;
        public Form1()
        {
            InitializeComponent();
            this.FormClosing += OnApplicationQuit;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnApplicationQuit);
        }

        private void WebSocketClient_Connected(object sender, EventArgs e)
        {
            conStatus.Text = "Connected";
            ConnectBtn.Enabled = false;
            DisconnectBtn.Enabled = true;
            AddLog("Connected.");
        }

        private void WebSocketClient_MessageSent(object sender, string message)
        {
            AddMessageSent(message);
        }

        private void WebSocketClient_RawMessageReceived(object sender, ArraySegment<byte> messageBuffer)
        {
            // Handle raw message received event
        }

        private void WebSocketClient_TextMessageReceived(object sender, string message)
        {
            AddMessageReceived(message);
        }

        private void WebSocketClient_Closed(object sender, WebSocketCloseEventArgs e)
        {
            conStatus.Text = "Connection closed";
            DisconnectBtn.Enabled = false;
            ConnectBtn.Enabled = true;
            AddLog("Connection closed, Reason: " + e.CloseStatusDescription + " Status: " + e.CloseStatus);
        }

        private void WebSocketClient_ConnectionError(object sender, Exception ex)
        {
            conStatus.Text = "Connection error";
            DisconnectBtn.Enabled = false;
            AddLog("Connection closed: " + ex.Message);
            ConnectBtn.Enabled = true;
        }



        private void AddLog(string log)
        {
            LogTextBox.AppendText(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " : " + log + "\n");
            LogTextBox.ScrollToCaret();
        }
        

        private void AddMessageReceived(string message)
        {
            messagesFromServerTextBox.AppendText(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " : " + message + "\n");
        }

        private void AddMessageSent(string message)
        {
            sentMesssagesTextBox.AppendText(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " : " + message + "\n");
            sentMesssagesTextBox.ScrollToCaret();
        }



        //private async Task ConnectBtn_ClickAsync(object sender, EventArgs e)
        private async void ConnectBtn_ClickAsync(object sender, EventArgs e)
        {
            if (webSocketClient != null && webSocketClient.IsConnected)
            {
                AddLog("Already connected");
                return;
            }
            AddLog("Connecting...");
            ConnectBtn.Enabled = false;
            if (webSocketClient != null)
            {
                webSocketClient.Dispose();
            }
            webSocketClient = new WebSocketClient();

            webSocketClient.Connected += WebSocketClient_Connected;
            webSocketClient.MessageSent += WebSocketClient_MessageSent;
            webSocketClient.RawMessageReceived += WebSocketClient_RawMessageReceived;
            webSocketClient.TextMessageReceived += WebSocketClient_TextMessageReceived;
            webSocketClient.Closed += WebSocketClient_Closed;
            webSocketClient.ConnectionError += WebSocketClient_ConnectionError;
            await webSocketClient.ConnectAsync(urlTextBox.Text);
        }

        private async void DisconnectBtn_Click(object sender, EventArgs e)
        {
            if (webSocketClient == null || !webSocketClient.IsConnected)
            {
                return;
            }
            DisconnectBtn.Enabled = false;
            await webSocketClient.CloseAsync();
        }

        private async void sendMessageBtn_Click(object sender, EventArgs e)
        {
            if (webSocketClient == null || !webSocketClient.IsConnected)
            {
                AddLog("You need to connect to a server before sending messages");
                return;
            }
            if (messageTextBox.Text == "")
            {
                AddLog("You cannot send empty messages");
                return;
            }
            await webSocketClient.SendMessageAsync(messageTextBox.Text);
        }

        private async void OnApplicationQuit(object sender, FormClosingEventArgs e)
        {
            if (webSocketClient == null)
            {
                return;
            }
            await webSocketClient.CloseAsync();
        }
    }
}
