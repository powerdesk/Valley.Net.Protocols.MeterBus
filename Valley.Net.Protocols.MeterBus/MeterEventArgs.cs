using System;
using System.Collections.Generic;
using System.Text;
using Valley.Net.Protocols.MeterBus.EN13757_2;

namespace Valley.Net.Protocols.MeterBus
{
    public sealed class MeterEventArgs : EventArgs
    {
        public byte Address { get; }

        public Frame Frame { get; }

        public MeterEventArgs(byte address)
        {
            Address = address;
        }

        public MeterEventArgs(Frame frame)
        {
            Frame = frame;
        }
    }
}
