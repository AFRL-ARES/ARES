using Radzen;

namespace UI.Services.Dialog;

internal sealed class RadzenUiDialogService : IUiDialogService
{
  private readonly DialogService _dialogService;

  public RadzenUiDialogService(DialogService dialogService)
  {
    _dialogService = dialogService;
  }

  public async Task<bool> ConfirmAsync(string message, string? title = null, UiConfirmOptions? options = null)
  {
    ConfirmOptions? radzenOptions = null;
    if(options is not null)
    {
      radzenOptions = new ConfirmOptions
      {
        OkButtonText = options.OkButtonText,
        CancelButtonText = options.CancelButtonText
      };
    }

    var result = await _dialogService.Confirm(message, title ?? "", radzenOptions);
    return result.GetValueOrDefault();
  }
}
