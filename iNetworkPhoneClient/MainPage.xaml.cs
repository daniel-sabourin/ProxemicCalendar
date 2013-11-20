using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using GroupLab.iNetwork;
using GroupLab.iNetwork.Tcp;

namespace iNetworkPhoneClient
{
    public partial class MainPage : PhoneApplicationPage
    {

        private Connection _connection;
        private string _ipAddress = "127.0.0.1";
        private int _port = 12345;

        #region iNetwork Methods

        private void InitializeConnection()
        {
            // connect to the server
            this._connection = new Connection(this._ipAddress, this._port);
            this._connection.Connected += new ConnectionEventHandler(OnConnected);
            this._connection.Start();
        }

        void OnConnected(object sender, ConnectionEventArgs e)
        {
            this._connection.MessageReceived += new ConnectionMessageEventHandler(OnMessageReceived);
        }

        private void OnMessageReceived(object sender, Message msg)
        {
            try
            {
                if (msg != null)
                {
                    switch (msg.Name)
                    {
                        default:
                            // don't do anything
                            break;
                        case "Name-of-Message":
                            // do something here
                            break;

                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message + "\n" + e.StackTrace);
            }

        }

        #endregion

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            InitializeConnection();

            // to send a message 
            // Message msg = new Message("Name-of-Message");
            // msg.AddField("Name-of-Field", 0);
            // this._connection.SendMessage(msg);
        }
    }
}