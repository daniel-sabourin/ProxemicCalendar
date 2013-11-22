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

namespace iNetworkClient
{
    /// <summary>
    /// Interaction logic for CalendarEvent.xaml
    /// </summary>
    public partial class CalendarEvent : UserControl
    {
        public CalendarEvent()
        {
            InitializeComponent();
        }

        public ScatterViewItem CreateScatterViewItem()
        {
            ScatterViewItem svi = new ScatterViewItem() { Width = 200, Height = 200, Content = this, Background = Brushes.Transparent };

            svi.ShowsActivationEffects = false;
            svi.BorderBrush = System.Windows.Media.Brushes.Transparent;

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
    }
}
