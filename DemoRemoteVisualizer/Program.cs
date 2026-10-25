using DemoRemoteVisualizer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<DemoVisualizerService>();

var app = builder.Build();

app.MapGrpcService<DemoVisualizerService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
