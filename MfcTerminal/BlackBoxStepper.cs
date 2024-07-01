using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace MfcTerminal
{
    internal class BlackBoxStepper
    {
        private byte[] _positionGet = new byte[] { 0xA1, 0x22, 0x04 };
        private byte[] _targetPositionGet = new byte[] { 0xA1, 0x0A, 0x04 };
        private byte[] _idk = new byte[] { 0xA1, 0x53, 0x01 };
        private byte[] _energize = new byte[] { 0x85 };
        private byte[] _deEnergize = new byte[] { 0x86 };
        private byte[] _reset = new byte[] { 0xB0 };
        private byte[] _exitSafeStart = new byte[] { 0x83 };

        public async Task Start()
        {
            var port = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
            port.NewLine = "\r";

            port.Open();
            var blah = Task.Run(() => {
                while (true)
                {
                    var bepis = (byte)port.ReadByte();
                    Console.Write($"{bepis:X2} ");

                }
            });

            Console.WriteLine("Ready!");

            while (true)
            {
                var input = Console.ReadLine();
                var split = input?.Split(" ");
                switch (split?.FirstOrDefault())
                {
                    case "move":
                        var num = int.Parse(split[1]);
                        var cmd = GetTargetCommand(num, 0xE0);
                        port.Write(cmd, 0, cmd.Length);
                        break;
                    case "getpos":
                        port.Write(_positionGet, 0, _positionGet.Length);
                        break;
                    case "gettargetpos":
                        port.Write(_targetPositionGet, 0, _targetPositionGet.Length);
                        break;
                    case "idk":
                        port.Write(_idk, 0, _idk.Length);
                        break;
                    case "en":
                        port.Write(_energize, 0, _energize.Length);
                        break;
                    case "den":
                        port.Write(_deEnergize, 0, _deEnergize.Length);
                        break;
                    case "exitsafe":
                        port.Write(_exitSafeStart, 0, _exitSafeStart.Length);
                        break;
                    case "velocity":
                        var velocity = int.Parse(split[1]);
                        var velocityCmd = GetTargetCommand(velocity, 0xE3);
                        port.Write(velocityCmd, 0, velocityCmd.Length);
                        break;
                    case "reset":
                        port.Write(_reset, 0, _reset.Length);
                        break;
                    case "write":
                        var nums = split[1..];
                        var parsed = nums.Select(n => byte.Parse(n, System.Globalization.NumberStyles.HexNumber)).ToArray();
                        port.Write(parsed, 0, parsed.Length);
                        break;
                    case "maxspeed":
                        var speed = int.Parse(split[1]);
                        var speedCmd = GetTargetCommand(speed, 0xE6);
                        port.Write(speedCmd, 0, speedCmd.Length);
                        break;
                    case "maxaccel":
                        var accel = int.Parse(split[1]);
                        var accelCmd = GetTargetCommand(accel, 0xEA);
                        port.Write(accelCmd, 0, accelCmd.Length);
                        break;
                    case "maxdecel":
                        var decel = int.Parse(split[1]);
                        var decelCmd = GetTargetCommand(decel, 0xE9);
                        port.Write(decelCmd, 0, decelCmd.Length);
                        break;
                    default:
                        continue;
                }
            }
        }

        private static byte[] GetTargetCommand(int input, byte command)
        {
            var target = (uint)input;
            var ans = new byte[] { command, (byte)(((target >> 7) & 1) | ((target >> 14) & 2) | ((target >> 21) & 4) | ((target >> 28) & 8)), (byte)(target >> 0 & 0x7F), (byte)(target >> 8 & 0x7F), (byte)(target >> 16 & 0x7F), (byte)(target >> 24 & 0x7F) };
            return ans;
        }
    }
}
