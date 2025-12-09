using Ares.Device.Serial;
using Ares.Device.Serial.Simulation;
using System;
using System.IO.Ports;

namespace ChemyxPumpPlugin.Simulation;

internal class SimChemyxPumpConnection : AresSerialSimConnection, IChemyxPumpConnection
{
    private readonly SimChemyxPump _pump;

    public SimChemyxPumpConnection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(150) })
    {
        _pump = new SimChemyxPump(AddDataReceived);
    }

    public override void SendInternally(byte[] bytes)
    {
        _pump.SendCommand(bytes);
    }
}
