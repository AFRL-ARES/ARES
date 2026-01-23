using System.Threading.Tasks;
using Ares.Services;
using Grpc.Core;
using System;
using System.IO;
using System.Threading.Channels;
using Ares.Core.Scripting;
using Microsoft.Extensions.Logging;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;
using System.Linq;
using AresScript;
using Ares.Datamodel.Scripting;
using System.Text.RegularExpressions;
using AresScript.Generated;
using Antlr4.Runtime;

namespace Ares.Core.Grpc.Services;

public partial class AresScriptingService : Ares.Services.AresScriptingService.AresScriptingServiceBase
{
  private readonly ILogger<AresScriptingService> _logger;
  private readonly BaseEnvironmentBuilder _environmentBuilder;

  public AresScriptingService(ILogger<AresScriptingService> logger, BaseEnvironmentBuilder environmentBuilder)
  {
    _logger = logger;
    _environmentBuilder = environmentBuilder;

  }
  public override async Task ExecuteScript(ScriptExecutionRequest request, IServerStreamWriter<ScriptExecutionOutput> responseStream, ServerCallContext context)
  {
    var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100));
    var env = _environmentBuilder.Build();
    var runner = new ScriptRunner(env);
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
      channel.Writer.TryWrite($"Run failed: {e}");
      channel.Writer.TryComplete(e);
      _logger.LogError("Script runner failed: {Exception}", e);
      throw;
    }
    await readTask;
  }

  public override Task<AutocompleteCatalogResponse> GetAutocompleteCatalog(Empty request, ServerCallContext context)
  {
    var environment = _environmentBuilder.Build();
    var response = BuildAutocompleteCatalog(environment);
    return Task.FromResult(response);
  }

  public override async Task<CompletionResponse> GetCompletions(CompletionRequest request, ServerCallContext context)
  {
    var environment = await BuildEnvironmentForCompletions(request.Script);
    var catalog = BuildAutocompleteCatalog(environment);
    var systemFunctions = environment.GetAllSystemFunctions();
    var userFunctions = environment.GetAllUserFunctions();
    var userVariables = environment.GetAllUserVariableNames();
    var items = new List<CompletionItem>();

    if(TryGetParentIdentifier(request.Script, request.CursorLine, request.CursorColumn, out var parentIdentifier))
    {
      var ns = catalog.Namespaces.FirstOrDefault(n => string.Equals(n.Identifier, parentIdentifier, StringComparison.Ordinal));
      if(ns is not null)
      {
        items.AddRange(ns.Functions.Select(func => new CompletionItem
        {
          Label = func.Name,
          InsertText = func.Name,
          Detail = func.Description,
          Documentation = func.Description,
          Kind = CompletionItemKind.Function,
          ParentIdentifier = ns.Identifier,
          InputSchema = func.InputSchema,
          OutputSchema = func.OutputSchema
        }));
      }
    }
    else
    {
      items.AddRange(catalog.Namespaces.Select(ns => new CompletionItem
      {
        Label = ns.Identifier,
        InsertText = ns.Identifier,
        Detail = ns.DisplayName,
        Documentation = ns.Description,
        Kind = MapNamespaceKindToCompletionKind(ns.Kind),
        ParentIdentifier = string.Empty
      }));

      items.AddRange(systemFunctions
        .Where(func => string.IsNullOrWhiteSpace(func.Namespace))
        .Select(func => new CompletionItem
        {
          Label = func.Name,
          InsertText = func.Name,
          Detail = func.Description,
          Documentation = func.Description,
          Kind = CompletionItemKind.Function,
          InputSchema = func.InputSchema,
          OutputSchema = func.OutputSchema
        }));

      items.AddRange(userFunctions.Select(func => new CompletionItem
      {
        Label = func.Name,
        InsertText = func.Name,
        Detail = "User function",
        Kind = CompletionItemKind.Function
      }));

      items.AddRange(userVariables.Select(name => new CompletionItem
      {
        Label = name,
        InsertText = name,
        Detail = "User variable",
        Kind = CompletionItemKind.Variable
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
    return response;
  }

  public override async Task<ValidateScriptResponse> ValidateScript(ValidateScriptRequest request, ServerCallContext context)
  {
    var diagnostics = new List<Diagnostic>();

    var stream = new AntlrInputStream(request.Script ?? string.Empty);
    var lexer = new AresIndentationLexer(stream);
    var lexerListener = new CollectingLexerErrorListener(diagnostics);
    lexer.RemoveErrorListeners();
    lexer.AddErrorListener(lexerListener);

    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    var parserListener = new CollectingParserErrorListener(diagnostics);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(parserListener);

    AresLangParser.ProgramContext? programCtx = null;
    try
    {
      programCtx = parser.program();
    }
    catch(Exception ex)
    {
      AppendDiagnosticFromException(diagnostics, ex);
    }

    if(programCtx is not null)
    {
      var environment = _environmentBuilder.Build();
      var validator = new AresValidationInterpreter(environment, AresValidationInterpreter.ValidationMode.Strict);
      try
      {
        await validator.Visit(programCtx);
      }
      catch(Exception ex)
      {
        AppendDiagnosticFromException(diagnostics, ex);
      }
    }

    var response = new ValidateScriptResponse();
    response.Diagnostics.AddRange(diagnostics);
    return response;
  }

  private async Task<AresScriptEnvironment> BuildEnvironmentForCompletions(string script)
  {
    var environment = _environmentBuilder.Build();
    if(string.IsNullOrWhiteSpace(script))
    {
      return environment;
    }

    try
    {
      var stream = new AntlrInputStream(script);
      var lexer = new AresIndentationLexer(stream);
      var tokenStream = new CommonTokenStream(lexer);
      var parser = new AresLangParser(tokenStream);
      var programCtx = parser.program();
      var validator = new AresValidationInterpreter(environment, AresValidationInterpreter.ValidationMode.Lenient);
      await validator.Visit(programCtx);
    }
    catch
    {
      // Ignore parse/validation errors for autocomplete; fall back to system scope.
    }

    return environment;
  }

  private static AutocompleteCatalogResponse BuildAutocompleteCatalog(AresScriptEnvironment env)
  {
    var systemFunctions = env.GetAllSystemFunctions();
    var systemVariables = env.GetAllSystemVariables();
    var namespaceMap = new Dictionary<string, NamespaceSymbol>(StringComparer.Ordinal);

    foreach(var func in systemFunctions)
    {
      var namespaceName = func.Namespace;
      if(!namespaceMap.TryGetValue(func.Namespace, out var ns))
      {
        ns = new NamespaceSymbol
        {
          NamespaceId = namespaceName,
          Identifier = namespaceName,
          DisplayName = namespaceName,
          Description = string.Empty,
          Kind = NamespaceKind.Device
        };
        namespaceMap[namespaceName] = ns;
      }

      ns.Functions.Add(new FunctionSymbol
      {
        Id = func.Id,
        Name = func.Name,
        Description = func.Description,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema
      });
    }

    var response = new AutocompleteCatalogResponse
    {
      CatalogVersion = string.Empty
    };
    response.Namespaces.AddRange(namespaceMap.Values);
    response.GlobalFunctions.AddRange(systemFunctions
      .Where(func => string.IsNullOrWhiteSpace(func.Namespace))
      .Select(func => new FunctionSymbol
      {
        Id = func.Id,
        Name = func.Name,
        Description = func.Description,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema
      }));
    response.Globals.AddRange(systemVariables.Select(kv => new GlobalVariableSymbol
    {
      Name = kv.Key,
      Description = string.Empty,
      Schema = new Ares.Datamodel.SchemaEntry { Type = Ares.Datamodel.AresDataType.UnspecifiedType },
      Value = kv.Value
    }));
    return response;
  }

  private static CompletionItemKind MapNamespaceKindToCompletionKind(NamespaceKind kind)
  {
    return kind switch
    {
      NamespaceKind.Device => CompletionItemKind.Device,
      NamespaceKind.Planner => CompletionItemKind.Planner,
      NamespaceKind.Analyzer => CompletionItemKind.Analyzer,
      _ => CompletionItemKind.Unspecified
    };
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

    var lastOpenParen = prefix.LastIndexOf('(');
    var lastCloseParen = prefix.LastIndexOf(')');
    if(lastOpenParen > dotIndex && lastOpenParen > lastCloseParen)
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
    var match = IdentifierRegex().Match(text);
    return match.Success ? match.Groups[1].Value : string.Empty;
  }

  [GeneratedRegex(@"([A-Za-z_][A-Za-z0-9_]*)\s*$")]
  private static partial Regex IdentifierRegex();

  [GeneratedRegex(@"(\d+):(\d+)\s*$")]
  private static partial Regex LineColumnRegex();

  private static void AppendDiagnosticFromException(ICollection<Diagnostic> diagnostics, Exception ex)
  {
    var message = ex.Message ?? "Validation error";
    var line = 1;
    var column = 1;

    var match = LineColumnRegex().Match(message);
    if(match.Success
      && int.TryParse(match.Groups[1].Value, out var parsedLine)
      && int.TryParse(match.Groups[2].Value, out var parsedColumn))
    {
      line = parsedLine;
      column = parsedColumn;
    }

    diagnostics.Add(new Diagnostic
    {
      StartLine = line,
      StartColumn = column,
      EndLine = line,
      EndColumn = column,
      Message = message,
      Severity = DiagnosticSeverity.Error,
      Code = string.Empty
    });
  }

  private sealed class CollectingLexerErrorListener : IAntlrErrorListener<int>
  {
    private readonly ICollection<Diagnostic> _diagnostics;

    public CollectingLexerErrorListener(ICollection<Diagnostic> diagnostics)
    {
      _diagnostics = diagnostics;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
      string msg, RecognitionException e)
    {
      _diagnostics.Add(CreateDiagnostic(line, charPositionInLine, msg));
    }
  }

  private sealed class CollectingParserErrorListener : BaseErrorListener
  {
    private readonly ICollection<Diagnostic> _diagnostics;

    public CollectingParserErrorListener(ICollection<Diagnostic> diagnostics)
    {
      _diagnostics = diagnostics;
    }

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
      string msg, RecognitionException e)
    {
      _diagnostics.Add(CreateDiagnostic(line, charPositionInLine, msg, offendingSymbol));
    }
  }

  private static Diagnostic CreateDiagnostic(int line, int charPositionInLine, string message, IToken? offendingSymbol = null)
  {
    var startColumn = Math.Max(1, charPositionInLine + 1);
    var tokenLength = offendingSymbol?.Text?.Length ?? 1;
    var endColumn = Math.Max(startColumn, startColumn + tokenLength - 1);

    return new Diagnostic
    {
      StartLine = Math.Max(1, line),
      StartColumn = startColumn,
      EndLine = Math.Max(1, line),
      EndColumn = endColumn,
      Message = message,
      Severity = DiagnosticSeverity.Error,
      Code = string.Empty
    };
  }
}
