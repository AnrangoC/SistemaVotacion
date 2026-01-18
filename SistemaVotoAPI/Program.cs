using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;

// Arreglo para que PostgreSQL acepte las fechas de las elecciones
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ---------------- CONFIGURACIÓN DE BASE DE DATOS ----------------
builder.Services.AddDbContext<APIVotosDbContext>(options =>
{
    // 1. Intentamos conectar a la variable de entorno de Render
    var envConnection = Environment.GetEnvironmentVariable("DefaultConnection");

    if (!string.IsNullOrEmpty(envConnection))
    {
        options.UseNpgsql(envConnection);
        Console.WriteLine("CONECTADO A: RENDER (Nube)");
    }
    else
    {
        // 2. Si no hay nube, buscamos en el appsettings.json
        var localConnection = builder.Configuration.GetConnectionString("APIVotosDbContext.postgresql");

        // Si la primera opción local es nula, probamos con la segunda
        if (string.IsNullOrEmpty(localConnection))
        {
            localConnection = builder.Configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrEmpty(localConnection))
        {
            throw new InvalidOperationException("No se encontró cadena de conexión ni en Render ni en Local");
        }

        options.UseNpgsql(localConnection);
        Console.WriteLine("CONECTADO A: LOCALHOST");
    }
});

// ---------------- CONTROLLERS ----------------
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

// ---------------- SWAGGER ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Activamos Swagger para pruebas en cualquier entorno
app.UseSwagger();
app.UseSwaggerUI();

// ---------------- CREACIÓN AUTOMÁTICA DE TABLAS ----------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<APIVotosDbContext>();
        context.Database.EnsureCreated();
        Console.WriteLine("Base de datos lista.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error en la BD: " + ex.Message);
    }
}

// ---------------- MIDDLEWARE ----------------
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();