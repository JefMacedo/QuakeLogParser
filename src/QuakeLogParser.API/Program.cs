using Microsoft.OpenApi.Models;
using QuakeLogParser.Application.Interfaces;
using QuakeLogParser.Infrastructure.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Quake log parser",
                Version = "v1",
                Description = "API de consultas do log do jogo Quake 3 Arena",
                Contact = new OpenApiContact
                {
                    Name = "Jeferson Macedo",
                    Email = "jhef.salles@gmail.com"
                }
            });
        });

        builder.Services.AddScoped<ILogParserService, LogParserService>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DevPolicy", policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}