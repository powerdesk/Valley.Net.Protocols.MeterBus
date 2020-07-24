using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using static Valley.Net.Protocols.MeterBus.EN13757_2.Manufacturer;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public class SecondaryAddress
    {
        public byte[] IdBCD { get; } = new byte[4];
        public Manuf ManCode { get; protected set; }
        public byte Firmware { get; protected set; }
        public DeviceType DeviceType { get; protected set; }

        public SecondaryAddress(string id, Manuf man = Manuf.ANY, DeviceType type = DeviceType.Reserved_0xFF)
        {
            Firmware = 0xFF; // Set Wildcards
            DeviceType = type;
            ManCode = man;
            IdBCD = id.ToBCDArray(true);
        }

    }
}
