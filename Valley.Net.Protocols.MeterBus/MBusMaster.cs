﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valley.Net.Bindings;
using Valley.Net.Bindings.Tcp;
using Valley.Net.Protocols.MeterBus.EN13757_2;
using Valley.Net.Protocols.MeterBus.EN13757_3;

namespace Valley.Net.Protocols.MeterBus
{
    //
    // Packet formats:
    //
    // ACK: size = 1 byte
    //
    //   byte1: ack   = 0xE5
    //
    // SHORT: size = 5 byte
    //
    //   byte1: start   = 0x10
    //   byte2: control = ...
    //   byte3: address = ...
    //   byte4: chksum  = ...
    //   byte5: stop    = 0x16
    //
    // CONTROL: size = 9 byte
    //
    //   byte1: start1  = 0x68
    //   byte2: length1 = ...
    //   byte3: length2 = ...
    //   byte4: start2  = 0x68
    //   byte5: control = ...
    //   byte6: address = ...
    //   byte7: ctl.info= ...
    //   byte8: chksum  = ...
    //   byte9: stop    = 0x16
    //
    // LONG: size = N >= 9 byte
    //
    //   byte1: start1  = 0x68
    //   byte2: length1 = ...
    //   byte3: length2 = ...
    //   byte4: start2  = 0x68
    //   byte5: control = ...
    //   byte6: address = ...
    //   byte7: ctl.info= ...
    //   byte8: data    = ...
    //          ...     = ...
    //   byteN-1: chksum  = ...
    //   byteN: stop    = 0x16
    //
    //
    //

    public sealed class MBusMaster
    {
        private readonly IEndPointBinding _binding = null;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private Packet _packet = null;
        private TimeSpan _defaultTimeOut;

        public event EventHandler<MeterEventArgs> Meter;

        public MBusMaster(string ipAddress, int port, int defaultTimeOut = 3)
            : this(new TcpBinding(new IPEndPoint(IPAddress.Parse(ipAddress), port), new MeterbusFrameSerializer()))
        {
            _defaultTimeOut = TimeSpan.FromSeconds(defaultTimeOut);

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    // response ok -> let waiting threads proceed
                    case AckFrame frame:
                        {
                            Debug.WriteLine("AckFrame on thread: " + Thread.CurrentThread.ManagedThreadId);
                            _resetEvent.Set();
                        }
                        break;
                    case FixedDataLongFrame frame:
                        {
                            _packet = frame.ToPacket();
                            _resetEvent.Set();
                        }
                        break;
                    case VariableDataLongFrame frame:
                        {
                            _packet = frame.ToPacket();
                            _resetEvent.Set();
                        }
                        break;
                }
            };

            ((SocketBinding)_binding).Error += (sender, e) => Console.WriteLine("M-Bus error received: " + e.Error.Message);

            ((SocketBinding)_binding).IoCompleted += (sender, e) => Console.WriteLine(e.SocketError.ToString());
        }

        public MBusMaster(IEndPointBinding binding)
        {
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        public async Task ConnectAsync()
        {
            await _binding.ConnectAsync();
        }

        /// <summary>
        /// Initialization of slave with SND_NKE
        /// </summary>
        /// <param name="address">Primary address</param>
        /// <returns>true if </returns>
        public async Task<bool> Ping(byte address)
        {
            await _binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, address));
            return true;
        }

        public async Task<bool> SelectMeter(int address)
        {
            //LongFrame frame = new LongFrame()
            return true;
        }

        public async Task SetMeterAddress(byte address, byte newaddress)
        {
            var length = (byte)0x03;

            var data = new byte[length];
            data[0] = 0x01;
            data[1] = 0x7a;
            data[2] = newaddress;

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new LongFrame((byte)ControlMask.SND_UD, (byte)ControlInformation.DATA_SEND, address, data, length));
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        //public async Task SetBaudRate(byte address, byte bauderate)
        //{
        //    var payload = new byte[1];
        //    payload[0] = bauderate;

        //    await _endpoint.Send(new LongMeterBusPackage(ControlCommand.SND_UD, address, payload));
        //}

        public async Task SetId(byte address)
        {
            var length = (byte)0x06;

            var data = new byte[length];
            data[0] = 0x0c;
            data[1] = 0x79;
            data[2] = 0x01;
            data[3] = 0x02;
            data[4] = 0x03;
            data[5] = 0x04;

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new LongFrame((byte)ControlMask.SND_UD, (byte)ControlInformation.DATA_SEND, address, data, length));
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        public async Task ResetApplication(byte address)
        {
            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new ControlFrame((byte)ControlMask.SND_UD, (byte)ControlInformation.APPLICATION_RESET, address));
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        /// <summary>
        /// Request for Class 1 Data.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<Packet> RequestAlarm(byte address, TimeSpan timeout)
        {
            var resetEvent = new AutoResetEvent(false);

            Packet packet = null;

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    case FixedDataLongFrame frame:
                        {
                            packet = frame.ToPacket();

                            resetEvent.Set();
                        }
                        break;
                    case VariableDataLongFrame frame:
                        {
                            packet = frame.ToPacket();

                            resetEvent.Set();
                        }
                        break;
                }
            };

            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new ShortFrame((byte)ControlMask.REQ_UD1, address));

                if (!resetEvent.WaitOne(timeout))
                    throw new TimeoutException();
            }
            finally
            {
                await _binding.DisconnectAsync();
            }

            return packet;
        }

        /// <summary>
        /// Request for Class 2 Data.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<Packet> RequestData(byte address, TimeSpan timeout)
        {
            _resetEvent.Reset();

            await _binding.SendAsync(new ShortFrame((byte)ControlMask.REQ_UD2, address)); // request data

            if (!_resetEvent.WaitOne(timeout))
                throw new TimeoutException();

            return _packet;
        }



        public async Task Initialize(byte address)
        {
            try
            {
                await _binding.ConnectAsync();

                await _binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, address));
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }

        public async Task Scan(byte[] addresses, TimeSpan timeout)
        {
            var resetEvent = new AutoResetEvent(false);

            _binding.PacketReceived += (sender, e) =>
            {
                switch (e.Packet)
                {
                    case AckFrame frame:
                        {
                            resetEvent.Set();
                        }
                        break;
                    case LongFrame frame:
                        {
                            resetEvent.Set();

                            Meter?.Invoke(this, new MeterEventArgs(frame.Address));
                        }
                        break;
                }
            };

            try
            {
                await _binding.ConnectAsync();

                foreach (var address in addresses)
                {
                    await _binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, address));

                    if (!resetEvent.WaitOne(timeout))
                        continue;

                    await _binding.SendAsync(new ShortFrame(0x7b, address)); //request data.

                    if (!resetEvent.WaitOne(timeout))
                        throw new TimeoutException();
                }
            }
            finally
            {
                await _binding.DisconnectAsync();
            }
        }
    }
}
