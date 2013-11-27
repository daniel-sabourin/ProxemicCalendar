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
        private string _ipAddress = "10.11.94.40";
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
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            cameraButton.Visibility = System.Windows.Visibility.Collapsed;
            galleryButton.Visibility = System.Windows.Visibility.Collapsed;

            if (e.TaskResult == TaskResult.OK)
            {
                System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.SetSource(e.ChosenPhoto);
                DisplayImage.Source = bmp;
            }
        }


        private void DisplayImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
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

        public void SendItem(EventItem ei)
        {
            Message message = new Message("EventItem");
            message.AddField("eventItem", ei.CreateTransferableEvent());
            _connection.SendMessage(message);
        }

        private void eventListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object o in e.AddedItems)
            {
                EventItem ei = (EventItem)o;
                ei.Selected();

                SendItem(ei);
            }

            foreach (object o in e.RemovedItems)
            {
                EventItem ei2 = (EventItem)o;
                ei2.Deselected();
            }


        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/Background.png", UriKind.Relative));

            eventListBox.Items.Add(new EventItem("Doctor", DateTime.Now, bmp) { Width = eventListBox.ActualWidth });
            eventListBox.Items.Add(new EventItem("Doctor", DateTime.Now, bmp) { Width = eventListBox.ActualWidth });
            eventListBox.Items.Add(new EventItem("Doctor", DateTime.Now, bmp) { Width = eventListBox.ActualWidth });
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.ImageSource bmp = DisplayImage.Source;        
            eventListBox.Items.Add(new EventItem(eventTitleBox.Text, DateTime.Now, bmp) { Width = eventListBox.ActualWidth });

            eventTitleBox.Text = "";
            eventTimeBox.Text = "";

            pivotControl.SelectedIndex = 0;
        }

    }
}