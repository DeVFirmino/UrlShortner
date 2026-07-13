using System.Collections.Concurrent;
using UrlShortner.Entities;
using UrlShortner.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ShortCodeGenerator>();
builder.Services.AddSingleton<IUrlStore, InMemoryUrlStore>();     
builder.Services.AddScoped<IUrlShorteningService, UrlShorteningService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


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

app.UseHttpsRedirection();


