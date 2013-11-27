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

namespace iNetworkPhoneClient
{
    public partial class EventItem : UserControl
    {
        public EventItem(string name, DateTime time, System.Windows.Media.ImageSource bmp)
        {
            InitializeComponent();

            eventName.Text = name;
            eventTime.Text = time.ToString();
            eventImage.Source = bmp;
        }

        public void Selected()
        {
            LayoutRoot.Background = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
        }

        public void Deselected()
        {
            LayoutRoot.Background = new SolidColorBrush(Colors.Transparent);
        }
    }
}
