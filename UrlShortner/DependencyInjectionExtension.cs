using Cassandra;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using UrlShortner.Contracts;
using UrlShortner.Errors;
using UrlShortner.Filters;
using UrlShortner.Infrastructure.ShortCodes;
using UrlShortner.Infrastructure.Storage;
using UrlShortner.UseCases.ResolveShortCode;
using UrlShortner.UseCases.ShortenUrl;
using ISession = Cassandra.ISession;

namespace UrlShortner;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IResolveShortCodeUseCase, ResolveShortCodeUseCase>();
        services.AddScoped<IShortenUrlUseCase, ShortenUrlUseCase>();

        return services;
    }

    public static IMvcBuilder AddApi(this IServiceCollection services)
    {
        // A body the model binder cannot read never reaches the use case, so
        // the framework would answer for it with its own ValidationProblemDetails
        // shape. One API, one error contract: those requests answer with the
        // same ErrorResponse as every other failure.
        return services
            .AddControllers(options => options.Filters.Add<ExceptionFilter>())
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(
                    new ErrorResponse { Errors = [ErrorMessages.InvalidRequestBody] }));
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
        services.AddSingleton(CreateCassandraSession(configuration));
        services.AddSingleton<IConnectionMultiplexer>(CreateRedisConnection(configuration));
        services.AddSingleton<CassandraUrlStore>();

        // The cache is a store that wraps another store, so swapping between
        // "database only" and "database + cache" stays a one-line change.
        services.AddSingleton<IUrlStore>(provider => new CachedUrlStore(
            provider.GetRequiredService<CassandraUrlStore>(),
            provider.GetRequiredService<IConnectionMultiplexer>()));

        return services;
    }

    private static ISession CreateCassandraSession(IConfiguration configuration)
    {
        string host = configuration["CASSANDRA_HOST"] ?? "127.0.0.1";

        ISession session = Cluster.Builder()
            .AddContactPoint(host)
            .WithPort(9042)
            .Build()
            .Connect();

        // Creating the schema on startup is a deliberate feature of this study
        // project: a fresh database has to work straight after "compose up".
        // A production service would apply schema changes in a deployment step
        // instead.
        session.Execute(
            """
            CREATE KEYSPACE IF NOT EXISTS url_shortener
            WITH replication = {'class':'SimpleStrategy','replication_factor':1}
            """);

        session.Execute(
            """
            CREATE TABLE IF NOT EXISTS url_shortener.shortened_urls
            (code text PRIMARY KEY, id uuid, long_url text, short_url text, created_on_utc timestamp)
            """);

        session.ChangeKeyspace("url_shortener");

        return session;
    }

    private static IConnectionMultiplexer CreateRedisConnection(IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("Redis") ?? "localhost:6379";

        return ConnectionMultiplexer.Connect(connectionString);
    }
}
