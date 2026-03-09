using AlicatMFCRemastered.Commands.Responses;
using System.Diagnostics;
using System.Text;
using UnitsNet;

namespace AlicatMFCRemastered.Simulation;

public class AlicatSim : IAlicatSim
{
  private readonly Pressure _absolutePressureBase = Pressure.FromAtmospheres(100);
  private readonly string[] _availableGases = { "Air", "Ar", "CH4", "CO", "CO2", "C2H6", "H2", "He", "N2", "N2O", "Ne", "O2" };
  private readonly Action<byte[]> _byteSender;
  private readonly bool _showVolumetric;
  private readonly bool _showTemperature;
  private readonly DataFrameInfoLine[] _formatLines;
  private readonly string[] _mfgResponse;
  private readonly CancellationTokenSource _generalCancellationTokenSource = new();
  private readonly StandardVolumeFlow _massFlowBase = StandardVolumeFlow.FromStandardLitersPerMinute(8000);
  private readonly ISet<StatusCode> _statusCodes = new HashSet<StatusCode>();
  private readonly Temperature _temperatureBase = Temperature.FromDegreesCelsius(40);
  private readonly VolumeFlow _volumetricFlowBase = VolumeFlow.FromMillilitersPerSecond(6700);

  private Pressure _absolutePressure;
  private string _currentGas = "Air";

  private StandardVolumeFlow _massFlow;
  private bool _processingCommand;
  private StandardVolumeFlow _setpoint = StandardVolumeFlow.FromStandardLitersPerMinute(8000);

  private CancellationTokenSource _streamingTokenSource = new();
  private Temperature _temperature;
  private VolumeFlow _volumetricFlow;

  public AlicatSim(Action<byte[]> byteSender, char id, bool showVolumetric = true, bool showTemperature = true)
  {
    _byteSender = byteSender;
    DeviceId = id;
    _showVolumetric = showVolumetric;
    _showTemperature = showTemperature;
    var random = new Random();
    _absolutePressure = Pressure.FromAtmospheres(_absolutePressureBase.Atmospheres + random.Next(-10, 10));
    _temperature = Temperature.FromDegreesCelsius(_temperatureBase.DegreesCelsius + random.Next(-10, 10));
    _volumetricFlow = VolumeFlow.FromCubicCentimetersPerMinute(_volumetricFlowBase.CubicCentimetersPerMinute + random.Next(-10, 10));
    _massFlow = StandardVolumeFlow.FromStandardLitersPerMinute(_massFlowBase.StandardLitersPerMinute + random.Next(-10, 10));

    var lineNumber = 1;
    var formatLines = new List<DataFrameInfoLine?>()
    {
      new DataFrameInfoLine(DeviceId.ToString(), lineNumber++, DataFormatField.UnitId.ToFriendlyString(), "char", "A", "Z", "na"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Pressure.ToFriendlyString(), "signed", "+000.00", "+160.00", "PSIA"),
      showTemperature ? new(DeviceId.ToString(), lineNumber++, DataFormatField.Temperature.ToFriendlyString(), "signed", "-010.00", "+050.00", "C") : null,
      showVolumetric ? new(DeviceId.ToString(), lineNumber++, DataFormatField.Volumetric.ToFriendlyString(), "signed", "+0000.0", "+0500.0", "CCM") : null,
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Mass.ToFriendlyString(), "signed", "+0000.0", "+0500.0", "SLPM"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Setpoint.ToFriendlyString(), "signed", "+0000.0", "+0500", "SLPM"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Gas.ToFriendlyString(), "string", _availableGases.First(), _availableGases.Last(), "na"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Error.ToFriendlyString(), "string", "na", StatusCode.Adc.ToString(), "na"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Status.ToFriendlyString(), "string", "na", StatusCode.Lck.ToString(), "na"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Status.ToFriendlyString(), "string", "na", StatusCode.Ovr.ToString(), "na"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Status.ToFriendlyString(), "string", "na", StatusCode.Pov.ToString(), "na"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Status.ToFriendlyString(), "string", "na", StatusCode.Tov.ToString(), "na"),
      new(DeviceId.ToString(), lineNumber++, DataFormatField.Status.ToFriendlyString(), "string", "na", StatusCode.Vov.ToString(), "na"),
      new(DeviceId.ToString(), lineNumber, DataFormatField.Status.ToFriendlyString(), "string", "na", StatusCode.Mov.ToString(), "na")
    };

