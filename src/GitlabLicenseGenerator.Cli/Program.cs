using GitlabLicenseGenerator.Cli;
using GitlabLicenseGenerator.Core.Licensing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder();
builder.Logging.ClearProviders();
builder.Services.Configure<LicenseGenerationOptions>(builder.Configuration.GetSection("License"));
builder.Services.AddHostedService<LicenseGenerationService>();

await builder.Build().RunAsync();