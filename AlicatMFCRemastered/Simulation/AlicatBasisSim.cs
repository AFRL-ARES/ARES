using AlicatMFCRemastered.Commands.Extensions;
using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Enums;
using System.Diagnostics;
using System.Text;
using UnitsNet;

namespace AlicatMFCRemastered.Simulation;

public class AlicatBasisSim : IAlicatSim
{
  private readonly string[] _availableGases = { "Air", "Ar", "CO2", "N2", "O2", "N2O", "H2", "He", "CH4" };
  private readonly Action<byte[]> _byteSender;
  private readonly CancellationTokenSource _generalCancellationTokenSource = new();
  private readonly StandardVolumeFlow _flowBase = StandardVolumeFlow.FromStandardLitersPerMinute(8000);
  private readonly StandardVolumeFlow _totalizer = StandardVolumeFlow.FromStandardLitersPerMinute(0);
  private readonly ISet<StatusCode> _statusCodes = new HashSet<StatusCode>();
  private readonly Temperature _temperatureBase = Temperature.FromDegreesCelsius(40);
  private readonly float _valveDriveBase = 0;

  private string _currentGas = "Air";

  private StandardVolumeFlow _massFlow;
  private bool _processingCommand;
  private StandardVolumeFlow _setpoint = StandardVolumeFlow.FromStandardLitersPerMinute(8000);
  private float _valveDrive;
  private CancellationTokenSource _streamingTokenSource = new();
  private Temperature _temperature;
  private MfcSetpointSourceEnum _setpointSource = MfcSetpointSourceEnum.Analog;

  public AlicatBasisSim(Action<byte[]> byteSender, char id)
  {
    _byteSender = byteSender;
    DeviceId = id;
    var random = new Random();
    _temperature = Temperature.FromDegreesCelsius(_temperatureBase.DegreesCelsius + random.Next(-10, 10));
    _massFlow = StandardVolumeFlow.FromStandardLitersPerMinute(_flowBase.StandardLitersPerMinute + random.Next(-10, 10));
    _valveDrive = _valveDriveBase;
    Start();
  }

  private string TemperatureString => _temperature.Value.ToString("+0.00;-#");
  private string MassFlowString => _massFlow.Value.ToString("+0.00;-#");
  private string SetpointString => _setpoint.Value.ToString("+0.00;-#");
  private string ValveDriveString => _valveDrive.ToString("+0.00;-#");
  private string TotalizerString => _totalizer.Value.ToString("+0.00;-#");

  public char DeviceId { get; private set; }

  public void Dispose()
  {
    _generalCancellationTokenSource.Dispose();
    _streamingTokenSource.Dispose();
  }

  public async Task SendCommand(byte[] command)
  {
    var stringCommand = Encoding.ASCII.GetString(command);
    // simulating the mfc ignoring commands when it's busy processing existing ones
    if (_processingCommand)
    {
      Debug.WriteLine($"Got command [{stringCommand}], but ignored as MFC is busy.");
      return;
    }

    _processingCommand = true;
    var random = new Random();
    // some fake delay to simulate transmission/processing/etc.
    await Task.Delay(random.Next(10, 20));
    
    ProcessCommand(stringCommand);
    _processingCommand = false;
  }

  private void ProcessCommand(string input)
  {
    if(!input.EndsWith('\r'))
      return;

    input = input.TrimEnd('\r');
    //Trace.WriteLine($"{GetType().Name} {DeviceId} Received {input}");
    var deviceId = input.FirstOrDefault();
    if(deviceId is < 'A' or > 'Z')
      return;

    if(deviceId != DeviceId)
      return;

    ProcessQualifiedCommand(input[1..]);
  }

  private void Send(string simulatedResponse)
  {
    if(!simulatedResponse.EndsWith('\r'))
      simulatedResponse += '\r';

    var serialData = Encoding.ASCII.GetBytes(simulatedResponse.ToCharArray());
    var random = new Random();
    var blah = random.Next(1, 10);
    Task.Run(async () =>
    {
      if(serialData.Length < 10)
      {
        _byteSender(serialData);
        return;
      }
      _byteSender(serialData[..blah]);
      await Task.Delay(10);
      _byteSender(serialData[blah..]);

    });
  }

