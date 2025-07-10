using ChattingApplicationProject.Data;
using ChattingApplicationProject.Interfaces;
using ChattingApplicationProject.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IUserService, UserService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "DevelopmentCorsPolicy",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:4200", "https://localhost:4200") // Angular app domains
                .AllowAnyHeader()
                .AllowAnyMethod();
            // .AllowCredentials();
        }
    );
});

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

// Enable CORS
app.UseCors("DevelopmentCorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets();

app.UseHttpsRedirection();

app.UseAuthorization();

app.Use(
    async (context, next) =>
    {
        if (context.Request.Path == "/ws")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                // You can now handle the WebSocket connection here
                // For now, just close it immediately
                await webSocket.CloseAsync(
                    System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    System.Threading.CancellationToken.None
                );
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
        else
        {
            await next();
        }
    }
);

app.MapControllers();

app.Run();
