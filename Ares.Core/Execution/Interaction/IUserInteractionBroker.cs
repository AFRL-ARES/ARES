namespace Ares.Core.Execution.Interaction;

public interface IUserInteractionBroker
{
  /// <summary>
  /// A method used to get data manually via the user input during a campaign.
  /// </summary>
  /// <param name="prompt">The prompt to be displayed to the user.</param>
  /// <param name="token">A cancellation token to cancel the request.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains the user input.</returns>
  Task<string> RequestInputAsync(string prompt, CancellationToken token);
  
  /// <summary>
  /// A method used to request confirmation from the user to continue the campaign
  /// </summary>
  /// <param name="prompt">The prompt to be displayed to the user.</param>
  /// <param name="token">A cancellation token to cancel the request.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task<bool> RequestConfirmation(string prompt, CancellationToken token);

  IObservable<InteractionRequest> ActiveRequestStream { get; }

  void SubmitResponse(string response);

  // Optional: Let the UI cancel the prompt explicitly
  void CancelRequest();
}