using Ares.Datamodel;

namespace UI.Features.CampaignEdit.Internal;

public sealed record VisualizationValueSource(string Path, AresDataType Type, string DisplayName);