using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;

using GroupLab.iNetwork;
using GroupLab.iNetwork.Tcp;
using System.IO;
using Microsoft.Surface.Presentation.Controls;


namespace iNetworkClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Connection _connection;
        private string _ipAddress = "127.0.0.1";
        private int _port = 12345;

        private KinectSensor sensor;

        private float _playerDepth;

        public float PlayerDepth
        {
            get { return _playerDepth; }
            set
            {
                _playerDepth = value;

                if (value > 3.5)
                    PlayerState = CalendarEvent.State.Far;
                else if (value <= 3.5 && value >= 1.5)
                    PlayerState = CalendarEvent.State.Medium;
                else if (value < 1.5)
                    PlayerState = CalendarEvent.State.Close;
            }
        }

        private CalendarEvent.State _playerState;

        public CalendarEvent.State PlayerState
        {
            get { return _playerState; }
            set
            {
                if (PlayerState != value)
                {
                    _playerState = value;
                    //Console.WriteLine(value);

                    ChangeAllCalendarEventStates(value);
                }


            }
        }


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
            this.Dispatcher.Invoke(new Action(delegate()
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

                                MainScatterView.Items.Add(new CalendarEvent(tEvent.EventName, CreateImageFromFile("../../Resources/Koala.jpg"), DateTime.Now).CreateScatterViewItem());

                                break;

                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + "\n" + e.StackTrace);
                }
            }));

        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            InitializeConnection();
            InitializeBackgroundMovie();

            MainScatterView.Items.Add(new CalendarEvent("Doctor", CreateImageFromFile("../../Resources/Koala.jpg"), DateTime.Now).CreateScatterViewItem());
            MainScatterView.Items.Add(new CalendarEvent("Vet Appt", CreateImageFromFile("../../Resources/birds.png"), DateTime.Now).CreateScatterViewItem());
            MainScatterView.Items.Add(new CalendarEvent("Globalfest", CreateImageFromFile("../../Resources/fireworks.png"), DateTime.Now).CreateScatterViewItem());
            MainScatterView.Items.Add(new CalendarEvent("Vet Appt", CreateImageFromFile("../../Resources/dog.png"), DateTime.Now).CreateScatterViewItem());
        }

        private Image CreateImageFromFile(string path)
        {
            Image myImage3 = new Image();
            BitmapImage bi3 = new BitmapImage();
            bi3.BeginInit();
            bi3.UriSource = new Uri(path, UriKind.Relative);
            bi3.EndInit();
            myImage3.Stretch = Stretch.Fill;
            myImage3.Source = bi3;

            return myImage3;
        }

        private void InitializeBackgroundMovie()
        {
            // Code for the background movie
            mediaElement.LoadedBehavior = MediaState.Manual;
            mediaElement.SpeedRatio = 1;
            mediaElement.MediaEnded += delegate(object sender, RoutedEventArgs e)
            {
                mediaElement.Stop();
                mediaElement.Play();
            };
            mediaElement.Source = new Uri(Environment.CurrentDirectory + "/../../Resources/background.mov");
            mediaElement.Play();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            #region Kinect Setup

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                TransformSmoothParameters parameters = new TransformSmoothParameters
                {
                    Smoothing = 0.7f,
                    Correction = 0.3f,
                    Prediction = 0.4f,
                    JitterRadius = 1.0f,
                    MaxDeviationRadius = 0.5f
                };

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable(parameters);

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    SkeletonViz.Sensor = this.sensor;
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                Console.WriteLine("No Kinect Found");
            }

            #endregion
        }

                /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            SkeletonViz.DrawSkeletons(skeletons);

            if (sensor.SkeletonStream.TrackingMode == SkeletonTrackingMode.Default)
            {
                Skeleton playerSkeleton = (from s in skeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();

                if (playerSkeleton != null)
                {
                    Joint j = playerSkeleton.Joints[JointType.ShoulderCenter];

                    PlayerDepth = j.Position.Z;
                }
                else
                {
                    PlayerDepth = 9001;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Content.ToString())
            {
                case "Close":
                    ChangeAllCalendarEventStates(CalendarEvent.State.Close);
                    break;
                case "Medium":
                    ChangeAllCalendarEventStates(CalendarEvent.State.Medium);
                    break;
                case "Far":
                    ChangeAllCalendarEventStates(CalendarEvent.State.Far);
                    break;
                default:
                    break;
            }
        }

        private void ChangeAllCalendarEventStates(CalendarEvent.State state)
        {
            foreach (CalendarEvent ce in GetAllCalendarEvents())
                ce.EventState = state;
        }

        private List<CalendarEvent> GetAllCalendarEvents()
        {
            List<CalendarEvent> list = new List<CalendarEvent>();

            foreach (object o in MainScatterView.Items)
            {
                try
                {
                    CalendarEvent ce = (CalendarEvent)((ScatterViewItem)o).Content;
                    list.Add(ce);
                }
                catch { }
            }

            return list;
        }


    }
}
