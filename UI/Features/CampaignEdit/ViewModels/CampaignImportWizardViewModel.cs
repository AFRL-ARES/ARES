using Ares.Core.Campaigns;
using Ares.Core.Campaigns.Models;
using ReactiveUI;

namespace UI.Features.CampaignEdit.ViewModels;

public sealed class CampaignImportWizardViewModel(ICampaignTemplateTransferService transferService) : ReactiveObject
{
  public async Task InitializeAsync(string json)
  {
    _isBusy = true;
    _error = null;
    RaiseStateChanged();
    try
    {
      _analysis = await transferService.AnalyzeImportAsync(json);
    }
    catch(Exception exception)
    {
      _error = exception.Message;
    }
    finally
    {
      _isBusy = false;
      RaiseStateChanged();
    }
  }

  public async Task<CampaignTemplateImportResult?> CommitAsync()
  {
    if(_analysis is null || _isBusy)
      return null;

    _isBusy = true;
    _error = null;
    RaiseStateChanged();
    try
    {
      return await transferService.CommitImportAsync(_analysis);
    }
    catch(Exception exception)
    {
      _error = exception.Message;
      return null;
    }
    finally
    {
      _isBusy = false;
      RaiseStateChanged();
    }
  }

  private void RaiseStateChanged()
  {
    this.RaisePropertyChanged(nameof(Analysis));
    this.RaisePropertyChanged(nameof(IsBusy));
    this.RaisePropertyChanged(nameof(Error));
    this.RaisePropertyChanged(nameof(CanCommit));
  }

  private CampaignImportAnalysis? _analysis;
  private bool _isBusy;
  private string? _error;
  public CampaignImportAnalysis? Analysis => _analysis;
  public bool IsBusy => _isBusy;
  public string? Error => _error;
  public bool CanCommit => _analysis is not null && !_isBusy;
}
