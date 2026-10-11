using Fimple.Atm.Api.Extensions;
using Fimple.Atm.Api.Middlewares;
using Fimple.Atm.Application.Extensions;
using Fimple.Atm.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddLocalSettings(builder.Environment.ContentRootPath);
}

builder.Services.AddAtmControllers();
builder.Services.AddAtmOpenApi();
builder.Services.AddPostgresDatabase(builder.Configuration.GetConnectionString("PostgreSql"));
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.MigrateDatabase();

app.UseMiddleware<ExceptionMiddleware>();
app.UseAtmOpenApi();

var useHttpsRedirection = app.Configuration.GetValue("Hosting:UseHttpsRedirection", true);
if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.MapAtmHealthChecks();
app.Run();

public partial class Program;
