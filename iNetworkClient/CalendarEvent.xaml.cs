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
using Microsoft.Surface.Presentation.Controls;
using System.Globalization;
using System.Windows.Media.Animation;

namespace iNetworkClient
{
    /// <summary>
    /// Interaction logic for CalendarEvent.xaml
    /// </summary>
    public partial class CalendarEvent : UserControl
    {
        public const double BlackBackgroundOpacity = 0.55;
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
            }
        }


        public CalendarEvent(string name, Image image, DateTime date)
        {
            InitializeComponent();

            EventName = name;
            Image = image;
            Date = date;
        }

        public ScatterViewItem CreateScatterViewItem()
        {
            ScatterViewItem svi = new ScatterViewItem() { Width = 200, Height = 200, Content = this, Background = Brushes.Transparent };

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

        private double CalculateFontSize()
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
            double fontSize = CalculateFontSize();
            textLabel.FontSize = fontSize;
        }

        private void AnimateToState(State state)
        {
            switch (state)
            {
                case State.Close:
                    break;
                case State.Medium:

                    DoubleAnimation backgroundAnimation = new DoubleAnimation(blackBackgroundEllipse.Opacity, BlackBackgroundOpacity, FadeDuration);
                    DoubleAnimation textAnimation = new DoubleAnimation(textLabel.Opacity, 1, FadeDuration);
                    textLabel.BeginAnimation(Label.OpacityProperty, textAnimation);
                    blackBackgroundEllipse.BeginAnimation(Ellipse.OpacityProperty, backgroundAnimation);

                    break;
                case State.Far:
                    break;
                default:
                    break;
            }
        }
    }
}
