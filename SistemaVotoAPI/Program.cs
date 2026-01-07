using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using SistemaVotoModelos; // Asegúrate de tener esta referencia para usar Rol y los modelos

[cite_start]// Arreglo para que PostgreSQL acepte las fechas de las elecciones [cite: 6]
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ---------------- CONFIGURACIÓN DE BASE DE DATOS ----------------
builder.Services.AddDbContext<APIVotosDbContext>(options =>
{
    [cite_start]// 1. Intentamos conectar a la variable de entorno de Render [cite: 7]
    var envConnection = Environment.GetEnvironmentVariable("DefaultConnection");

    if (!string.IsNullOrEmpty(envConnection))
    {
        options.UseNpgsql(envConnection);
        [cite_start] Console.WriteLine("CONECTADO A: RENDER (Nube) [cite: 7]");
    }
    else
    {
        [cite_start]// 2. Si no hay nube, buscamos en el appsettings.json 
        var localConnection = builder.Configuration.GetConnectionString("APIVotosDbContext.postgresql");

        if (string.IsNullOrEmpty(localConnection))
        {
            localConnection = builder.Configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrEmpty(localConnection))
        {
            [cite_start] throw new InvalidOperationException("No se encontró cadena de conexión ni en Render ni en Local [cite: 8]");
        }

        options.UseNpgsql(localConnection);
        [cite_start] Console.WriteLine("CONECTADO A: LOCALHOST [cite: 9]");
    }
});

// ---------------- CONTROLLERS ----------------
[cite_start]// Configuración para ignorar ciclos infinitos en las relaciones de los modelos 
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

// ---------------- SWAGGER ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

[cite_start]// Activamos Swagger para pruebas en cualquier entorno 
app.UseSwagger();
app.UseSwaggerUI();

// ---------------- CREACIÓN AUTOMÁTICA DE TABLAS Y SEMILLA (SEED) ----------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<APIVotosDbContext>();

        [cite_start]// Asegura que las tablas existan según los modelos [cite: 13]
        context.Database.EnsureCreated();
        [cite_start] Console.WriteLine("Base de datos lista[cite: 14].");

        // --- CARGA DE ROLES POR DEFECTO ---
        // Esto verifica si la tabla de Roles está vacía para insertar los datos iniciales
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Rol { Nombre = "Administrador" },
                new Rol { Nombre = "Votante" },
                new Rol { Nombre = "Candidato" }
            );
            context.SaveChanges();
            Console.WriteLine("Roles iniciales cargados en la base de datos.");
        }
    }
    catch (Exception ex)
    {
        [cite_start] Console.WriteLine("Error en la BD: " + ex.Message[cite: 15]);
    }
}

// ---------------- MIDDLEWARE ----------------
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();