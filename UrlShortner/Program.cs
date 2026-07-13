using System.Collections.Concurrent;
using UrlShortener.Models;
using UrlShortner.Services;   


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ShortCodeGenerator>();

// Code -> ShortenedUrl. Vira Cassandra no próximo passo.
var store = new ConcurrentDictionary<string, ShortenedUrl>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
