using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Infrastructure.Ai;
using ServiceTemplate.Infrastructure.Observability;
using ServiceTemplate.Infrastructure.Persistence;

namespace ServiceTemplate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var isInMemory = configuration.GetValue<bool>("Testing", false)
            || configuration.GetValue<bool>("UseInMemoryDatabase", false);

        if (isInMemory)
        {
            var dbName = configuration.GetValue<string>("DatabaseName") ?? "orders_test_db";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });
        }
        else
        {
            var connectionString = configuration.GetConnectionString("Database")
                ?? "Host=localhost;Port=5432;Database=orders_db;Username=postgres;Password=postgres";

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseVector();
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                });
            });
        }

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Register AI Pipeline & Decorators
        services.AddAiInfrastructure(configuration);

        // Register OpenTelemetry Observability
        services.AddServiceObservability(configuration);

        return services;
    }
}
