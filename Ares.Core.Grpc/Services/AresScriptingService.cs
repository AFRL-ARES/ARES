using System.Threading.Tasks;
using Ares.Services;
using Grpc.Core;
using System;
using System.Threading.Channels;
using Ares.Core.Scripting;
using Microsoft.Extensions.Logging;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;
using System.Linq;
using AresScript;

namespace Ares.Core.Grpc.Services;

public class AresScriptingService : Ares.Services.AresScriptingService.AresScriptingServiceBase
{
  private readonly ILogger<AresScriptingService> _logger;
  private readonly IEnumerable<ISystemFunctionProvider> _systemFunctionProviders;
  private const char FunctionSeparator = '_';

  public AresScriptingService(ILogger<AresScriptingService> logger, IEnumerable<ISystemFunctionProvider> systemFunctionProviders)
  {
    _logger = logger;
    _systemFunctionProviders = systemFunctionProviders;
  }
  public override async Task ExecuteScript(ScriptExecutionRequest request, IServerStreamWriter<ScriptExecutionOutput> responseStream, ServerCallContext context)
  {
    var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100));
    var runner = new ScriptRunner();
    runner.ScriptOutput.Subscribe(output =>
    {
      if(!channel.Writer.TryWrite(output))
      {
        _logger.LogWarning("Dropped script output because channel is full. {Output}", output);
      }
    });
    async Task ReadOutputAsync()
    {
      try
      {
        await foreach(var val in channel.Reader.ReadAllAsync(context.CancellationToken))
        {
          await responseStream.WriteAsync(new ScriptExecutionOutput { Output = val });
        }
      }
      catch(OperationCanceledException) when(context.CancellationToken.IsCancellationRequested)
      {
        _logger.LogInformation("Grpc stream cancelled while sending script output.");
      }
      catch(RpcException e)
      {
        _logger.LogError("RpcException while trying to write to the grpc stream: {Exception}", e);
      }
      catch(Exception e)
      {
        _logger.LogError($"Exception while reading script output. {e}");
      }
    }
    var readTask = ReadOutputAsync();

    try
    {
      await runner.RunScriptAsync(request.Script, context.CancellationToken);
      channel.Writer.TryComplete();
    }
    catch(Exception e)
    {
      channel.Writer.TryComplete(e);
      _logger.LogError("Script runner failed: {Exception}", e);
      throw;
    }
    await readTask;
  }

  public override Task<AvailableDeviceCommandsResponse> GetAvailableDeviceCommands(Empty request, ServerCallContext context)
  {
    var aresFunctions = _systemFunctionProviders
      .OfType<DeviceFunctionProvider>()
      .SelectMany(sfp => sfp.GetFunctions())
      .ToArray();
      
    var functionDescriptions = aresFunctions.Select(func =>
    {
      return new AresFunctionDescription
      {
        Id = func.Id,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema,
        Description = func.Description
      };
    }).ToArray();

    var response = new AvailableDeviceCommandsResponse();
    response.Functions.AddRange(functionDescriptions);
    return Task.FromResult(response);
  }

  public override Task<AutocompleteCatalogResponse> GetAutocompleteCatalog(Empty request, ServerCallContext context)
  {
    var response = BuildAutocompleteCatalog();
    return Task.FromResult(response);
  }

  public override Task<CompletionResponse> GetCompletions(CompletionRequest request, ServerCallContext context)
  {
    var catalog = BuildAutocompleteCatalog();
    var systemFunctions = _systemFunctionProviders.SelectMany(sfp => sfp.GetFunctions()).ToArray();
    var items = new List<CompletionItem>();

    if(TryGetParentIdentifier(request.Script, request.CursorLine, request.CursorColumn, out var parentIdentifier))
    {
      var device = catalog.Devices.FirstOrDefault(d => string.Equals(d.Identifier, parentIdentifier, StringComparison.Ordinal));
      if(device is not null)
      {
        items.AddRange(device.Functions.Select(func => new CompletionItem
        {
          Label = func.Name,
          InsertText = func.Name,
          Detail = func.Description,
          Documentation = func.Description,
          Kind = CompletionItemKind.Function,
          ParentIdentifier = device.Identifier,
          InputSchema = func.InputSchema,
          OutputSchema = func.OutputSchema
        }));
      }
    }
    else
    {
      items.AddRange(catalog.Devices.Select(device => new CompletionItem
      {
        Label = device.Identifier,
        InsertText = device.Identifier,
        Detail = device.DisplayName,
        Documentation = device.Description,
        Kind = CompletionItemKind.Device,
        ParentIdentifier = string.Empty
      }));

      items.AddRange(systemFunctions
        .Where(func => string.IsNullOrWhiteSpace(func.Namespace))
        .Select(func => new CompletionItem
        {
          Label = func.Id,
          InsertText = func.Id,
          Detail = func.Description,
          Documentation = func.Description,
          Kind = CompletionItemKind.Function,
          InputSchema = func.InputSchema,
          OutputSchema = func.OutputSchema
        }));

      items.AddRange(catalog.Globals.Select(global => new CompletionItem
      {
        Label = global.Name,
        InsertText = global.Name,
        Detail = global.Description,
        Documentation = global.Description,
        Kind = CompletionItemKind.Variable,
        Schema = global.Schema
      }));
    }

    var response = new CompletionResponse();
    response.Items.AddRange(items);
    return Task.FromResult(response);
  }

  private AutocompleteCatalogResponse BuildAutocompleteCatalog()
  {
    var aresFunctions = _systemFunctionProviders.SelectMany(sfp => sfp.GetFunctions()).ToArray();
    var deviceMap = new Dictionary<string, DeviceSymbol>(StringComparer.Ordinal);

    foreach(var func in aresFunctions)
    {
      if(!TryResolveDeviceFunction(func, out var deviceIdentifier, out var functionName))
      {
        continue;
      }

      if(!deviceMap.TryGetValue(deviceIdentifier, out var device))
      {
        device = new DeviceSymbol
        {
          DeviceId = deviceIdentifier,
          Identifier = deviceIdentifier,
          DisplayName = deviceIdentifier,
          Description = string.Empty
        };
        deviceMap[deviceIdentifier] = device;
      }

      device.Functions.Add(new FunctionSymbol
      {
        Id = func.Id,
        Name = functionName,
        Description = func.Description,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema
      });
    }

    var response = new AutocompleteCatalogResponse
    {
      CatalogVersion = string.Empty
    };
    response.Devices.AddRange(deviceMap.Values);
    response.GlobalFunctions.AddRange(aresFunctions
      .Where(func => string.IsNullOrWhiteSpace(func.Namespace))
      .Select(func => new FunctionSymbol
      {
        Id = func.Id,
        Name = func.Id,
        Description = func.Description,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema
      }));
    return response;
  }

  private static bool TryResolveDeviceFunction(AresSystemFunction func, out string deviceIdentifier, out string functionName)
  {
    deviceIdentifier = string.Empty;
    functionName = string.Empty;

    if(!string.IsNullOrWhiteSpace(func.Namespace))
    {
      deviceIdentifier = func.Namespace;
      if(!string.IsNullOrWhiteSpace(func.Id))
      {
        var separatorIdx = func.Id.IndexOf(FunctionSeparator);
        if(separatorIdx > 0 && separatorIdx < func.Id.Length - 1)
        {
          var prefix = func.Id[..separatorIdx];
          functionName = string.Equals(prefix, func.Namespace, StringComparison.Ordinal)
            ? func.Id[(separatorIdx + 1)..]
            : func.Id;
        }
        else
        {
          functionName = func.Id;
        }

        return true;
      }

      return false;
    }

    if(string.IsNullOrWhiteSpace(func.Id))
    {
      return false;
    }

    var separatorIndex = func.Id.IndexOf(FunctionSeparator);
    if(separatorIndex <= 0 || separatorIndex >= func.Id.Length - 1)
    {
      return false;
    }

    deviceIdentifier = func.Id[..separatorIndex];
    functionName = func.Id[(separatorIndex + 1)..];
    return true;
  }

  private static bool TryGetParentIdentifier(string script, int cursorLine, int cursorColumn, out string parentIdentifier)
  {
    parentIdentifier = string.Empty;

    if(cursorLine <= 0 || cursorColumn <= 0)
    {
      return false;
    }

    if(string.IsNullOrEmpty(script))
    {
      return false;
    }

    var lines = script.Split(["\r\n", "\n"], StringSplitOptions.None);
    if(cursorLine > lines.Length)
    {
      return false;
    }

    var line = lines[cursorLine - 1];
    var safeColumn = Math.Min(cursorColumn - 1, line.Length);
    var prefix = line[..safeColumn];

    var dotIndex = prefix.LastIndexOf('.');
    if(dotIndex < 0)
    {
      return false;
    }

    var left = prefix[..dotIndex];
    var identifier = ExtractTrailingIdentifier(left);
    if(string.IsNullOrEmpty(identifier))
    {
      return false;
    }

    parentIdentifier = identifier;
    return true;
  }

  private static string ExtractTrailingIdentifier(string text)
  {
    if(string.IsNullOrEmpty(text))
    {
      return string.Empty;
    }

    var end = text.Length - 1;
    while(end >= 0 && char.IsWhiteSpace(text[end]))
    {
      end--;
    }

    if(end < 0)
    {
      return string.Empty;
    }

    var start = end;
    while(start >= 0 && IsIdentifierChar(text[start]))
    {
      start--;
    }

    start++;
    if(start > end || !IsIdentifierStart(text[start]))
    {
      return string.Empty;
    }

    return text.Substring(start, end - start + 1);
  }

  private static bool IsIdentifierChar(char value)
  {
    return IsIdentifierStart(value) || char.IsDigit(value);
  }

  private static bool IsIdentifierStart(char value)
  {
    return value == '_' || char.IsLetter(value);
  }
}
