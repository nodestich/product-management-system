using Microsoft.EntityFrameworkCore;

using ProductManagement.Api.Errors;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Application.Services;
using ProductManagement.Infrastructure.Persistence;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

// One-shot schema migration mode used by the compose "migrate" service.
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Seed
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);

    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
