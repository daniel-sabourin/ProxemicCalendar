﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GroupLab.iNetwork;

namespace iNetworkClient
{
    public class TransferableEvent : ITransferable
    {
        string _eventName;
        public string EventName
        {
            get { return _eventName; }
            set { _eventName = value; }
        }

        string _eventTime;

        public string EventTime
        {
            get { return _eventTime; }
            set { _eventTime = value; }
        }

        byte[] _eventImage;
        public byte[] EventImage
        {
            get { return _eventImage; }
            set { _eventImage = value; }
        }

        public TransferableEvent(string eventName, string eventTime, byte[] eventImage)
        {
            _eventTime = eventTime;
            _eventName = eventName;
            _eventImage = eventImage;
        }

        public TransferableEvent(NetworkStreamInfo info)
        {
            _eventName = info.GetString("eventname");
            _eventTime = info.GetString("datetime");
            _eventImage = info.GetBinary("image");
        }

        public void GetStreamData(NetworkStreamInfo info)
        {
            info.AddValue("eventname", _eventName);
            info.AddValue("datetime", _eventTime);
            info.AddValue("image", _eventImage);
        }
    }
}
