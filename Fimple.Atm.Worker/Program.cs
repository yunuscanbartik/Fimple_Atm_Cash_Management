using Fimple.Atm.Application.Extensions;
using Fimple.Atm.Infrastructure.Extensions;
using Fimple.Atm.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddLocalSettingsInDevelopment();

builder.Services.AddPostgresDatabase(builder.Configuration.GetConnectionString("PostgreSql"));
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHostedService<QuoteExpiryWorker>();
builder.Services.AddHostedService<OutboxWorker>();

var host = builder.Build();
host.Run();
