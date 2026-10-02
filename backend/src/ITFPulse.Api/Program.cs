using DotNetEnv;
using ITFPulse.Application;
using ITFPulse.Infrastructure;
using ITFPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("ITFPULSE_");

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Swagger support
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// CI runs this command once before deploying the same image to Render.
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<ITFPulseDbContext>();
    await database.Database.MigrateAsync();
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Render terminates TLS at its proxy; the container receives HTTP.
if (!app.Configuration.GetValue<bool>("Hosting:HttpsTerminatedAtProxy"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.UseCors("DefaultCorsPolicy");

app.MapControllers();

app.Run();
