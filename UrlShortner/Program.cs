using System.Collections.Concurrent;
using UrlShortner.Entities;
using UrlShortner.Services;
using Cassandra;
using ISession = Cassandra.ISession;

var builder = WebApplication.CreateBuilder(args);

var cluster = Cluster.Builder()
    .AddContactPoint("127.0.0.1")
    .WithPort(9042)
    .Build();
var session = cluster.Connect("url_shortener");   // "url_shortener" = o keyspace que você criou
builder.Services.AddSingleton<ISession>(session);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ShortCodeGenerator>();
builder.Services.AddSingleton<IUrlStore, CassandraUrlStore>();     
builder.Services.AddScoped<IUrlShorteningService, UrlShorteningService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();   // UI em /swagger
}

app.UseHttpsRedirection();

app.MapPost("/shorten", async (ShortenUrlRequest req, HttpContext http, IUrlShorteningService service) =>
{
    if (string.IsNullOrWhiteSpace(req.Url) ||
        !Uri.TryCreate(req.Url, UriKind.Absolute, out _))
    {
        return Results.BadRequest(new { error = "URL inválida" });
    }

    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var entity = await service.ShortenAsync(req.Url, baseUrl);

    return Results.Ok(new { entity.Code, entity.ShortUrl });
});

app.MapGet("/{code}", async (string code, IUrlShorteningService service) =>
{
    var longUrl = await service.GetLongUrlAsync(code);

    return longUrl is null
        ? Results.NotFound(new { error = "code not found" })
        : Results.Redirect(longUrl, permanent: false);
});

app.Run();

