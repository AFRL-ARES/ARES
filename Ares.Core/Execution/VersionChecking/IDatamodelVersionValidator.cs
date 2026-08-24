using Ares.Datamodel;
using Grpc.Core;
using NuGet.Versioning;

namespace Ares.Core.Execution.VersionChecking;

public interface IDatamodelVersionValidator
{
  /// <summary>
  /// This method takes in a received version of an external ARES service's datamodel and checks if it's a safe version to interact with
  /// </summary>
  /// <param name="serviceVersion">The provided version of the external service's datamodel</param>
  /// <param name="externalServiceName">A name associated with the external service, for clarity in output from ARES</param>
  /// <returns>True if the datamodel version is safe for interaction, false otherwise.</returns>
  public Task<bool> CheckDatamodelVersionValidity(Metadata grpcMetadata, string externalServiceName);

}