  /// <summary>
  /// Qualified essentially means that we've verified the ID of the command to match the ID of this device
  /// </summary>
  /// <param name="command"></param>
  private void ProcessQualifiedCommand(string command)
  {
    if(string.IsNullOrEmpty(command))
    {
      SendDataFrame();
      return;
    }

    if(command.StartsWith("@="))
    {
      DeviceId = command["@=".Length..].First();
      SendDataFrame();
      return;
    }

    if(command.StartsWith("VE", StringComparison.InvariantCultureIgnoreCase))
    {
      SendFirmwareVersion();
      return;
    }

    if(command.StartsWith("HPUR", StringComparison.InvariantCultureIgnoreCase))
    {
      ProcessHold(command["HPUR".Length..]);
      return;
    }

    if(command.StartsWith("C", StringComparison.InvariantCultureIgnoreCase))
    {
      _statusCodes.Remove(StatusCode.Hld);
      _valveDrive = 0;
      SendDataFrame();
      return;
    }

    if(command.StartsWith("GS", StringComparison.InvariantCultureIgnoreCase))
    {
      var gasNumString = command["GS".Length..];
      ProcessGas(gasNumString);
      return;
    }

    if(command.StartsWith("S", StringComparison.InvariantCultureIgnoreCase))
    {
      ProcessSetpoint(command["S".Length..]);
      return;
    }

    if(command.StartsWith("LSS", StringComparison.InvariantCultureIgnoreCase))
    {
      ProcessSetpointSource(command["LSS".Length..]);
      return;
    }

    Send("?");
  }

  private void ProcessHold(string query)
  {
    // We can ignore the effect on the flow, mainly just set the status code and hold the valve drive
    var percentageParsed = float.TryParse(query, out var holdPercentage);
    if(percentageParsed)
    {
      _statusCodes.Add(StatusCode.Hld);
      _valveDrive = holdPercentage;
      SendDataFrame();
    }
    else
      Send("?");
  }

  private void ProcessSetpointSource(string query)
  {
    query = query.Trim();
    if(string.IsNullOrEmpty(query))
    {
      Send($"{DeviceId} {_setpointSource.ToStringSource()}");
      return;
    }

    var src = SetpointSourceExtensions.FromStringSource(query);
    if(src == MfcSetpointSourceEnum.UnknownSource)
    {
      Send("?");
      return;
    }

    _setpointSource = src;
    Send($"{DeviceId} {_setpointSource.ToStringSource()}");
  }

  private void ProcessSetpoint(string query)
  {
    query = query.Trim();
    var numeric = double.TryParse(query, out var setpoint);
    if(!numeric)
    {
      Send("?");
      return;
    }

    if(_setpointSource == MfcSetpointSourceEnum.Analog || _setpointSource == MfcSetpointSourceEnum.UnknownSource)
    {
      SendDataFrame();
      // ignore?
      return;
    }

    _setpoint = StandardVolumeFlow.FromStandardLitersPerMinute(setpoint);

    SendDataFrame();
  }

  private void ProcessGas(string query)
  {
    if(string.IsNullOrEmpty(query))
    {
      SendCurrentGas();
      return;
    }

    var trimmed = query.Trim();
    if(trimmed == "*")
    {
      SendAvailableGasResponse();
      return;
    }

    var gasNumParsed = int.TryParse(trimmed, out var gasNum);
    if(gasNumParsed && gasNum < _availableGases.Length)
    {
      _currentGas = _availableGases[gasNum];
      SendCurrentGas();
      return;
    }

    Send("?");
  }

  private void SendCurrentGas()
  {
    var gas = _currentGas;
    var gasIdx = Array.IndexOf(_availableGases, gas);
    Send($"{DeviceId} {gasIdx} {gas}");
  }

  private void SendAvailableGasResponse()
  {
    var gasResponses = new List<string>();
    foreach(var gas in _availableGases)
    {
      var response = $"{DeviceId} {gasResponses.Count:D2}   {gas}";
      gasResponses.Add(response);
    }
    var gasResponse = string.Join('\n', gasResponses);

    Send($"{gasResponse}\r");
  }

  private void SendFirmwareVersion()
  {
    Send($"{DeviceId} V3.0.13");
  }

  private void Start()
  {
    Task.Run(async () =>
    {
      while(!_generalCancellationTokenSource.IsCancellationRequested)
      {
        RandomizeData();
        await Task.Delay(TimeSpan.FromMilliseconds(200));
      }
    },
      _generalCancellationTokenSource.Token);
  }

  private void RandomizeData()
  {
    var random = new Random();
    _temperature = Temperature.FromDegreesCelsius(_temperatureBase.DegreesCelsius + random.Next(-10, 10));
    _massFlow = StandardVolumeFlow.FromStandardLitersPerMinute(_flowBase.StandardLitersPerMinute + random.Next(-10, 10));
    if(!_statusCodes.Contains(StatusCode.Hld))
      _valveDrive = random.Next(1, 99);
  }

  private void SendDataFrame()
  {
    var data = new List<string>
    {
      $"{DeviceId}",
      $"{TemperatureString}",
      MassFlowString,
      TotalizerString,
      SetpointString,
      ValveDriveString,
      _currentGas
    };

    var dataString = string.Join(' ', data);
    if(_statusCodes.Any())
      dataString += $" {string.Join(" ", _statusCodes)}";

    Send(dataString);
  }
}
