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
using Microsoft.Surface.Presentation.Controls;
using System.Globalization;
using System.Windows.Media.Animation;
using System.IO;

namespace iNetworkClient
{
    /// <summary>
    /// Interaction logic for CalendarEvent.xaml
    /// </summary>
    public partial class CalendarEvent : UserControl
    {
        public const double BlackBackgroundOpacityMax = 0.55;
        public const double BlackBackgroundOpacityMin = 0.2;
        public TimeSpan FadeDuration = TimeSpan.FromSeconds(1);

        public enum State { Close, Medium, Far };
        private State _eventState = State.Far;
        public State EventState
        {
            get { return _eventState; }
            set
            {
                _eventState = value;
                AnimateToState(EventState);
            }
        }


        private string _eventName;
        public string EventName
        {
            get { return _eventName; }
            set
            {
                _eventName = value;
                textLabel.Content = EventName;
                ResizeFont();
            }
        }

        private Image _image;
        public Image Image
        {
            get { return _image; }
            set
            {
                _image = value;
                imageBrush.ImageSource = Image.Source;
            }
        }

        private DateTime _date;
        public DateTime Date
        {
            get { return _date; }
            set
            {
                _date = value;

                timeLabel.Content = value.ToString();
            }
        }


        public CalendarEvent(string name, Image image, DateTime date)
        {
            InitializeComponent();

            EventName = name;
            Image = image;
            Date = date;
        }

        public CalendarEvent(TransferableEvent tEvent)
        {
            InitializeComponent();

            EventName = tEvent.EventName;
            Image = new Image() { Source = LoadImage(tEvent.EventImage) };
            Date = DateTime.Now;
        }

        public ScatterViewItem CreateScatterViewItem()
        {
            ScatterViewItem svi = new ScatterViewItem() { Orientation = 0, Width = 256, Height = 256, Content = this, Background = Brushes.Transparent };

            svi.ShowsActivationEffects = false;
            svi.BorderBrush = System.Windows.Media.Brushes.Transparent;

            // Removing the shadow on ScatterViewItem
            RoutedEventHandler loadedEventHandler = null;
            loadedEventHandler = new RoutedEventHandler(delegate
            {
                svi.Loaded -= loadedEventHandler;
                Microsoft.Surface.Presentation.Generic.SurfaceShadowChrome ssc;
                ssc = svi.Template.FindName("shadow", svi) as Microsoft.Surface.Presentation.Generic.SurfaceShadowChrome;
                ssc.Visibility = Visibility.Hidden;
            });
            svi.Loaded += loadedEventHandler;

            return svi;
        }

        private double CalculateFontSize(Label textLabel)
        {
            // Very bad algorithm!
            for (int i = 12; i < 150; i++)
            {
                double testWidth = MeasureString(i, textLabel).Width;
                if (testWidth > this.ActualWidth - 40)
                    return i;
            }

            // Max font size is 150
            return 150;
        }

        private Size MeasureString(double fontSize, Label label)
        {
            var formattedText = new FormattedText(
                label.Content.ToString(),
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch),
                fontSize,
                Brushes.Black);

            return new Size(formattedText.Width, formattedText.Height);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ResizeFont();
        }

        private void ResizeFont()
        {
            double fontSize = CalculateFontSize(textLabel);
            textLabel.FontSize = fontSize;
        }

        private BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private byte[] sourceToByteArray(BitmapSource bs)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bs));
            encoder.QualityLevel = 25;
            byte[] ba = new byte[0];
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Frames.Add(BitmapFrame.Create(bs));
                encoder.Save(stream);
                ba = stream.ToArray();
                stream.Close();
            }
            return ba;
        }

        private void AnimateToState(State state)
        {
            switch (state)
            {
                case State.Close:
                    DoubleAnimation backgroundAnimation2 = new DoubleAnimation(blackBackgroundEllipse.Opacity, BlackBackgroundOpacityMax, FadeDuration);
                    DoubleAnimation textAnimation2 = new DoubleAnimation(textLabel.Opacity, 1, FadeDuration);
                    DoubleAnimation dateAnimation2 = new DoubleAnimation(timeLabel.Opacity, 1, FadeDuration);
                    timeLabel.BeginAnimation(Label.OpacityProperty, dateAnimation2);
                    textLabel.BeginAnimation(Label.OpacityProperty, textAnimation2);
                    blackBackgroundEllipse.BeginAnimation(Ellipse.OpacityProperty, backgroundAnimation2);

                    break;
                case State.Medium:

                    DoubleAnimation backgroundAnimation = new DoubleAnimation(blackBackgroundEllipse.Opacity, BlackBackgroundOpacityMax, FadeDuration);
                    DoubleAnimation textAnimation = new DoubleAnimation(textLabel.Opacity, 1, FadeDuration);
                    textLabel.BeginAnimation(Label.OpacityProperty, textAnimation);
                    blackBackgroundEllipse.BeginAnimation(Ellipse.OpacityProperty, backgroundAnimation);

                    DoubleAnimation dateAnimation = new DoubleAnimation(timeLabel.Opacity, 0, FadeDuration);
                    timeLabel.BeginAnimation(Label.OpacityProperty, dateAnimation);

                    break;
                case State.Far:
                    DoubleAnimation backgroundAnimation1 = new DoubleAnimation(blackBackgroundEllipse.Opacity, BlackBackgroundOpacityMin, FadeDuration);
                    DoubleAnimation textAnimation1 = new DoubleAnimation(textLabel.Opacity, 0, FadeDuration);
                    textLabel.BeginAnimation(Label.OpacityProperty, textAnimation1);
                    blackBackgroundEllipse.BeginAnimation(Ellipse.OpacityProperty, backgroundAnimation1);

                    DoubleAnimation dateAnimation1 = new DoubleAnimation(timeLabel.Opacity, 0, FadeDuration);
                    timeLabel.BeginAnimation(Label.OpacityProperty, dateAnimation1);

                    break;
                default:
                    break;
            }
        }

        public TransferableEvent CreateTransferableEvent()
        {
            return new TransferableEvent(EventName, Date.ToString() , sourceToByteArray(Image.Source as BitmapSource));
        }
    }
}
