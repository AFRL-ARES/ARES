using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Planning;

public record PlanResult(ParameterMetadata Metadata, AresValue Value);
