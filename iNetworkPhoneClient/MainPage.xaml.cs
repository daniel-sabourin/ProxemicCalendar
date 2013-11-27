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

using Microsoft.Phone.Tasks;

using GroupLab.iNetwork;
using GroupLab.iNetwork.Tcp;

namespace iNetworkPhoneClient
{
    public partial class MainPage : PhoneApplicationPage
    {

        private Connection _connection;
        private string _ipAddress = "127.0.0.1";
        private int _port = 12345;

        PhotoChooserTask photoChooserTask;
        CameraCaptureTask cameraCaptureTask;

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

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            cameraCaptureTask = new CameraCaptureTask();
            cameraCaptureTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            // to send a message 
            // Message msg = new Message("Name-of-Message");
            // msg.AddField("Name-of-Field", 0);
            // this._connection.SendMessage(msg);
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            cameraButton.Visibility = System.Windows.Visibility.Collapsed;
            galleryButton.Visibility = System.Windows.Visibility.Collapsed;

            if (e.TaskResult == TaskResult.OK)
            {
                //MessageBox.Show(e.ChosenPhoto.Length.ToString());

                //Code to display the photo on the page in an image control named myImage.
                System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.SetSource(e.ChosenPhoto);
                DisplayImage.Source = bmp;
            }
        }

        private void DisplayImage_Tap(object sender, GestureEventArgs e)
        {
            cameraButton.Visibility = System.Windows.Visibility.Visible;
            galleryButton.Visibility = System.Windows.Visibility.Visible;
        }

        private void cameraButton_Click(object sender, RoutedEventArgs e)
        {
            cameraCaptureTask.Show();
        }

        private void galleryButton_Click(object sender, RoutedEventArgs e)
        {
            photoChooserTask.Show();
        }

    }
}