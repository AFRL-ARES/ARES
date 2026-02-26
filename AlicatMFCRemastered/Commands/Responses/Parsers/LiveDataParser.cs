using System.Text.RegularExpressions;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using Ares.Toolkit.Serial.Commands;
using UnitsNet;
using UnitsNet.Units;

namespace AlicatMFCRemastered.Commands.Responses.Parsers;

internal class LiveDataParser : AsciiResponseParser<LiveDataResponse>
{
  public LiveDataParser(DataFrameFormatEntry[] formatEntries)
  {
    FormatEntries = formatEntries;
  }

  public DataFrameFormatEntry[] FormatEntries { get; }

  /// <summary>
  /// Some of the MFCs start spitting out strange data that sometimes
  /// ends up mixing with valid data from other MFCs. If that happens
  /// then we pretty much have to discard the data
  /// </summary>
  /// <param name="message"></param>
  /// <returns></returns>
  private bool IsValid(string message)
  {
    var containsStartChars = message.Contains("\u0002");
    var containsCancel = message.Contains("\u0018");
    var containsNulls = message.Contains("\0");

    return !containsStartChars && !containsCancel && !containsNulls;
  }

  private bool TryParse(string message, out LiveDataResponse? response)
  {
    if(!FormatEntries.Any() || !IsValid(message))
    {
      response = null;
      return false;
    }

    var tokens = message.Split(" ", StringSplitOptions.RemoveEmptyEntries);

    // TODO If we get more tokens than we know what to expect, that means we should either
    // request the data format more often or add some missing formats
    if(tokens.Length > FormatEntries.Length + 1)
    {
      response = null;
      return false;
      // throw new InvalidOperationException($"Got {tokens.Length} response tokens while only knowing how to handle up to {formatEntriesArray.Length + 1} tokens.");
    }

    char id = default;
    Pressure? absolutePressure = default;
    Pressure? gaugePressure = default;
    Pressure? barometricPressure = default;
    Pressure? differentialPressure = default;
    Temperature? temperature = default;
    VolumeFlow? volumetricFlow = default;
    VolumeFlow? totalizedVolumetricFlow = default;
    StandardVolumeFlow? massFlow = default;
    StandardVolumeFlow? setpoint = default;
    StandardVolumeFlow? totalizedMassFlow = default;
    double? valveDrive = null;
    string? gas = default;
    List<StatusCode> statusCodes = new();
    for(var i = 0; i < tokens.Length; i++)
    {
      var format = FormatEntries.First(fe => fe.EntryNumber == i + 1);
      //var format = FormatEntries[i];
      var token = tokens[i];
      _ = double.TryParse(token, out var value);
      switch(format.Field)
      {
        case DataFormatField.Unknown:
          break;
        case DataFormatField.UnitId:
          id = token[0];
          if(id != FormatEntries.First().Id)
          {
            response = null;
            return false;
          }
          break;
        case DataFormatField.Pressure:
          absolutePressure = Pressure.From(value, (PressureUnit)(format.Unit ?? Pressure.BaseUnit));
          break;
        case DataFormatField.GaugePressure:
          gaugePressure = Pressure.From(value, (PressureUnit)(format.Unit ?? Pressure.BaseUnit));
          break;
        case DataFormatField.BarometricPressure:
          barometricPressure = Pressure.From(value, (PressureUnit)(format.Unit ?? Pressure.BaseUnit));
          break;
        case DataFormatField.Temperature:
          temperature = Temperature.From(value, (TemperatureUnit)(format.Unit ?? Temperature.BaseUnit));
          break;
        case DataFormatField.Volumetric:
          volumetricFlow = VolumeFlow.From(value, (VolumeFlowUnit)(format.Unit ?? VolumeFlow.BaseUnit));
          break;
        case DataFormatField.Mass:
          massFlow = StandardVolumeFlow.From(value,
            (StandardVolumeFlowUnit)(format.Unit ?? StandardVolumeFlow.BaseUnit));

          break;
        case DataFormatField.Setpoint:
          setpoint = StandardVolumeFlow.From(value,
            (StandardVolumeFlowUnit)(format.Unit ?? StandardVolumeFlow.BaseUnit));

          break;
        case DataFormatField.TotalizedMassFlow:
          totalizedMassFlow = StandardVolumeFlow.From(value,
            (StandardVolumeFlowUnit)(format.Unit ?? StandardVolumeFlow.BaseUnit));

          break;
        case DataFormatField.TotalizedVolumetricFlow:
          totalizedVolumetricFlow = VolumeFlow.From(value, (VolumeFlowUnit)(format.Unit ?? VolumeFlow.BaseUnit));
          break;
        case DataFormatField.Gas:
          gas = token;
          break;
        case DataFormatField.DifferentialPressure:
          differentialPressure = Pressure.From(value, (PressureUnit)(format.Unit ?? Pressure.BaseUnit));
          break;
        case DataFormatField.Error:
        case DataFormatField.Status:
          var foundCode = Enum.TryParse<StatusCode>(token, true, out var code);
          if(foundCode)
            statusCodes.Add(code);
          break;
        case DataFormatField.StatusCodes:
          var matches = Regex.Matches(token, @"\[(.*?)\]");
          var result = matches.Select(m => m.Groups[1].Value).ToList();
          foreach(var match in result)
          {
            var foundCode2 = Enum.TryParse<StatusCode>(match, true, out var code2);
            if(foundCode2)
              statusCodes.Add(code2);
          }
          break;
        case DataFormatField.ValveDrive:
          valveDrive = value;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    // TODO maybe figure out a better way to figure our if this was a live data message?
    // all live data frames should contain a gas, so if we were unable to find one here, that might mean
    // that this message was not parsable by the live parser
    // also check that the second token doesn't start with one of the common response line indicators
    // (ex.: G** for gas, D** for data format...
    if(string.IsNullOrEmpty(gas) || tokens[1].StartsWith('D') || tokens[1].StartsWith('G') || tokens[1].StartsWith('M'))
    {
      response = null;
      return false;
    }

    var mfcLiveDataInfoResponse = new LiveDataResponse(id,
      absolutePressure,
      gaugePressure,
      barometricPressure,
      differentialPressure,
      temperature,
      volumetricFlow,
      totalizedVolumetricFlow,
      massFlow,
      setpoint,
      totalizedMassFlow,
      valveDrive,
      gas,
      statusCodes);

    response = mfcLiveDataInfoResponse;


    return true;
  }

  protected override bool TryParseResponse(string line, out LiveDataResponse? response)
  {
    try
    {
      if(TryParse(line, out response))
        return true;
    }
    catch(Exception)
    {
      response = null;
    }
    return false;
  }
}
