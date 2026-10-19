var builder = WebApplication.CreateBuilder(args);

// reverse proxy
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// reverse proxy
app.MapReverseProxy();

app.Run();
