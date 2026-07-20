namespace Ares.Core.Campaigns;

public class CampaignTemplateImportException(string message, Exception? innerException = null)
  : Exception(message, innerException);
