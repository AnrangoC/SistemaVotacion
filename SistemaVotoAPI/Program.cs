// Nuevo
using Microsoft.AspNetCore.Authentication.Cookies;
﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Services.EmailServices;
using System;
using System.IO;

// Configuración global para compatibilidad con fechas en PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Nuevo
// Cookies compartidas con MVC (llaves dentro de la solución)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(
            Path.Combine(builder.Environment.ContentRootPath, "..", "SharedKeys")
        )
    )
    .SetApplicationName("SistemaVotoApp");

// Nuevo
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SistemaVotoAuth";
        options.SlidingExpiration = true;
    });

// Nuevo
builder.Services.AddAuthorization();

// Configuración de la base de datos
// La API soporta múltiples entornos:
// Producción en Render mediante variable de entorno
// Desarrollo local con PostgreSQL local
// Desarrollo local conectado a PostgreSQL en Render

builder.Services.AddDbContext<APIVotosDbContext>(options =>
{
    var envConnection = Environment.GetEnvironmentVariable("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(envConnection))
    {
        options.UseNpgsql(envConnection);
        Console.WriteLine("Base de datos configurada mediante variable de entorno");
    }
    else
    {
        var localConnection = builder.Configuration
            .GetConnectionString("APIVotosDbContext.postgresql");

        if (string.IsNullOrWhiteSpace(localConnection))
        {
            localConnection = builder.Configuration
                .GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(localConnection))
        {
            throw new InvalidOperationException(
                "No se encontró ninguna cadena de conexión válida"
            );
        }

        options.UseNpgsql(localConnection);
        Console.WriteLine("Base de datos configurada mediante appsettings.json");
    }
});

// Configuración de controladores y serialización JSON
// Se ignoran referencias circulares entre entidades

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling =
            ReferenceLoopHandling.Ignore;
    });

// Configuración de Swagger para documentación y pruebas de la API

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Registro del servicio de correo electrónico para inyección de dependencias
builder.Services.AddScoped<IEmailServices,EmailServices>();


//Servicio de envío de correos electrónicos
builder.Services.AddScoped<IEmailServices, EmailServices>();

var app = builder.Build();

// Middleware de documentación

app.UseSwagger();
app.UseSwaggerUI();

// Pipeline final de la aplicación

app.UseHttpsRedirection();

// Nuevo
app.UseAuthentication();

app.UseAuthorization();
app.MapControllers();
app.Run();
