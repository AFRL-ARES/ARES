namespace AlicatMFCRemastered.Commands.Responses;

internal record DataFrameInfoLine(
  string Id,
  int LineNumber,
  string Name,
  string Type,
  string MinVal,
  string MaxVal,
  string Units);
