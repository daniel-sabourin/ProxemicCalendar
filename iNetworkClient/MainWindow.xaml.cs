﻿using System;
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
using System.Globalization;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Timers;
using Fizbin.Kinect.Gestures;
using Fizbin.Kinect.Gestures.Segments;


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
        private GestureController gestureController;

        private ScatterViewItem HoveredItem;
        private Timer HoverTimer;

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

                    ChangeAllCalendarEventStates(value);

                    if (value == CalendarEvent.State.Medium || value == CalendarEvent.State.Close)
                    {
                        SetStoryboardStatus(false);

                        trackingEllipse.Opacity = 1;

                        int cellSize = 286;

                        int numberOfRows = (int)MainScatterView.ActualHeight / cellSize;
                        int numberOfCols = (int)MainScatterView.ActualWidth / cellSize;

                        for (int i = 0; i < MainScatterView.Items.Count; i++)
                        {
                            int rowPos = i / numberOfCols;
                            int colPos = i % numberOfCols;

                            int x = cellSize * colPos + (cellSize / 2);
                            int y = cellSize * rowPos + (cellSize / 2);

                            ScatterViewItem svi = (ScatterViewItem)MainScatterView.Items[i];
                            AnimateScatterViewToPoint(svi, new Point(x, y));

                        }
                    }
                    else
                    {
                        //DisableTracking();
                        InSelectionMode = false;

                        UpdateStoryboards();
                        SetStoryboardStatus(true);
                    }

                    if (value == CalendarEvent.State.Close)
                    {
                        DoubleAnimation skelAnimation = new DoubleAnimation();
                        skelAnimation.From = SkeletonViz.Opacity;
                        skelAnimation.To = 0.4;
                        skelAnimation.Duration = TimeSpan.FromSeconds(1);
                        SkeletonViz.BeginAnimation(SkeletonVisualizer.OpacityProperty, skelAnimation);
                    }
                    else
                    {
                        DoubleAnimation skelAnimation = new DoubleAnimation();
                        skelAnimation.From = SkeletonViz.Opacity;
                        skelAnimation.To = 0.75;
                        skelAnimation.Duration = TimeSpan.FromSeconds(1);
                        SkeletonViz.BeginAnimation(SkeletonVisualizer.OpacityProperty, skelAnimation);
                    }
                }
            }
        }

        private Dictionary<ScatterViewItem, Storyboard> StoryboardDictionary = new Dictionary<ScatterViewItem, Storyboard>();

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

                                //DateTime dt = DateTime.Parse(tEvent.EventTime);

                                CalendarEvent ce = new CalendarEvent(tEvent) { EventState = PlayerState };
                                ScatterViewItem svi = ce.CreateScatterViewItem();

                                MainScatterView.Items.Add(svi);

                                svi.Loaded += delegate(object sender2, RoutedEventArgs e)
                                {
                                    Storyboard sb = CreateStoryboard(svi);
                                    StoryboardDictionary[svi] = sb;
                                    sb.Begin();
                                };

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

            HoverTimer = new Timer();
            HoverTimer.Interval = 2000;
            HoverTimer.AutoReset = false;
            HoverTimer.Elapsed += delegate(object source, ElapsedEventArgs timerE)
            {
                this.Dispatcher.Invoke(new Action(delegate()
                {
                    SendItem(HoveredItem);

                    ScatterViewItem itemToRemove = HoveredItem;

                    DoubleAnimation widthAnim = new DoubleAnimation(HoveredItem.ActualWidth, 0, TimeSpan.FromSeconds(0.5));
                    widthAnim.Completed += delegate(object sender, EventArgs e)
                    {
                        MainScatterView.Items.Remove(itemToRemove);
                    };
                    HoveredItem.BeginAnimation(ScatterViewItem.WidthProperty, widthAnim);

                    DoubleAnimation heightAnim = new DoubleAnimation(HoveredItem.ActualHeight, 0, TimeSpan.FromSeconds(0.5));
                    HoveredItem.BeginAnimation(ScatterViewItem.HeightProperty, heightAnim);

                    DoubleAnimation opacityAnim = new DoubleAnimation(HoveredItem.Opacity, 0, TimeSpan.FromSeconds(0.5));
                    HoveredItem.BeginAnimation(ScatterViewItem.OpacityProperty, opacityAnim);      
                }));
            };

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

        bool storyboardplaying = true;
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();

            storyboardplaying = !storyboardplaying;
            SetStoryboardStatus(storyboardplaying);
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
            UpdateStoryboards();
            SetStoryboardStatus(true);

            gestureController = new GestureController();
            gestureController.GestureRecognized += new EventHandler<GestureEventArgs>(gestureController_GestureRecognized);

            RegisterGestures();

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

        private void RegisterGestures()
        {
            IRelativeGestureSegment[] waveRightSegments = new IRelativeGestureSegment[6];
            WaveRightSegment1 waveRightSegment1 = new WaveRightSegment1();
            WaveRightSegment2 waveRightSegment2 = new WaveRightSegment2();
            waveRightSegments[0] = waveRightSegment1;
            waveRightSegments[1] = waveRightSegment2;
            waveRightSegments[2] = waveRightSegment1;
            waveRightSegments[3] = waveRightSegment2;
            waveRightSegments[4] = waveRightSegment1;
            waveRightSegments[5] = waveRightSegment2;
            gestureController.AddGesture("WaveRight", waveRightSegments);

            IRelativeGestureSegment[] waveLeftSegments = new IRelativeGestureSegment[6];
            WaveLeftSegment1 waveLeftSegment1 = new WaveLeftSegment1();
            WaveLeftSegment2 waveLeftSegment2 = new WaveLeftSegment2();
            waveLeftSegments[0] = waveLeftSegment1;
            waveLeftSegments[1] = waveLeftSegment2;
            waveLeftSegments[2] = waveLeftSegment1;
            waveLeftSegments[3] = waveLeftSegment2;
            waveLeftSegments[4] = waveLeftSegment1;
            waveLeftSegments[5] = waveLeftSegment2;
            gestureController.AddGesture("WaveLeft", waveLeftSegments);
        }

        private bool _inSelectionMode = false;
        public bool InSelectionMode
        {
            get { return _inSelectionMode; }
            set
            {
                _inSelectionMode = value;

                if (value)
                {
                    trackingEllipse.Visibility = System.Windows.Visibility.Visible;
                    //SkeletonViz.Visibility = System.Windows.Visibility.Collapsed;
                    trackingEllipse.Opacity = 1;
                }
                else
                {
                    trackingEllipse.Visibility = System.Windows.Visibility.Collapsed;
                    //SkeletonViz.Visibility = System.Windows.Visibility.Visible;
                    DisableTracking();
                }
            }
        }

        private void DisableTracking()
        {
            trackingEllipse.Opacity = 0;
            Canvas.SetLeft(trackingEllipse, 0);
            Canvas.SetTop(trackingEllipse, 0);
        }

        JointType TrackingJoint = JointType.HandRight;
        void gestureController_GestureRecognized(object sender, GestureEventArgs e)
        {
            switch (e.GestureName)
            {
                case "WaveLeft":
                    TrackingJoint = JointType.HandLeft;
                    trackingBrush.ImageSource = new BitmapImage(new Uri(@"../../Resources/leftHand.png", UriKind.Relative));
                    break;
                case "WaveRight":
                    TrackingJoint = JointType.HandRight;
                    trackingBrush.ImageSource = new BitmapImage(new Uri(@"../../Resources/rightHand.png", UriKind.Relative));
                    break;
            }

            InSelectionMode = !InSelectionMode;
        }

        private void UpdateStoryboards()
        {
            foreach (object o in MainScatterView.Items)
            {
                ScatterViewItem svi = (ScatterViewItem)o;
                StoryboardDictionary[svi] = CreateStoryboard(svi);
            }
        }

        private void SetStoryboardStatus(bool on)
        {
            if (on)
            {
                foreach (KeyValuePair<ScatterViewItem, Storyboard> pair in StoryboardDictionary)
                    pair.Value.Begin();
            }
            else
            {
                foreach (KeyValuePair<ScatterViewItem, Storyboard> pair in StoryboardDictionary)
                {
                    pair.Value.Stop();
                }
            }
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
                    gestureController.UpdateAllGestures(playerSkeleton);

                    Joint j = playerSkeleton.Joints[JointType.ShoulderCenter];
                    PlayerDepth = j.Position.Z;

                    if (InSelectionMode)
                    {
                        Joint trackingHand = playerSkeleton.Joints[TrackingJoint];
                        Joint trackingPoint = SkeletalExtensions.ScaleTo(trackingHand, (int)SkeletonViz.ActualWidth, (int)SkeletonViz.ActualHeight);

                        Canvas.SetLeft(trackingEllipse, trackingPoint.Position.X);
                        Canvas.SetTop(trackingEllipse, trackingPoint.Position.Y);

                        DependencyObject sa = MainScatterView.InputHitTest(new Point(trackingPoint.Position.X, trackingPoint.Position.Y)) as DependencyObject;

                        while (sa != null && sa.GetType() != typeof(ScatterViewItem))
                            sa = VisualTreeHelper.GetParent(sa);

                        if (sa != null)
                        {
                            if (HoveredItem == null)
                            {
                                HoveredItem = (ScatterViewItem)sa;
                                HoverTimer.Start();
                            }
                        }
                        else
                        {
                            HoveredItem = null;
                            HoverTimer.Stop();
                        }
                    }
                }
                else
                {
                    PlayerDepth = 9001;
                }
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

        private Storyboard CreateStoryboard(ScatterViewItem svi)
        {
            Storyboard sb = new Storyboard();
            PointAnimation pA = new PointAnimation(svi.ActualCenter, new Point(MainScatterView.ActualWidth / 2, MainScatterView.ActualHeight / 2), TimeSpan.FromSeconds(3));
            pA.Duration = CalculateTime(pA.From.Value, pA.To.Value, 250);            
            pA.FillBehavior = FillBehavior.Stop;
            pA.Completed += delegate(object sender, EventArgs e)
            {
                Point newPoint = CalculateNextPoint(pA.From.Value, pA.To.Value, new Rect(0, 0, MainScatterView.ActualWidth, MainScatterView.ActualHeight));

                pA.From = pA.To.Value;
                pA.To = newPoint;
                pA.Duration = CalculateTime(pA.From.Value, pA.To.Value, 250);

                svi.Center = pA.To.Value;
                sb.Begin();
            };

            Storyboard.SetTarget(pA, svi);
            Storyboard.SetTargetProperty(pA, new PropertyPath(ScatterViewItem.CenterProperty));
            sb.Children.Add(pA);
            return sb;
        }

        private void AnimateScatterViewToPoint(ScatterViewItem svi, Point endPoint)
        {
            Storyboard stb = new Storyboard();

            PointAnimation moveCenter = new PointAnimation();
            moveCenter.From = svi.ActualCenter;
            moveCenter.To = endPoint;
            moveCenter.FillBehavior = FillBehavior.Stop;
            moveCenter.Duration = new Duration(TimeSpan.FromSeconds(2.0));
            moveCenter.EasingFunction = new BackEase();

            DoubleAnimation orientation = new DoubleAnimation();
            orientation.From = svi.Orientation;
            orientation.To = 0;
            orientation.FillBehavior = FillBehavior.Stop;
            orientation.Duration = new Duration(TimeSpan.FromSeconds(2.0));

            stb.Children.Add(moveCenter);
            Storyboard.SetTarget(moveCenter, svi);
            Storyboard.SetTargetProperty(moveCenter, new PropertyPath(ScatterViewItem.CenterProperty));

            stb.Children.Add(orientation);
            Storyboard.SetTarget(orientation, svi);
            Storyboard.SetTargetProperty(orientation, new PropertyPath(ScatterViewItem.OrientationProperty));

            svi.Center = endPoint;
            svi.Orientation = 0;
            stb.Begin(this);
        }

        private TimeSpan CalculateTime(Point p1, Point p2, double speed)
        {
            double distance = CalculateDistance(p1, p2);
            return TimeSpan.FromSeconds(distance / speed);
        }

        public double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private double CalculateSlope(Point p1, Point p2)
        {
            return (p2.Y - p1.Y) / (p2.X - p1.X);
        }

        private double CalculateB(double slope, Point p)
        {
            return p.Y - (slope * p.X);
        }

        private double CalculateX(double slope, double y, double b)
        {
            return (y - b) / slope;
        }

        private double CalculateY(double slope, double x, double b)
        {
            return (slope * x) + b;
        }

        private Point CalculateNextPoint(Point startingPoint, Point collisionPoint, Rect rect)
        {
            double s = -1 * CalculateSlope(startingPoint, collisionPoint);
            double b = CalculateB(s, collisionPoint);

            int side = CalculateSide(collisionPoint, rect);

            double colX = 0;
            double colY = 0;

            if (s < 0)
            {
                switch (side)
                {
                    case 0:
                    case 1:
                        colX = CalculateX(s, rect.Bottom, b);
                        colY = CalculateY(s, rect.Left, b);
                        break;
                    case 2:
                    case 3:
                        colX = CalculateX(s, rect.Top, b);
                        colY = CalculateY(s, rect.Right, b);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (side)
                {
                    case 0:
                    case 3:
                        colX = CalculateX(s, rect.Bottom, b);
                        colY = CalculateY(s, rect.Right, b);
                        break;
                    case 1:
                    case 2:
                        colX = CalculateX(s, rect.Top, b);
                        colY = CalculateY(s, rect.Left, b);
                        break;

                    default:
                        break;
                }
            }

            Point colPX = new Point(colX, CalculateY(s,colX,b));
            Point colPY = new Point(CalculateX(s,colY,b),colY);

            if (rect.Contains(colPX))
                return colPX;
            else if (rect.Contains(colPY))
                return colPY;
            else
                return new Point(500, 500);

        }

        private int CalculateSide(Point p, Rect rect)
        {
            if (p.Y == rect.Top)
            {
                return 0;
            }
            else if (p.X == rect.Right)
            {
                return 1;
            }
            else if (p.Y == rect.Bottom)
            {
                return 2;
            }
            else if (p.X == rect.Left)
            {
                return 3;
            }

            return -1;
        }

        private void SendItem(ScatterViewItem svi)
        {
            CalendarEvent ce = (CalendarEvent)(svi.Content);
            TransferableEvent te = ce.CreateTransferableEvent();

            Message message = new Message("EventItem");
            message.AddField("eventItem", te);
            _connection.SendMessage(message);
        }

    }
}
