using Fimple.Atm.Api.Extensions;
using Fimple.Atm.Api.Middlewares;
using Fimple.Atm.Application.Extensions;
using Fimple.Atm.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAtmControllers();
builder.Services.AddOpenApi();
builder.Services.AddPostgresDatabase(builder.Configuration.GetConnectionString("PostgreSql"));
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.MigrateDatabase();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
