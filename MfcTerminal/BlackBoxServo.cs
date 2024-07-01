using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace MfcTerminal
{
    internal class BlackBoxServo
    {
        /*
         * Status response example
         * ERROR:
         *  FF FF 11 01 47 C2 3C 14 80 00 00 18 02 18 02 01 00
         * NORMAL(?):
         *  FF FF 11 01 47 56 A8 00 00 00 00 18 02 18 02 01 00
         * 
         */
        List<byte> _bytesLmao = new List<byte>();

        private static byte[] _pistonDown = new byte[] {0xff, 0xff, 0x0c, 0x01, 0x05, 0x76, 0x88, 0x4e, 0x02, 0x00, 0x01, 0x32};
        private static byte[] _pistonUp = new byte[] {0xff, 0xff, 0x0c, 0x01, 0x05, 0xda, 0x24, 0xe0, 0x01, 0x00, 0x01, 0x32 };
        private static byte[] _moveTo500 = new byte[] {0xff, 0xff, 0x0c, 0x01, 0x05, 0xce, 0x30, 0xf4, 0x01, 0x00, 0x01, 0x32 };
        private static byte[] _status = new byte[] { 0xff, 0xff, 0x07, 0x01, 0x07, 0x00, 0xfe };
        private static byte[] _reset = new byte[] { 0xff, 0xff, 0x07, 0x01, 0x09, 0x0e, 0xf0 };

        public async Task Start()
        {
            var port = new SerialPort("COM5");
            port.NewLine = "\r\n";

            port.Open();
            var blah = Task.Run(() => {
                while (true)
                {
                    var bepis = (byte)port.ReadByte();

                    _bytesLmao.Add(bepis);
                    Console.Write($"{bepis:X2} ");

                }
            });

            Console.WriteLine("Ready!");

            while (true)
            {
                var input = Console.ReadLine();
                switch (input)
                {
                    case "status":
                        port.Write(_status, 0, _status.Length);
                        break;
                    case "pistondown":
                        port.Write(_pistonDown, 0, _pistonDown.Length);
                        break;
                    case "pistonup":
                        port.Write(_pistonUp, 0, _pistonUp.Length);
                        break;
                    case "moveto":
                        port.Write(_moveTo500, 0, _moveTo500.Length);
                        break;
                    case "reset":
                        port.Write(_reset, 0, _reset.Length);
                        break;
                    default:
                        continue;
                }
            }
        }
    }
}
