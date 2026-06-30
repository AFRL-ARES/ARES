using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.Execution.Interaction;

public class DefaultUserInteractionBroker : IUserInteractionBroker
{
  private readonly Subject<InteractionRequest> _requestSubject = new();
  private TaskCompletionSource<object>? _pendingRequest;

  public IObservable<InteractionRequest> ActiveRequestStream => _requestSubject.AsObservable();

  public async Task<string> RequestInputAsync(string prompt, CancellationToken token)
  {
    _pendingRequest = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
    using var cancellationRegistration = token.Register(() =>
    {
      _pendingRequest.TrySetCanceled(token);
    });

    _requestSubject.OnNext(new InteractionRequest(Guid.NewGuid().ToString(), InteractionType.DataInput, prompt));

    var result = await _pendingRequest.Task;
    return (string)result;
  }

  public void SubmitResponse(string response)
  {
    _pendingRequest?.TrySetResult(response);
  }

  public void CancelRequest()
  {
    _pendingRequest?.TrySetCanceled();
  }

  public async Task<bool> RequestConfirmation(string prompt, CancellationToken token)
  {
    _pendingRequest = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

    using var cancellationRegistration = token.Register(() =>
    {
      _pendingRequest.TrySetCanceled(token);
    });

    _requestSubject.OnNext(new InteractionRequest(Guid.NewGuid().ToString(), InteractionType.Confirmation, prompt));

    var result = await _pendingRequest.Task;
    return (bool)result;
  }
}
