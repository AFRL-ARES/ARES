namespace Ares.Device.Serial.Tests
{
  internal class BufferTests
  {
    [Test]
    public void BufferExtensions_Splits_And_Removes_Blocks()
    {
      var buffer = new List<SerialBlock>();
      var block1 = new SerialBlock(new byte[] { 1, 2, 3, 4 }, DateTime.UtcNow);
      var block2 = new SerialBlock(new byte[] { 5, 6, 7, 8 }, DateTime.UtcNow);
      var block3 = new SerialBlock(new byte[] { 9, 10, 11, 12 }, DateTime.UtcNow);
      var block4 = new SerialBlock(new byte[] { 13, 14, 15, 16 }, DateTime.UtcNow);
      buffer.Add(block1);
      buffer.Add(block2);
      buffer.Add(block3);
      buffer.Add(block4);

      var test = buffer.SelectMany(buff => buff.Data).ToArray();

      var arrSeg = new ArraySegment<byte>(test, 6, 5);

      buffer.RemoveBytes(arrSeg);

      Assert.Multiple(() =>
        {
          Assert.That(buffer[0].Data.Length, Is.EqualTo(4));
          Assert.That(buffer[0].Data.First(), Is.EqualTo(1));
          Assert.That(buffer[0].Data.Last(), Is.EqualTo(4));
        });
      Assert.Multiple(() =>
        {
          Assert.That(buffer[1].Data.Length, Is.EqualTo(2));
          Assert.That(buffer[1].Data.First(), Is.EqualTo(5));
          Assert.That(buffer[1].Data.Last(), Is.EqualTo(6));
        });
      Assert.Multiple(() =>
        {
          Assert.That(buffer[2].Data.Length, Is.EqualTo(1));
          Assert.That(buffer[2].Data.First(), Is.EqualTo(12));
          Assert.That(buffer[2].Data.Last(), Is.EqualTo(12));
        });
      Assert.Multiple(() =>
        {
          Assert.That(buffer[3].Data.Length, Is.EqualTo(4));
          Assert.That(buffer[3].Data.First(), Is.EqualTo(13));
          Assert.That(buffer[3].Data.Last(), Is.EqualTo(16));
        });
    }
  }
}
