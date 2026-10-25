using System.Text.Json;
using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Visualizing.Remote;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoRemoteVisualizer.Services;

public sealed class DemoVisualizerService : RemoteVisualizerService.RemoteVisualizerServiceBase
{
  public const string TemperatureFlowRate = "Temperature and Flow Rate";
  public const string AnalysisObjectiveSummary = "Analysis Objective Summary";
  public const string ParameterObjective = "Parameter Versus Objective";
  public const string ResponseMap = "Experiment Response Map";

  public override Task<VisualizationOptionsResponse> GetVisualizationOptions(Empty request, ServerCallContext context)
  {
    var response = new VisualizationOptionsResponse();
    response.VisualizationOptions.AddRange(
    [
      new VisualizationOption
      {
        VisualizationName = TemperatureFlowRate,
        ChartType = "scatter",
        VisualizationInputSchema = CreateSchema("Temperature", "Flow Rate")
      },
      new VisualizationOption
      {
        VisualizationName = AnalysisObjectiveSummary,
        ChartType = "bar",
        VisualizationInputSchema = CreateSchema("Yield")
      },
      new VisualizationOption
      {
        VisualizationName = ParameterObjective,
        ChartType = "scatter",
        VisualizationInputSchema = CreateSchema("Temperature", "Yield")
      },
      new VisualizationOption
      {
        VisualizationName = ResponseMap,
        ChartType = "heatmap",
        VisualizationInputSchema = CreateSchema("X", "Y", "Value")
      }
    ]);

    return Task.FromResult(response);
  }

  public override Task<GetUpdatedVisualResponse> RequestUpdatedVisualization(GetUpdatedVisualRequest request, ServerCallContext context)
  {
    ArgumentNullException.ThrowIfNull(request);

    var chartJson = request.VisualizationName switch
    {
      TemperatureFlowRate => CreateTemperatureFlowRateChart(request.VisualizationData),
      AnalysisObjectiveSummary => CreateAnalysisObjectiveChart(request.VisualizationData),
      ParameterObjective => CreateParameterObjectiveChart(request.VisualizationData),
      ResponseMap => CreateResponseMapChart(request.VisualizationData),
      _ => throw new RpcException(new Status(StatusCode.NotFound, $"Unknown visualization '{request.VisualizationName}'."))
    };

    return Task.FromResult(new GetUpdatedVisualResponse { ChartJsonData = chartJson });
  }

  public override Task<ConnectionStatus> GetConnectionStatus(Empty request, ServerCallContext context)
    => Task.FromResult(new ConnectionStatus
    {
      Status = AresStatus.Connected,
      Info = "The demo visualizer is connected."
    });

  public override Task<StateResponse> GetState(Empty request, ServerCallContext context)
    => Task.FromResult(new StateResponse
    {
      State = State.Active,
      StateMessage = "The demo visualizer is active."
    });

  public override Task<InfoResponse> GetInfo(Empty request, ServerCallContext context)
    => Task.FromResult(new InfoResponse
    {
      Name = "Demo Plotly Visualizer",
      Version = "0.1.0",
      Description = "A stateless Plotly-compatible visualizer with multiple chart options."
    });

  public override Task StreamPlotlyVisualization(
    GetUpdatedVisualRequest request,
    IServerStreamWriter<GetUpdatedVisualResponse> responseStream,
    ServerCallContext context)
    => throw new RpcException(new Status(StatusCode.Unimplemented, "Streaming is not part of the demo visualizer yet."));

