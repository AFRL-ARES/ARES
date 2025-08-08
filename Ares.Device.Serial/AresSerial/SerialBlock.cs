using System;

namespace Ares.Device.Serial
{
  internal record SerialBlock(byte[] Data, DateTime Timestamp);
}
