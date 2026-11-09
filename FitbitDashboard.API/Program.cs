var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Program.cs in your .NET Core API project
builder.Services.AddControllers(options =>
{
    // Automatically applies "api/" to the front of all attribute routes
    options.Conventions.Add(new RoutePrefixConvention("api"));
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7170") // Your Blazor URL
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("AllowBlazor");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
