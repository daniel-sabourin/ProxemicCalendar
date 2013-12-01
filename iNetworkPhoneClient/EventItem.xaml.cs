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
using System.Windows.Media.Imaging;
using System.IO;

namespace iNetworkPhoneClient
{
    public partial class EventItem : UserControl
    {
        public EventItem(string name, DateTime time, System.Windows.Media.ImageSource bmp)
        {
            InitializeComponent();


            eventName.Text = name;
            eventTime.Text = time.ToString("ddd MMM dd, yyyy hh:mm tt");
            eventImage.Source = bmp;
        }

        public EventItem(TransferableEvent tEvent)
        {
            InitializeComponent();

            eventName.Text = tEvent.EventName;
            eventTime.Text = DateTime.Parse(tEvent.EventTime).ToString("ddd MMM dd, yyyy hh:mm tt");

            using (MemoryStream stream = new MemoryStream(tEvent.EventImage))
            {
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.SetSource(stream);
                eventImage.Source = bitmapimage;
            }

        }

        public void Selected()
        {
            LayoutRoot.Background = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
        }

        public void Deselected()
        {
            LayoutRoot.Background = new SolidColorBrush(Colors.Transparent);
        }

        public TransferableEvent CreateTransferableEvent()
        {
            return new TransferableEvent(eventName.Text, eventTime.Text, sourceToByteArray(eventImage.Source as BitmapSource));
        }

        private static byte[] sourceToByteArray(BitmapSource bs)
        {
            try
            {
                BitmapImage bitmapImage = (BitmapImage)bs;
                byte[] data;
                WriteableBitmap wb = new WriteableBitmap(bitmapImage);
                using (MemoryStream ms = new MemoryStream())
                {
                    wb.SaveJpeg(ms, bitmapImage.PixelHeight, bitmapImage.PixelWidth, 0, 100);
                    data = ms.ToArray();
                }
                return data;
            }
            catch
            {
                return null;
            }
        }

    }
}
