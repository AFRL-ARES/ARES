using Ares.Datamodel;
using System.ComponentModel;

namespace UI.Features.CustomCommand;

public sealed class CustomCommandDashboardArgument(string name, AresValueSchema schema, AresValue value, int index) : INotifyPropertyChanged
{
  private AresValue _value = value;

  public string Name { get; } = name;
  public AresValueSchema Schema { get; } = schema;
  public int Index { get; } = index;

  public AresValue Value  
  {
    get => _value;
    set
    {
      if(ReferenceEquals(_value, value))
        return;

      _value = value;
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
  }

  public event PropertyChangedEventHandler? PropertyChanged;
}