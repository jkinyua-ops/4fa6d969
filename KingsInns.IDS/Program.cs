using Duende.IdentityServer.EntityFramework.DbContexts;
using KingsInns.IDS;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(ctx.Configuration));

    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    // Apply migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            // Apply PersistedGrantDbContext migrations
            var persistedGrantDbContext = services.GetRequiredService<PersistedGrantDbContext>();
            persistedGrantDbContext.Database.Migrate();

            // Apply ConfigurationDbContext migrations
            var configurationDbContext = services.GetRequiredService<ConfigurationDbContext>();
            configurationDbContext.Database.Migrate();

            // Apply ApplicationDbContext migrations
            var applicationDbContext = services.GetRequiredService<KingsInns.IDS.Data.ApplicationDbContext>();
            applicationDbContext.Database.Migrate();

            // Seed data
            var userManager = services.GetRequiredService<UserManager<KingsInns.IDS.Models.ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            SeedData.EnsureSeedData(services, userManager, roleManager, app.Configuration);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying migrations or seeding the database.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}