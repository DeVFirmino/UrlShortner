using UrlShortner;
using UrlShortner.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<PublicOriginOptions>()
    .BindConfiguration(PublicOriginOptions.SectionName)
    .Validate(options => PublicOriginOptions.IsValid(options.BaseUrl),
        "PublicOrigin:BaseUrl must be a public HTTP or HTTPS origin.")
    .ValidateOnStart();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
