using Fimple.Atm.Application.Extensions;
using Fimple.Atm.Infrastructure.Extensions;
using Fimple.Atm.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPostgresDatabase(builder.Configuration.GetConnectionString("PostgreSql"));
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHostedService<QuoteExpiryWorker>();

var host = builder.Build();
host.Run();
