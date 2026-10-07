using Amazon.Lambda.AspNetCoreServer.Hosting;
using HelloWorld;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IValuesService, ValuesService>();

// Uses API Gateway HTTP API events in Lambda and Kestrel during local development.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();
// API Gateway terminates HTTPS; local development also supports plain HTTP.
app.MapControllers();
app.Run();

// Expose the entry point to WebApplicationFactory integration tests.
public partial class Program { }
