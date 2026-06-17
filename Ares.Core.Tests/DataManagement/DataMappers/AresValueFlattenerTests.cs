using Ares.Core.DataManagement.DataMappers;
using Ares.Datamodel.Extensions;

namespace Ares.Core.Tests.DataManagement.DataMappers;

internal class AresValueFlattenerTests
{
  [Test]
  public void Flatten_ReturnsScalarWithOriginalName()
  {
    var flattened = AresValueFlattener.Flatten("Value", AresValueHelper.CreateNumber(1)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(flattened.Key, Is.EqualTo("Value"));
      Assert.That(flattened.Value.NumberValue, Is.EqualTo(1));
    }
  }

  [Test]
  public void Flatten_RecursivelyFlattensNestedStructs()
  {
    var value = AresValueHelper.CreateStruct();
    value.StructValue.Fields["Nested"] = AresValueHelper.CreateStruct();
    value.StructValue.Fields["Nested"].StructValue.Fields["Value"] = AresValueHelper.CreateString("result");

    var flattened = AresValueFlattener.Flatten("Root", value).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(flattened.Key, Is.EqualTo("Root.Nested.Value"));
      Assert.That(flattened.Value.StringValue, Is.EqualTo("result"));
    }
  }

  [Test]
  public void Flatten_ReturnsNoValuesForEmptyStruct()
  {
    var flattened = AresValueFlattener.Flatten("Root", AresValueHelper.CreateStruct());

    Assert.That(flattened, Is.Empty);
  }
}
