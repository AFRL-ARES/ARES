using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Messaging;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Analyzing;

public abstract class AnalyzerBase<T> : IAnalyzer where T : IMessage, new()
{
  private readonly ISubject<AnalyzerState> _analyzerStateSubject = new BehaviorSubject<AnalyzerState>(AnalyzerState.Disconnected);

  public AnalyzerBase(string name, Version version)
  {
    Name = name;
    Version = version;
    AnalyzerStateObservable = _analyzerStateSubject.AsObservable();
  }

  public string Name { get; }
  public Version Version { get; }
  public IObservable<AnalyzerState> AnalyzerStateObservable { get; }
  public AnalyzerState AnalyzerState { get; protected set; }

  public virtual bool InputSupported(string fullTypeName)
    => typeof(T).FullName == fullTypeName;

  public Task<Analysis> Analyze(Any? input, CancellationToken cancellationToken)
  {
    if (input is null)
      return Task.FromResult(GetDefaultResult());

    var unpackedMessage = UnpackMessage(input);
    if (unpackedMessage is null)
      return Task.FromResult(GetDefaultResult());

    return AnalyzeMessage(unpackedMessage, cancellationToken);
  }

  private Analysis GetDefaultResult()
  {
    return new Analysis
    {
      UniqueId = Guid.NewGuid().ToString(),
      Analyzer = new AnalyzerInfo { Name = Name, UniqueId = Guid.NewGuid().ToString(), Version = Version.ToString() },
      Result = 0
    };
  }

  private T? UnpackMessage(Any input)
  {
    try
    {
      var unpackedMessage = input.Unpack<T>();
      return unpackedMessage;
    }
    catch (InvalidProtocolBufferException e)
    {
      return default;
    }
  }

  protected abstract Task<Analysis> AnalyzeMessage(T input, CancellationToken cancellationToken);
}
