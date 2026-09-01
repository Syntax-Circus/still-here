using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StillHere.Infrastructure;
using StillHere.Infrastructure.Persistence;
using StillHere.Web.Components;
using SyntaxCircus.AspNetCore.Serilog;
using SyntaxCircus.Common;
using SyntaxCircus.DotEnv;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration.ShouldLoadDotEnv(builder.Environment))
{
    builder.Configuration.AddSyntaxCircusDotEnvFiles(builder.Environment.ContentRootPath);
}

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.AddStandardSerilog(configureFileLogging: options =>
{
    options.Enabled = true;
    options.Path = builder.Configuration["Logging:FilePath"] ?? "logs/log-.txt";
    options.RetainedFileCountLimit = 30;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCurrentUserService();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeysPath"]
            ?? Path.Combine(builder.Environment.ContentRootPath, "keys")));

var app = builder.Build();

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/healthz", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok()
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.Run();
