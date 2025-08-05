using Ares.Device.Serial;
using GenericSerialDevice;
using GenericSerialDevice.Commands;
using GenericSerialDevice.Commands.Responses;
using RestSerialDevice.Commands;
using RestSerialDevice.Structure;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using RestSerialDevice.Services;
using System.Text.Json;
using System.Diagnostics;
using GenericSerialDevice.Commands.Responses.JsonResponses;

namespace RestSerialDevice;

public class SerialRestDevice : SerialDevice<ISerialRestDeviceConnection>, ISerialRestDevice
{
  
  private readonly ISubject<ReadDataResponse?> _stateSubject = new BehaviorSubject<ReadDataResponse?>(default);
  private CancellationTokenSource _internalStateUpdateTokenSource = new();
  private Task? _stateUpdater;
  private readonly HttpClient _deviceClient = new HttpClient();
  private readonly Uri _address;
  

  public SerialRestDevice(string deviceName, ISerialRestDeviceConnection connection) : base(deviceName, connection)
  {
    StateStream = _stateSubject.AsObservable();
  }
  

  
  public async Task<DeviceMethodResponse> ProcessCommand(string methodName, List<string> parameterNames, List<string> parameterValues)
  {
    var matchingFunction = Functions.FirstOrDefault(func => func.Name == methodName);

    if(matchingFunction is null)
      return new DeviceMethodResponse();


    Debug.Assert(parameterNames.Count == parameterValues.Count);
    Debug.Assert(matchingFunction.Parameters.Count == parameterNames.Count);

    var queryString = string.Empty;

    for(int i = 0; i < parameterValues.Count; i++)
    {
      var paramString = $"{parameterNames[i]}={parameterValues[i]}";

      if(i != 0)
        queryString = $"{queryString}&{paramString}";

      else
        queryString = paramString;
    }

    var requestAddress = new Uri($"{_address}{methodName}?{queryString}");
    var request = new HttpRequestMessage(HttpMethod.Get, requestAddress);
    var response = await _deviceClient.SendAsync(request);

    var deviceResponseString = await response.Content.ReadAsStringAsync();
    return ParseOutput(matchingFunction, deviceResponseString);
    
  }
  
  private DeviceMethodResponse ParseOutput(RestDeviceMethod matchingMethod, string response)
  {
    var methodResponse = new DeviceMethodResponse();

    JsonElement? responseElement = null;

    if(matchingMethod.Output != null && matchingMethod.Output.Any())
    {
      var outputElement = matchingMethod.Output.First();
      using(var doc = JsonDocument.Parse(response))
      {
        var root = doc.RootElement;
        var found = root.TryGetProperty(outputElement.Name, out var property);

        if(found)
          responseElement = property;

        if(responseElement is null)
          return methodResponse;

        switch(responseElement.Value.ValueKind)
        {
          case (JsonValueKind.String):
            var stringValue = responseElement.Value.GetString();
            methodResponse.StringValue = stringValue;
            break;

          case (JsonValueKind.True):
            methodResponse.BoolValue = true;
            break;

          case (JsonValueKind.False):
            methodResponse.BoolValue = false;
            break;

          case (JsonValueKind.Number):
            var numberValue = responseElement.Value.GetDouble();
            methodResponse.DoubleValue = numberValue;
            break;

          default:
            break;
        }
      }
    }

    return methodResponse;
  }

  public async Task Init()
  {
    var response = await Connection.Send(new GetDeviceCapabilitiesRequest());
    if(response is null)
    {
      // Added more robust error handling
      throw new InvalidOperationException("Failed to retrieve device capabilities. Response was null.");
    }

    ReportedName = response.DeviceName;
    FirmwareVersion = response.FirmwareVersion;
    Variables = response.Variables;
    Functions = response.Methods; 
  }

  public IObservable<ReadDataResponse?> StateStream { get; }

  public async Task<ReadDataResponse> GetAndUpdateState()
  {
    var response = await Connection.Send(new ReadDataRequest());
    _stateSubject.OnNext(response);
    return response;
  }

  public ReadDataResponse? GetState()
  => StateStream.Take(1).Wait();

  public async Task StartStateUpdater(TimeSpan interval)
  {
    await StopStateUpdater();
    _internalStateUpdateTokenSource = new CancellationTokenSource();
    await StartStateUpdater(interval, _internalStateUpdateTokenSource.Token);
  }

  public async Task StartStateUpdater()
  {
    await StopStateUpdater();
    await StartStateUpdater(TimeSpan.FromSeconds(5));
  }

  public async Task StopStateUpdater()
  {
    _internalStateUpdateTokenSource.Cancel();
    if(_stateUpdater is not null)
      await _stateUpdater;
  }

  private async Task StartStateUpdater(TimeSpan interval, CancellationToken token)
  {
    _stateUpdater = Task.Factory.StartNew(async _ =>
    {
      try
      {
        while(!token.IsCancellationRequested)
        {
          try
          {
            var response = await Connection.Send(new ReadDataRequest(), TimeSpan.FromSeconds(5));
            _stateSubject.OnNext(response);
          }
          catch (TimeoutException)
          {
            Debug.WriteLine("ReadDataRequest timed out. Retrying...");
          }
          await Task.Delay(interval, token);
        }
      }
      catch(ObjectDisposedException)
      {
      }
    },
      token,
      TaskCreationOptions.LongRunning);
  }

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      var response = await Connection.Send(new GetDeviceCapabilitiesRequest(), TimeSpan.FromSeconds(10));

      if (response is null)
        return new SerialDeviceValidationResult(false, "Device response returned null!");

      //if (response.DeviceName == string.Empty)
        //return new SerialDeviceValidationResult(false, "Device response was malformed, unable to parse response!");


      //DeviceId = response.DeviceId;
      ExternalDeviceName = response.DeviceName;
      //Hardware = response.Hardware;
      //IsExternalDeviceConnected = response.Connected;
      return new SerialDeviceValidationResult(true);
    }
    catch (TimeoutException e)
    {
      Console.WriteLine(e);
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
    }
    
    return new SerialDeviceValidationResult(false);
    
  }

  public ValueTask DisposeAsync()
  {
    return ValueTask.CompletedTask;
  }

  public override Task EnterSafeMode()
  {
    throw new NotImplementedException();
  }

  public string ReportedName { get; set; }

  public string FirmwareVersion { get; set; }

  public List<KeyValuePair<string, string>> Data { get; set; } = new();

  public string DeviceId { get; set; } = string.Empty;

  public string ExternalDeviceName { get; set; } = string.Empty;

  public string Hardware { get; set; } = string.Empty;


  public IEnumerable<RestSerialDevice.Structure.RestDeviceMethod> Functions { get; set;  } = new List<RestSerialDevice.Structure.RestDeviceMethod>();

  public List<RestDeviceVariable> Variables { get; set; } = new();

  public bool IsExternalDeviceConnected { get; set; }
}
