using Ares.Core.CustomCommands;
using Ares.Core.CustomCommands.Exchange;
using Ares.Core.CustomCommands.Import;
using ReactiveUI;

namespace UI.Features.CustomCommand.ViewModels;

public sealed class CustomCommandImportWizardViewModel(ICustomCommandTransferService transferService) : ReactiveObject
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

  public async Task<CustomCommandImportResult?> CommitAsync()
  {
    if(_analysis is null || _isBusy || !CanCommit)
      return null;

    _isBusy = true;
    _error = null;
    RaiseStateChanged();
    try
    {
      return await transferService.ImportAsync(_analysis);
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
    this.RaisePropertyChanged(nameof(HasInvalidDeviceReferences));
  }

  private CustomCommandImportAnalysis? _analysis;
  private bool _isBusy;
  private string? _error;
  public CustomCommandImportAnalysis? Analysis => _analysis;
  public bool IsBusy => _isBusy;
  public string? Error => _error;
  public bool HasInvalidDeviceReferences => _analysis?.DeviceReferences.Any(reference => reference.Status != CustomCommandDeviceReferenceStatus.Valid) ?? false;
  public bool CanCommit => _analysis is not null && !_isBusy && !HasInvalidDeviceReferences;
}
