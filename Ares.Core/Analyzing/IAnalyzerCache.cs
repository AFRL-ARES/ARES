using Ares.Messaging;
using Ares.Messaging.Analyzing;

namespace Ares.Core.Analyzing;
public interface IAnalyzerCache
{
  Task CacheAnalyzerInfo(RemoteAnalyzer analyzer);
  Task CacheAnalyzerSettings(RemoteAnalyzer analyzer);
  Task<AnalyzerInfo?> GetCachedAnalyzerInfo(string analyzerId);
  Task<AresStruct?> GetCachedAnalyzerSettings(string analyzerId);
}
