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
using System.IO;
using System.Windows.Media.Imaging;

using System.Windows.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;
using Microsoft.Devices;


namespace iNetworkPhoneClient
{
    public partial class MainPage : PhoneApplicationPage
    {
        Microphone microphone = Microphone.Default;
        byte[] buffer;

        private Connection _connection;
        private string _ipAddress = "192.168.1.98";
        private int _port = 12345;

        PhotoChooserTask photoChooserTask;
        CameraCaptureTask cameraCaptureTask;

        ListBoxItem emptyMessage = new ListBoxItem() { IsEnabled = false, Content = new TextBlock() { Text = "There doesn't seem to be anything here" } };

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
            Deployment.Current.Dispatcher.BeginInvoke(() =>
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
                            case "EventItem":
                                // do something here
                                TransferableEvent tEvent = msg.GetField("eventItem", typeof(TransferableEvent)) as TransferableEvent;
                                eventListBox.Items.Add(new EventItem(tEvent) { Width = eventListBox.ActualWidth });

                                VibrateController vibrate = VibrateController.Default;
                                vibrate.Start(TimeSpan.FromSeconds(0.4));

                                break;

                        }
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message + "\n" + e.StackTrace);
                }
            });
        }

        #endregion
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            InitializeConnection();
            InitializeMicrophone();
            InitializeChooserTasks();
        }

        #region Image Chooser
        private void InitializeChooserTasks()
        {
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            cameraCaptureTask = new CameraCaptureTask();
            cameraCaptureTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            cameraButton.Opacity = 0;
            galleryButton.Opacity = 0;

            galleryButton.Visibility = System.Windows.Visibility.Collapsed;
            cameraButton.Visibility = System.Windows.Visibility.Collapsed;

            // Connection gets dropped when you enter camera/photo chooser
            InitializeConnection();

            if (e.TaskResult == TaskResult.OK)
            {
                infoText.Visibility = System.Windows.Visibility.Collapsed;

                System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.SetSource(e.ChosenPhoto);
                DisplayImage.Source = bmp;
                DisplayImage.Opacity = 1;
            }
        }

        private void DisplayImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            galleryButton.Visibility = System.Windows.Visibility.Visible;
            cameraButton.Visibility = System.Windows.Visibility.Visible;

            Storyboard storyboard = new Storyboard();
            DoubleAnimation anim = new DoubleAnimation();
            anim.From = cameraButton.Opacity;
            anim.To = 1;
            anim.Duration = TimeSpan.FromSeconds(0.5);

            Storyboard.SetTarget(anim, cameraButton);
            Storyboard.SetTargetProperty(anim, new PropertyPath(Button.OpacityProperty));
            storyboard.Children.Add(anim);

            DoubleAnimation anim2 = new DoubleAnimation();
            anim2.From = galleryButton.Opacity;
            anim2.To = 1;
            anim2.Duration = TimeSpan.FromSeconds(0.5);

            Storyboard.SetTarget(anim2, galleryButton);
            Storyboard.SetTargetProperty(anim2, new PropertyPath(Button.OpacityProperty));
            storyboard.Children.Add(anim2);

            storyboard.Begin();
        }

        private void cameraButton_Click(object sender, RoutedEventArgs e)
        {
            cameraCaptureTask.Show();
        }

        private void galleryButton_Click(object sender, RoutedEventArgs e)
        {
            photoChooserTask.Show();
        }
        #endregion

        #region BlowDetection
        bool BlowTriggered = false;
        private void InitializeMicrophone()
        {
            // Timer to simulate the XNA Game Studio game loop (Microphone is from XNA Game Studio)
            DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromMilliseconds(33);
            dt.Tick += delegate { try { FrameworkDispatcher.Update(); } catch { } };
            dt.Start();

            microphone.BufferReady += delegate(object sender, EventArgs e)
            {
                microphone.GetData(buffer);

                //http://social.msdn.microsoft.com/Forums/en-US/ed1af567-166d-4b0c-8ab6-d9db6bd1f957/how-to-detect-the-volume-of-incoming-signal-into-kinect-microphone?forum=kinectsdkaudioapi
                long totalSquare = 0;
                for (int i = 0; i < buffer.Length; i += 2)
                {
                    short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                    totalSquare += sample * sample;
                }
                long meanSquare = 2 * totalSquare / buffer.Length;
                double rms = Math.Sqrt(meanSquare);
                double volume = rms / 32768.0;

                if (volume > 0.5)
                {
                    if (!BlowTriggered)
                    {
                        BlowTriggered = true;
                        System.Diagnostics.Debug.WriteLine("Ahoy! Blow detected at volume " + volume);

                        object obj = eventListBox.SelectedItem;
                        if (obj != null)
                        {
                            EventItem item = (EventItem)eventListBox.SelectedItem;
                            SendItem(item);
                        }
                    }
                }
                else
                {
                    BlowTriggered = false;
                }
            };
        }
        #endregion

        public void SendItem(EventItem item)
        {
            Message message = new Message("EventItem");
            message.AddField("eventItem", item.CreateTransferableEvent());
            _connection.SendMessage(message);

            VibrateController vibrate = VibrateController.Default;
            vibrate.Start(TimeSpan.FromSeconds(0.4));

            Storyboard storyboard = new Storyboard();
            DoubleAnimation anim = new DoubleAnimation();
            anim.From = item.ActualHeight;
            anim.To = 0;
            anim.Duration = TimeSpan.FromSeconds(0.5);
            anim.Completed += delegate(object sender, EventArgs e) {
                eventListBox.Items.Remove(item);
            };

            Storyboard.SetTarget(anim, item);
            Storyboard.SetTargetProperty(anim, new PropertyPath(EventItem.HeightProperty));

            storyboard.Children.Add(anim);

            storyboard.Begin();
        }

        private void eventListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object o in e.AddedItems)
            {
                EventItem ei = (EventItem)o;
                ei.Selected();
            }

            foreach (object o in e.RemovedItems)
            {
                EventItem ei2 = (EventItem)o;
                ei2.Deselected();
            }

        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime dt = DateTime.Parse(datePicker.ValueString + " " + timePicker.ValueString);

            System.Windows.Media.ImageSource bmp = DisplayImage.Source;

            if (DisplayImage.Opacity == 0)
                bmp = null;

            eventListBox.Items.Add(new EventItem(eventTitleBox.Text, dt, bmp) { Width = eventListBox.ActualWidth });

            // Clear Previous
            eventTitleBox.Text = "";
            datePicker.Value = DateTime.Now;
            timePicker.Value = DateTime.Now;

            DisplayImage.Opacity = 0;
            infoText.Visibility = System.Windows.Visibility.Visible;

            pivotControl.SelectedIndex = 0;
        }

        private void eventListBox_LayoutUpdated(object sender, EventArgs e)
        {
            if (eventListBox.Items.Count == 0)
            {
                eventListBox.Items.Add(emptyMessage);
            }
            else if (eventListBox.Items.Count > 1 && eventListBox.Items.Contains(emptyMessage))
            {
                eventListBox.Items.Remove(emptyMessage);
            }
        }

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivotControl.SelectedIndex == 0)
            {
                microphone.BufferDuration = TimeSpan.FromMilliseconds(1000);
                buffer = new byte[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];
                microphone.Start();
            }
            else
            {
                if (microphone.State == MicrophoneState.Started)
                {
                    microphone.Stop();
                }
            }
        }


    }
}