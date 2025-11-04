using Ares.Device.Rest;
using Ares.Datamodel.Extensions;
using RestDevice.Commands.Responses.JsonResponses;
using RestDevice.Structure;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Ares.Datamodel;
using Ares.Datamodel.Device;

namespace RestDevice;

public class RestDevice : AresRestDevice, IRestDevice
{
  private readonly HttpClient _deviceClient = new HttpClient();
  private readonly Uri _address;
  private readonly ISubject<ReadDataJsonResponse?> _statePublisher = new BehaviorSubject<ReadDataJsonResponse?>(default);
  private CancellationTokenSource _stateUpdaterCancellation = new();
  private Task _stateUpdater = Task.CompletedTask;

  public RestDevice(string name, string address) : base(name)
  {
    _address = new Uri(address);
    _deviceClient.BaseAddress = _address;
    _deviceClient.Timeout = TimeSpan.FromSeconds(15);
    _deviceClient = new HttpClient
    {
      BaseAddress = _address,
      Timeout = TimeSpan.FromSeconds(15)
    };

    StateStream = _statePublisher.AsObservable();
    var initialState = new ReadDataJsonResponse();
    _statePublisher.OnNext(initialState);
  }

  private async Task<bool> Init()
  {
    var jsonResponse = await RequestDeviceFunctionality();

    if(jsonResponse is null)
      return false;

    PopulateDeviceFunctionality(jsonResponse);
    return true;
  }

  private async Task<ServicesJsonResponse?> RequestDeviceFunctionality()
  {
    var request = CreateServicesRequest();
    if(request is null)
      return null;

    var httpResponse = await _deviceClient!.SendAsync(request);

    if(httpResponse is null || !httpResponse.IsSuccessStatusCode)
      return null;

    var jsonResponse = JsonSerializer.Deserialize<ServicesJsonResponse>(await httpResponse.Content.ReadAsStringAsync());

    return jsonResponse;
  }

  private void PopulateDeviceFunctionality(ServicesJsonResponse response)
  {
    Functions = response.Capabilities.Functions.ConvertFromJsonMethods();
    Version = response.FirmwareVersion;
    Variables = response.Capabilities.Variables.ConvertFromJsonVariables();
    ReportedName = response.DeviceName;
  }

  private HttpRequestMessage CreateServicesRequest()
  {
    var requestAddress = new Uri($"{_address}services");
    return new HttpRequestMessage(HttpMethod.Get, requestAddress);
  }

  public async Task<ReadDataJsonResponse?> GetAndUpdateState()
  {
    if(_deviceClient is null)
      return null;
    var request = CreateServicesRequest();
    var httpResponse = await _deviceClient.SendAsync(request);
    var jsonResponse = JsonSerializer.Deserialize<ReadDataJsonResponse>(await httpResponse.Content.ReadAsStringAsync());

    if(jsonResponse is null || !httpResponse.IsSuccessStatusCode)
      return null;

    _statePublisher.OnNext(jsonResponse);
    return jsonResponse;
  }

  private void StartStateUpdater(TimeSpan interval)
  {
    _stateUpdaterCancellation = new CancellationTokenSource();
    _stateUpdater = Task.Factory.StartNew(async _ =>
    {

      while(!_stateUpdaterCancellation.IsCancellationRequested)
      {
        try
        {
          await GetAndUpdateState();
          await Task.Delay(interval);
        }
        catch(TimeoutException)
        {
          continue;
        }
      }

    }, _stateUpdaterCancellation.Token, TaskCreationOptions.LongRunning);
  }

  private Task StopStateUpdater()
  {
    _stateUpdaterCancellation.Cancel();
    return _stateUpdater;
  }

  public async Task Start()
  {
    await StopStateUpdater();
    StartStateUpdater(TimeSpan.FromSeconds(2));
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    var success = await Init();

    if(success)
    {
      Status.OperationalState = OperationalState.Active;
      Status.Message = "Successfully Connected to REST API Device!";
    }

    else
    {
      Status.OperationalState = OperationalState.Error;
      Status.Message = "ARES was unable to connect to REST API Device";
    }

    return success;
  }

  public async ValueTask DisposeAsync()
  {
    _deviceClient.Dispose();
    _stateUpdaterCancellation.Cancel();
    await _stateUpdater;
    _statePublisher.OnCompleted();
  }

  public async Task<AresValue> ProcessCommand(string cmdName, List<string> parameterNames, List<string> parameterValues)
  {
    var matchingFunction = Functions.FirstOrDefault(func => func.Name == cmdName);

    if(matchingFunction is null)
      return AresValueHelper.CreateNull();


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

    var requestAddress = new Uri($"{_address}{cmdName}?{queryString}");
    var request = new HttpRequestMessage(HttpMethod.Get, requestAddress);
    var response = await _deviceClient.SendAsync(request);

    var deviceResponseString = await response.Content.ReadAsStringAsync();
    return ParseOutput(matchingFunction, deviceResponseString);
  }

  private AresValue ParseOutput(RestDeviceMethod matchingMethod, string response)
  {
    JsonElement? responseElement = null;

    if(matchingMethod.Output.Any())
    {
      var outputElement = matchingMethod.Output.First();
      using(var doc = JsonDocument.Parse(response))
      {
        var root = doc.RootElement;
        var found = root.TryGetProperty(outputElement.Name, out var property);

        if(found)
          responseElement = property;

        if(responseElement is null)
          return AresValueHelper.CreateNull();

        switch(responseElement.Value.ValueKind)
        {
          case JsonValueKind.String:
            var stringValue = responseElement.Value.GetString();
            if(stringValue is not null)
              return AresValueHelper.CreateString(stringValue);
            break;

          case JsonValueKind.True:
            return AresValueHelper.CreateBool(true);

          case (JsonValueKind.False):
            return AresValueHelper.CreateBool(false);

          case (JsonValueKind.Number):
            var numberValue = responseElement.Value.GetDouble();
            return AresValueHelper.CreateNumber(numberValue);

          default:
            break;
        }
      }
    }

    return AresValueHelper.CreateNull();
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    throw new NotImplementedException();
  }

  public string? ReportedName { get; set; }
  public List<KeyValuePair<string, string>> Data { get; set; } = [];
  public string Hardware { get; set; } = string.Empty;
  public List<RestDeviceMethod> Functions { get; set; } = [];
  public List<RestDeviceVariable> Variables { get; set; } = [];
  public bool IsExternalDeviceConnected { get; set; }
  public IObservable<ReadDataJsonResponse?> StateStream { get; }

}
