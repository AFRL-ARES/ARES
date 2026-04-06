using Ares.Core.Scripting;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Scripting;
using AresScript;
using AresScript.Environment;
using AresScript.Symbols;

namespace Ares.Core.Tests;

[TestFixture]
public class EnvironmentSymbolWriterTests
{
  [Test]
  public void AddSystemFunction_Adds_Root_Function()
  {
    var env = new AresScriptEnvironment();
    var function = CreateFunction("math::sum", "sum");

    env.AddSystemFunction(function);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(env.TryGetSystemFunction(function.Id, out var storedFunction), Is.True);
      Assert.That(storedFunction, Is.EqualTo(function));
      Assert.That(env.TryGetSystemValueSymbol("sum", out var value), Is.True);
      Assert.That(value?.SymbolKind, Is.EqualTo(SymbolKind.Function));
      Assert.That(value?.Value.FunctionValue.FunctionId, Is.EqualTo(function.Id));
    }
  }

  [Test]
  public void AddSystemFunction_Adds_Nested_Function_From_ParentName()
  {
    var env = new AresScriptEnvironment();
    var function = CreateFunction("devices::scope::run", "run", parentName: "devices.scope");

    env.AddSystemFunction(function);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(env.TryGetSystemValueSymbol("devices", out var devices), Is.True);
      Assert.That(devices?.StructFields?["scope"].StructFields?["run"].Value.FunctionValue.FunctionId, Is.EqualTo(function.Id));
    }
  }

  [Test]
  public void AddSystemFunction_Adds_Nested_Function_From_Namespace()
  {
    var env = new AresScriptEnvironment();
    var function = CreateFunction("math::angle::from", "from", @namespace: "math.angle");

    env.AddSystemFunction(function);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(env.TryGetSystemValueSymbol("math", out var math), Is.True);
      Assert.That(math?.StructFields?["angle"].StructFields?["from"].Value.FunctionValue.FunctionId, Is.EqualTo(function.Id));
    }
  }

  [Test]
  public void AddSystemValue_Adds_Root_Value()
  {
    var env = new AresScriptEnvironment();
    var value = new AresScriptValueSymbol(
      "answer",
      AresValueHelper.CreateNumber(42),
      IsReadOnly: true,
      SymbolKind: SymbolKind.Variable,
      Detail: "the answer",
      Documentation: "doc",
      IsUserDefined: false);

    env.AddSystemValue(value);

    Assert.That(env.TryGetSystemValueSymbol("answer", out var stored), Is.True);
    using(Assert.EnterMultipleScope())
    {
      Assert.That(stored?.Value.NumberValue, Is.EqualTo(42));
      Assert.That(stored?.Detail, Is.EqualTo("the answer"));
      Assert.That(stored?.Documentation, Is.EqualTo("doc"));
      Assert.That(stored?.IsReadOnly, Is.True);
      Assert.That(stored?.IsUserDefined, Is.False);
    }
  }

  [Test]
  public void AddSystemValue_Adds_Nested_Value()
  {
    var env = new AresScriptEnvironment();
    var value = new AresScriptValueSymbol(
      "mode",
      AresValueHelper.CreateString("auto"),
      IsReadOnly: true,
      SymbolKind: SymbolKind.Variable,
      IsUserDefined: false,
      ParentName: "experiment.config");

    env.AddSystemValue(value);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(env.TryGetSystemValueSymbol("experiment", out var experiment), Is.True);
      Assert.That(experiment?.StructFields?["config"].StructFields?["mode"].Value.StringValue, Is.EqualTo("auto"));
    }
  }

  [Test]
  public void AddSystemSymbols_Merges_Functions_And_Struct_Values_On_Same_Path()
  {
    var env = new AresScriptEnvironment();
    var function = CreateFunction("devices::pump::run", "run", parentName: "devices.pump");
    var structValue = new AresScriptValueSymbol(
      "pump",
      AresSystemValue.Struct(new Dictionary<string, AresSystemValue>(StringComparer.Ordinal)
      {
        ["status"] = AresSystemValue.String("idle") with { Name = "status", IsReadOnly = true },
        ["run"] = AresSystemValue.Function(function) with { Name = "run", SymbolKind = SymbolKind.Function, IsReadOnly = true }
      }).Value,
      IsReadOnly: true,
      SymbolKind: SymbolKind.Device,
      Detail: "Pump",
      Documentation: "Pump device",
      IsUserDefined: false,
      ParentName: "devices");

    env.AddSystemFunction(function);
    env.AddSystemValue(structValue);

    Assert.That(env.TryGetSystemValueSymbol("devices", out var devices), Is.True);
    using(Assert.EnterMultipleScope())
    {
      Assert.That(devices?.StructFields?["pump"].StructFields?["run"].Value.FunctionValue.FunctionId, Is.EqualTo(function.Id));
      Assert.That(devices?.StructFields?["pump"].StructFields?["status"].Value.StringValue, Is.EqualTo("idle"));
      Assert.That(devices?.StructFields?["pump"].Documentation, Is.EqualTo("Pump device"));
    }
  }

  [Test]
  public void AddSystemValue_Throws_On_Duplicate_Key()
  {
    var env = new AresScriptEnvironment();

    env.AddSystemValue(new AresScriptValueSymbol("answer", AresValueHelper.CreateNumber(1), IsUserDefined: false));

    var ex = Assert.Throws<InvalidOperationException>(() =>
      env.AddSystemValue(new AresScriptValueSymbol("answer", AresValueHelper.CreateNumber(2), IsUserDefined: false)));

    Assert.That(ex?.Message, Does.Contain("answer"));
  }

  [Test]
  public void AddSystemFunction_Throws_When_Path_Traverses_NonStruct()
  {
    var env = new AresScriptEnvironment();

    env.AddSystemValue(new AresScriptValueSymbol("devices", AresValueHelper.CreateString("busy"), IsUserDefined: false));

    var ex = Assert.Throws<InvalidOperationException>(() =>
      env.AddSystemFunction(CreateFunction("devices::pump::run", "run", parentName: "devices.pump")));

    Assert.That(ex?.Message, Does.Contain("devices"));
  }

  [Test]
  public void AddSystemValue_Preserves_Metadata_For_NonSystemValue_Symbol()
  {
    var env = new AresScriptEnvironment();
    var value = new AresScriptValueSymbol(
      "sample",
      AresValueHelper.CreateStruct(new AresStruct
      {
        Fields =
        {
          ["count"] = AresValueHelper.CreateNumber(3)
        }
      }),
      IsReadOnly: true,
      SymbolKind: SymbolKind.Struct,
      Detail: "detail",
      Documentation: "documentation",
      IsUserDefined: false,
      ParentName: "experiment");

    env.AddSystemValue(value);

    var stored = env.GetAllSystemVariables().Single(item => item.Key == "experiment").Value.StructFields!["sample"];
    using(Assert.EnterMultipleScope())
    {
      Assert.That(stored.Name, Is.EqualTo("sample"));
      Assert.That(stored.ParentName, Is.EqualTo("experiment"));
      Assert.That(stored.IsReadOnly, Is.True);
      Assert.That(stored.IsUserDefined, Is.False);
      Assert.That(stored.SymbolKind, Is.EqualTo(SymbolKind.Struct));
      Assert.That(stored.Detail, Is.EqualTo("detail"));
      Assert.That(stored.Documentation, Is.EqualTo("documentation"));
      Assert.That(stored.StructFields?["count"].Value.NumberValue, Is.EqualTo(3));
    }
  }

  [Test]
  public void BaseEnvironmentBuilder_Builds_From_StandardLibrary_And_Providers()
  {
    var extensionFunction = StandardLibrary.ExtensionFunctions.First();
    var providerSymbols = new IScriptSymbol[]
    {
      CreateFunction("provider::tools::ping", "ping", parentName: "provider.tools"),
      new AresScriptValueSymbol(
        "state",
        AresValueHelper.CreateString("ready"),
        IsReadOnly: true,
        SymbolKind: SymbolKind.Variable,
        IsUserDefined: false,
        ParentName: "provider")
    };
    var builder = new BaseEnvironmentBuilder([new StubSymbolProvider(providerSymbols)]);

    var env = builder.Build();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(env.TryGetSystemFunction("provider::tools::ping", out _), Is.True);
      Assert.That(env.TryGetSystemValueSymbol("provider", out var provider), Is.True);
      Assert.That(provider?.StructFields?["tools"].StructFields?["ping"].Value.FunctionValue.FunctionId, Is.EqualTo("provider::tools::ping"));
      Assert.That(provider?.StructFields?["state"].Value.StringValue, Is.EqualTo("ready"));
      Assert.That(env.TryGetExtensionFunction(extensionFunction.ReceiverKind, extensionFunction.MemberName, out _), Is.True);
      Assert.That(env.TryGetSystemFunction("len", out _), Is.True);
    }
  }

  [Test]
  public void Derived_Builder_Can_Extend_Base_Environment_With_Extension_Methods()
  {
    var builder = new DerivedBuilder([]);

    var env = builder.Build();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(env.TryGetSystemValueSymbol("derived", out var derived), Is.True);
      Assert.That(derived?.StructFields?["flag"].Value.BoolValue, Is.True);
    }
  }

  private static AresSystemFunctionSymbol CreateFunction(string id, string name, string @namespace = "", string? parentName = null)
  {
    return new AresSystemFunctionSymbol(
      id,
      name,
      (_, _) => Task.FromResult(AresValueHelper.CreateUnit()),
      new AresStructSchema(),
      new AresValueSchema { Type = AresDataType.Unit },
      @namespace,
      ParentName: parentName);
  }

  private sealed class StubSymbolProvider(IScriptSymbol[] symbols) : ISymbolProvider
  {
    public IScriptSymbol[] GetSymbols() => symbols;
  }

  private sealed class DerivedBuilder(IEnumerable<ISymbolProvider> symbolProviders) : BaseEnvironmentBuilder(symbolProviders)
  {
    public override AresScriptEnvironment Build()
    {
      var env = base.Build();
      env.AddSystemValue(new AresScriptValueSymbol(
        "flag",
        AresValueHelper.CreateBool(true),
        IsReadOnly: true,
        SymbolKind: SymbolKind.Variable,
        IsUserDefined: false,
        ParentName: "derived"));
      return env;
    }
  }
}