    _mfgResponse = new[]
    {
      $"{DeviceId} M00 ALICAT SCIENTIFIC",
      $"{DeviceId} M01",
      $"{DeviceId} M02 Ph   555-123-4567",
      $"{DeviceId} M03 Fax  555-123-4567",
      $"{DeviceId} M04 Model Number  MC-500SCCM-D-SIMULATED",
      $"{DeviceId} M05 Serial Number 83520",
      $"{DeviceId} M06 Date Manufactured 09/12/2012",
      $"{DeviceId} M07 Date Calibrated   09/12/2012",
      $"{DeviceId} M08 Calibrated By     QQ",
      $"{DeviceId} M09 Software Revision 4v08"
    };

    _formatLines = formatLines.OfType<DataFrameInfoLine>().ToArray();
    Start();
  }

  private string AbsolutePressureString => _absolutePressure.Value.ToString("+0;-#");
  private string TemperatureString => _temperature.Value.ToString("+0;-#");
  private string VolumetricFlowString => _volumetricFlow.Value.ToString("+0;-#");
  private string MassFlowString => _massFlow.Value.ToString("+0;-#");
  private string SetpointString => _setpoint.Value.ToString("+0;-#");

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
    if(deviceId is < 'A' or > 'Z' && deviceId != '*' && deviceId != '@')
      return;

    if(deviceId is '*' or '@')
    {
      StopDataStream();
      return;
    }

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
      SendDataInfo();

    command = command.Replace("$$", string.Empty);
    if(command.StartsWith("@="))
    {
      DeviceId = command["@=".Length..].First();
      if(DeviceId == '@')
        StartDataStream();
      else
        SendDataInfo();

      return;
    }

    if(command.StartsWith("??"))
    {
      ProcessQuery(command["??".Length..]);
      return;
    }

    if(command.StartsWith("VE", StringComparison.InvariantCultureIgnoreCase))
    {
      SendFirmwareVersion();
      return;
    }

    if(command.StartsWith("H", StringComparison.InvariantCultureIgnoreCase))
    {
      ProcessHold(command["H".Length..]);
      return;
    }

    if(command.StartsWith("C", StringComparison.InvariantCultureIgnoreCase))
    {
      _statusCodes.Remove(StatusCode.Hld);
      SendDataInfo();
      return;
    }

    if(command.StartsWith("G", StringComparison.InvariantCultureIgnoreCase))
    {
      var gasNumString = command["G".Length..];
      var gasNum = Convert.ToInt32(gasNumString);
      _currentGas = _availableGases[gasNum];
      SendDataInfo();
      return;
    }

    if(command.StartsWith("S", StringComparison.InvariantCultureIgnoreCase))
    {
      ProcessSetpoint(command["S".Length..]);
      return;
    }

    var numeric = double.TryParse(command, out var setpoint);
    if(numeric)
    {
      Trace.WriteLine($"{GetType().Name} does not support setpoints based on full scale yet.");
      SendDataInfo();
    }

  }

  private void ProcessHold(string query)
  {
    // HC HP and H all should set the status code to HLD that's mostly what we are concerned with
    // without going deep into how the MFC works
    _statusCodes.Add(StatusCode.Hld);
    SendDataInfo();
  }

  private void ProcessSetpoint(string query)
  {
    var numeric = double.TryParse(query, out var setpoint);
    if(!numeric)
      return;

    _setpoint = StandardVolumeFlow.FromStandardLitersPerMinute(setpoint);
    SendDataInfo();
  }

  private void ProcessQuery(string query)
  {
    if(query.StartsWith("G", StringComparison.InvariantCultureIgnoreCase))
      SendAvailableGasResponse(query[1..]);
    else if(query.StartsWith("M", StringComparison.InvariantCultureIgnoreCase))
      SendManufacturerInfoResponse(query[1..]);
    else if(query.StartsWith("D", StringComparison.InvariantCultureIgnoreCase))
      SendDataFrameInfoResponse(query[1..]);
  }

  private void SendAvailableGasResponse(string idx)
  {
    var gasResponses = new List<string>();
    foreach(var gas in _availableGases)
    {
      var response = $"{DeviceId} G{gasResponses.Count:D2}   {gas}";
      gasResponses.Add(response);
    }

    var hasIdx = int.TryParse(idx, out var resultIdx);

    if(!hasIdx)
    {
      Send(string.Join('\r', gasResponses));
      return;
    }

    var responseAtIdx = gasResponses.ElementAtOrDefault(resultIdx);
    Send(responseAtIdx ?? "?");
  }

  private void SendManufacturerInfoResponse(string idx)
  {
    var hasIdx = int.TryParse(idx, out var resultIdx);

    if(!hasIdx)
    {
      Send(string.Join('\r', _mfgResponse));
      return;
    }

    var responseAtIdx = _mfgResponse.ElementAtOrDefault(resultIdx);
    Send(responseAtIdx ?? "?");
  }

  private void SendDataFrameInfoResponse(string idx)
  {
    var responseLines =
      _formatLines
        .Select((line, i) => $"{DeviceId} D{i + 1:D2} {line.Name} {line.Type} {line.MinVal} {line.MaxVal} {line.Units}")
        .Prepend($"{DeviceId} D00 NAME_______ TYPE______ MinVal_ MaxVal_  UNITS_");

    var hasIdx = int.TryParse(idx, out var resultIdx);

    if(!hasIdx)
    {
      Send(string.Join('\r', responseLines));
      return;
    }

    var responseAtIdx = responseLines.ElementAtOrDefault(resultIdx);
    Send(responseAtIdx ?? "?");
  }

  private void SendFirmwareVersion()
  {
    Send($"{DeviceId}  4v08 Sep 11 2012,15:20:53");
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
    }, _generalCancellationTokenSource.Token);
  }

  private void RandomizeData()
  {
    var random = new Random();
    _absolutePressure = Pressure.FromAtmospheres(_absolutePressureBase.Atmospheres + random.Next(-10, 10));
    _temperature = Temperature.FromDegreesCelsius(_temperatureBase.DegreesCelsius + random.Next(-10, 10));
    _volumetricFlow = VolumeFlow.FromMillilitersPerSecond(_volumetricFlowBase.MillilitersPerSecond + random.Next(-10, 10));
    _massFlow = StandardVolumeFlow.FromStandardLitersPerMinute(_massFlowBase.StandardLitersPerMinute + random.Next(-10, 10));
    return;
  }

  private void StartDataStream()
  {
    _streamingTokenSource = new CancellationTokenSource();
    var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(_streamingTokenSource.Token, _generalCancellationTokenSource.Token);
    Task.Run(async () =>
    {
      while(!combinedSource.Token.IsCancellationRequested)
      {
        SendDataInfo();
        await Task.Delay(50, combinedSource.Token);
      }
    },
      combinedSource.Token);
  }

  private void StopDataStream()
  {
    _streamingTokenSource.Cancel();
  }

  private void SendDataInfo()
  {
    var data = new List<string>
    {
      $"{DeviceId}",
      AbsolutePressureString
    };

    if(_showTemperature)
      data.Add(TemperatureString);
    if(_showVolumetric)
      data.Add(VolumetricFlowString);
    data.Add(MassFlowString);
    data.Add(SetpointString);
    data.Add(_currentGas);

    var dataString = string.Join(' ', data);
    if(_statusCodes.Any())
      dataString += $" {string.Join(' ', _statusCodes)}";

    Send(dataString);
  }
}
