using System.Text.Json;
using Ares.Core.CustomCommands.Exchange;
using Ares.Core.CustomCommands.Import;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Core.Scripting;
using Ares.Datamodel;
using Ares.Datamodel.Automation;
using Microsoft.Extensions.Logging;

namespace Ares.Core.CustomCommands;

public sealed class CustomCommandTransferService(ICustomCommandPersistenceService _persistenceService, 
  CustomCommandDeviceDependencyAnalyzer _deviceDependencyAnalyzer,
  ILogger<CustomCommandTransferService> _logger) : ICustomCommandTransferService
{
  private readonly JsonSerializerOptions _serializerOptions = SerializerSettingsHelper.CreateCustomSerializationSettings();

  public async Task<CustomCommandExport?> ExportAsync(Guid commandId, CancellationToken cancellationToken = default)
  {
    var command = await _persistenceService.GetAsync(commandId);
    cancellationToken.ThrowIfCancellationRequested();
    if(command is null)
      return null;

    var definition = command.Clone();
    definition.UniqueId = string.Empty;
    definition.CustomCommandId = string.Empty;
    definition.VersionNumber = 0;
    var json = JsonSerializer.Serialize(definition, _serializerOptions);
    return new CustomCommandExport(definition, json, MakeFileName(definition.Name));
  }

  public async Task<CustomCommandImportAnalysis> AnalyzeImportAsync(string json, CancellationToken cancellationToken = default)
  {
    if(string.IsNullOrWhiteSpace(json))
      throw new CustomCommandImportException("The selected file is empty.");

    CustomCommandVersion definition;
    try
    {
      definition = JsonSerializer.Deserialize<CustomCommandVersion>(json, _serializerOptions)
        ?? throw new CustomCommandImportException("The selected file does not contain a custom command definition.");
    }
    catch(CustomCommandImportException ex)
    {
      _logger.LogError(ex, "Failed to Analyze Imported Custom Command");
      throw;
    }
    catch(JsonException exception)
    {
      _logger.LogError(exception, "Encountered a Json Parsing Exception Importing Custom Commands");
      throw new CustomCommandImportException("The selected file is not a valid custom command JSON file.", exception);
    }

    Validate(definition);
    cancellationToken.ThrowIfCancellationRequested();
    var deviceReferences = await _deviceDependencyAnalyzer.AnalyzeAsync(definition, cancellationToken);
    var existingCommands = await _persistenceService.GetCommandsAsync();
    var matches = existingCommands
      .Select(command => CreateMatch(definition, command))
      .Where(match => match.Score > 0)
      .OrderByDescending(match => match.Score)
      .ThenBy(match => match.Name, StringComparer.OrdinalIgnoreCase)
      .ToArray();

    var warnings = new List<string>();
    if(matches.Any(match => match.IsExact))
      warnings.Add("An identical custom command already exists locally. Importing will still create an independent command.");

    else if(matches.Length > 0)
      warnings.Add("Similar custom commands already exist locally. Review the matches before importing.");

    warnings.AddRange(deviceReferences
      .Where(reference => reference.Status != CustomCommandDeviceReferenceStatus.Valid)
      .Select(reference => reference.Description));

    return new CustomCommandImportAnalysis
    {
      Definition = definition,
      OriginalDefinition = definition.Clone(),
      ExistingMatches = matches,
      DeviceReferences = deviceReferences,
      Warnings = warnings
    };
  }

  public async Task<CustomCommandImportResult> ImportAsync(CustomCommandImportAnalysis analysis, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(analysis);
    ApplyDeviceSelections(analysis);
    if(analysis.DeviceReferences.Any(reference => reference.Status != CustomCommandDeviceReferenceStatus.Valid))
      throw new CustomCommandImportException("The custom command contains invalid device references. Resolve them before importing.");

    var commandId = await _persistenceService.ImportAsNewAsync(analysis.Definition, cancellationToken);
    var importedCommand = await _persistenceService.GetAsync(commandId);
    return new CustomCommandImportResult(commandId, importedCommand?.Name ?? analysis.Definition.Name, analysis.Warnings);
  }

  private static void ApplyDeviceSelections(CustomCommandImportAnalysis analysis)
  {
    var script = analysis.Definition.ScriptBody;
    foreach(var reference in analysis.DeviceReferences.Where(reference => !string.IsNullOrWhiteSpace(reference.SelectedReplacementDeviceName)))
    {
      var replacementIdentifier = ScriptIdentifierHelper.SanitizeIdentifier(reference.SelectedReplacementDeviceName!);
      var originalPath = string.IsNullOrWhiteSpace(reference.CommandIdentifier)
        ? $"devices.{reference.DeviceIdentifier}"
        : $"devices.{reference.DeviceIdentifier}.{reference.CommandIdentifier}";
      var replacementPath = string.IsNullOrWhiteSpace(reference.CommandIdentifier)
        ? $"devices.{replacementIdentifier}"
        : $"devices.{replacementIdentifier}.{reference.CommandIdentifier}";
      script = script.Replace(originalPath, replacementPath, StringComparison.Ordinal);
      reference.DeviceIdentifier = replacementIdentifier;
      reference.ResolvedDeviceId = reference.SelectedReplacementDeviceId;
      reference.Status = CustomCommandDeviceReferenceStatus.Valid;
      reference.Description = $"Using replacement device '{reference.SelectedReplacementDeviceName}'.";
    }

    analysis.Definition.ScriptBody = script;
  }

  private static void Validate(CustomCommandVersion definition)
  {
    if(string.IsNullOrWhiteSpace(definition.Name))
      throw new CustomCommandImportException("The custom command must have a name.");

    var duplicateParameter = definition.InputParameters
      .GroupBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
      .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);

    if(duplicateParameter is not null)
      throw new CustomCommandImportException(string.IsNullOrWhiteSpace(duplicateParameter.Key)
        ? "Custom command input parameter names cannot be empty."
        : $"Custom command input parameter '{duplicateParameter.Key}' is defined more than once.");

    if(definition.ScriptBody.Length > MaximumScriptLength)
      throw new CustomCommandImportException($"The custom command script exceeds the maximum length of {MaximumScriptLength:N0} characters.");
  }

  private static CustomCommandImportMatch CreateMatch(CustomCommandVersion imported, CustomCommandVersion existing)
  {
    var nameSimilarity = NameSimilarity(imported.Name, existing.Name);
    var matchingInputCount = CountMatchingInputs(imported.InputParameters.ToList(), existing.InputParameters.ToList());
    var inputSimilarity = CalculateInputSimilarity(imported.InputParameters.Count, existing.InputParameters.Count, matchingInputCount);

    var outputMatches = AreOutputsEqual(imported.OutputSchema, existing.OutputSchema);
    var scriptMatches = AreScriptsEqual(imported.ScriptBody, existing.ScriptBody);

    var isExact = IsExactMatch(imported, existing, matchingInputCount, outputMatches, scriptMatches);
    var score = isExact ? MatchWeights.ExactMatch : CalculateWeightedScore(nameSimilarity, inputSimilarity, outputMatches, scriptMatches);

    return new CustomCommandImportMatch
    {
      CommandId = TryParseCommandId(existing.CustomCommandId),
      Name = existing.Name,
      Score = score,
      IsExact = isExact,
      Description = BuildMatchDescription(nameSimilarity, matchingInputCount, imported.InputParameters.Count, outputMatches, scriptMatches)
    };
  }

  private static int CountMatchingInputs(List<CustomCommandParameter> imported, List<CustomCommandParameter> existing)
    => imported.Count(param => existing.Any(candidate => string.Equals(candidate.Name, param.Name, StringComparison.OrdinalIgnoreCase)));

  private static double CalculateInputSimilarity(int importedCount, int existingCount, int matchingCount)
  {
    var maxCount = Math.Max(importedCount, existingCount);
    return maxCount == 0 ? 1.0 : (double)matchingCount / maxCount;
  }

  private static bool AreOutputsEqual(AresValueSchema? imported, AresValueSchema? existing) 
    => (imported is null && existing is null) || (imported?.Equals(existing) == true);

  private static bool AreScriptsEqual(string? imported, string? existing) 
    => string.Equals(imported, existing, StringComparison.Ordinal);

  private static bool IsExactMatch(CustomCommandVersion imported, CustomCommandVersion existing, int matchingInputCount, bool outputMatches, bool scriptMatches)
  {
    var metadataMatches = string.Equals(imported.Name, existing.Name, StringComparison.Ordinal)
        && string.Equals(imported.Description, existing.Description, StringComparison.Ordinal);

    var inputsMatch = imported.InputParameters.Count == existing.InputParameters.Count
        && matchingInputCount == imported.InputParameters.Count
        && imported.InputParameters.Zip(existing.InputParameters).All(pair => pair.First.Schema?.Equals(pair.Second.Schema) == true);

    return metadataMatches && inputsMatch && outputMatches && scriptMatches;
  }

  private static int CalculateWeightedScore(double nameSimilarity, double inputSimilarity, bool outputMatches, bool scriptMatches)
  {
    return (int)(nameSimilarity * MatchWeights.Name)
        + (int)(inputSimilarity * MatchWeights.Input)
        + (outputMatches ? MatchWeights.Output : 0)
        + (scriptMatches ? MatchWeights.Script : 0);
  }

  private static Guid TryParseCommandId(string? commandIdStr) 
    => Guid.TryParse(commandIdStr, out var id) ? id : Guid.Empty;

  private static string BuildMatchDescription(double nameSimilarity, int matchingInputs, int totalInputs, bool outputMatches, bool scriptMatches)
  {
    var nameStatus = nameSimilarity == 1 ? "matches" : "is similar";
    var outputStatus = outputMatches ? "matches" : "differs";
    var scriptStatus = scriptMatches ? "matches" : "differs";

    return $"Name {nameStatus}; input names match {matchingInputs} of {totalInputs}; output schema {outputStatus}; script {scriptStatus}.";
  }

  private static double NameSimilarity(string? left, string? right)
  {
    var leftTokens = NormalizeName(left).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
    var rightTokens = NormalizeName(right).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
    if(leftTokens.Count == 0 || rightTokens.Count == 0)
      return 0;

    return leftTokens.Intersect(rightTokens).Count() / (double)leftTokens.Union(rightTokens).Count();
  }

  private static string NormalizeName(string? name)
    => string.Join(' ', (name ?? string.Empty).Split([' ', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

  private static string MakeFileName(string name)
  {
    var invalidCharacters = Path.GetInvalidFileNameChars();
    var sanitizedName = new string(name.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray()).Trim();
    return $"{(string.IsNullOrWhiteSpace(sanitizedName) ? "custom-command" : sanitizedName)}.arescommand.json";
  }

  public const int MaximumScriptLength = 1_000_000;
}