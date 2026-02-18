namespace UI.Application.Dialog;

public interface IUiDialogService
{
  Task<bool> ConfirmAsync(string message, string? title = null, UiConfirmOptions? options = null);
}

