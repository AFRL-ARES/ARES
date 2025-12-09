using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace ChemyxPumpPlugin.Simulation;

internal class SimChemyxPumpConnection : AresSerialSimConnection, IChemyxPumpConnection
{
    public SimChemyxPumpConnection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(150) })
    {
    }

    public override void SendInternally(byte[] bytes)
    {
        throw new NotImplementedException();
    }
}