  private static string CreateTemperatureFlowRateChart(IEnumerable<VisualizationDataPoint> points)
  {
    var orderedPoints = points.OrderBy(point => point.Index).ToArray();
    var temperature = orderedPoints.Select(point => GetRequiredNumber(point, "Temperature")).ToArray();
    var flowRate = orderedPoints.Select(point => GetRequiredNumber(point, "Flow Rate")).ToArray();
    var indices = orderedPoints.Select(point => point.Index).ToArray();

    return SerializeFigure(
      [
        new
        {
          x = indices,
          y = temperature,
          type = "scatter",
          mode = "lines+markers",
          name = "Temperature"
        },
        new
        {
          x = indices,
          y = flowRate,
          type = "scatter",
          mode = "lines+markers",
          name = "Flow Rate"
        }
      ],
      new
      {
        title = "Temperature and Flow Rate by Experiment",
        xaxis = new { title = "Experiment" },
        yaxis = new { title = "Value" },
        legend = new { orientation = "h" }
      });
  }

  private static string CreateAnalysisObjectiveChart(IEnumerable<VisualizationDataPoint> points)
  {
    var orderedPoints = points.OrderBy(point => point.Index).ToArray();
    var indices = orderedPoints.Select(point => point.Index).ToArray();
    var objectives = orderedPoints.Select(point => GetRequiredNumber(point, "Yield")).ToArray();

    return SerializeFigure(
      [new
      {
        x = indices,
        y = objectives,
        type = "bar",
        name = "Yield"
      }],
      new
      {
        title = "Analysis Objective by Experiment",
        xaxis = new { title = "Experiment" },
        yaxis = new { title = "Objective Value" }
      });
  }

  private static string CreateParameterObjectiveChart(IEnumerable<VisualizationDataPoint> points)
  {
    var orderedPoints = points.OrderBy(point => point.Index).ToArray();
    var temperature = orderedPoints.Select(point => GetRequiredNumber(point, "Temperature")).ToArray();
    var objectives = orderedPoints.Select(point => GetRequiredNumber(point, "Yield")).ToArray();

    return SerializeFigure(
      [new
      {
        x = temperature,
        y = objectives,
        type = "scatter",
        mode = "markers",
        name = "Yield"
      }],
      new
      {
        title = "Temperature Versus Analysis Objective",
        xaxis = new { title = "Temperature" },
        yaxis = new { title = "Yield" }
      });
  }

  private static string CreateResponseMapChart(IEnumerable<VisualizationDataPoint> points)
  {
    var orderedPoints = points.OrderBy(point => point.Index).ToArray();
    var xValues = orderedPoints.Select(point => GetRequiredNumber(point, "X")).Distinct().Order().ToArray();
    var yValues = orderedPoints.Select(point => GetRequiredNumber(point, "Y")).Distinct().Order().ToArray();
    var values = orderedPoints.ToDictionary(
      point => (X: GetRequiredNumber(point, "X"), Y: GetRequiredNumber(point, "Y")),
      point => GetRequiredNumber(point, "Value"));

    var zValues = yValues
      .Select(y => xValues
        .Select(x => values.TryGetValue((x, y), out var value) ? value : (double?)null)
        .ToArray())
      .ToArray();

    return SerializeFigure(
      [new
      {
        x = xValues,
        y = yValues,
        z = zValues,
        type = "heatmap",
        name = "Response"
      }],
      new
      {
        title = "Experiment Response Map",
        xaxis = new { title = "X" },
        yaxis = new { title = "Y" }
      });
  }

  private static double GetRequiredNumber(VisualizationDataPoint point, string fieldName)
  {
    if(!point.Values.Fields.TryGetValue(fieldName, out var value)
      || !value.TryGetNumericValue(out var number)
      || !double.IsFinite(number))
    {
      throw new RpcException(new Status(StatusCode.InvalidArgument, $"Visualization point {point.Index} is missing numeric field '{fieldName}'."));
    }

    return number;
  }

  private static AresStructSchema CreateSchema(params string[] fields)
  {
    var schema = new AresStructSchema();
    foreach(var field in fields)
    {
      schema.Fields[field] = AresSchemaBuilder.Entry(AresDataType.Float)
        .WithDescription($"Numeric value for {field}.")
        .Build();
    }

    return schema;
  }

  private static string SerializeFigure<TTrace>(IEnumerable<TTrace> data, object layout)
    => JsonSerializer.Serialize(new { data, layout });
}
