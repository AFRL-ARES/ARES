using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Analyzing;
using Ares.Core.Validation.Campaign;
using Ares.Core.Validation.Validators;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Ares.Core.Grpc.Services;

public class ValidationService : AresValidation.AresValidationBase
{
  private readonly IAnalyzerManager _analyzerManager;
  private readonly IEnumerable<ICampaignValidator> _validators;

  public ValidationService(IEnumerable<ICampaignValidator> validators, IAnalyzerManager analyzerManager)
  {
    _validators = validators;
    _analyzerManager = analyzerManager;
  }

  public override Task<ValidationResponse> ValidateAnalyzerSelection(AnalyzerValidationRequest request, ServerCallContext context)
  {
    var analyzer = _analyzerManager.GetAnalyzer(request.Analyzer);
    if (analyzer is null)
      throw new InvalidOperationException($"Could not find analyzer {request.Analyzer} to validate {request.OutputCommandMetadata}");

    var response = new ValidationResponse();

    if (request.OutputCommandMetadata is null)
      throw new InvalidOperationException("No command metadata specified");

    if (request.OutputCommandMetadata.OutputMetadata is null)
    {
      response.Success = false;
      response.Messages.Add("The command does not have an output metadata defined");
      return Task.FromResult(response);
    }

    var result = GoodAnalyzerValidator.Validate(request.OutputCommandMetadata.OutputMetadata, analyzer);
    response.Success = result.Success;
    response.Messages.AddRange(result.Messages);

    return Task.FromResult(response);
  }

  public override Task<ValidationResponse> ValidateFullCampaign(CampaignTemplate request, ServerCallContext context)
  {
    var validatorResponses = _validators.Select(validator => validator.Validate(request)).ToArray();
    var response = new ValidationResponse
    {
      Success = validatorResponses.All(result => result.Success)
    };

    response.Messages.AddRange(validatorResponses.SelectMany(result => result.Messages));

    return Task.FromResult(response);
  }

  public override Task<ValidationResponse> VlidateRegisteredDevices(Empty request, ServerCallContext context)
  {
    throw new NotImplementedException("Might not need this method");
  }
}
