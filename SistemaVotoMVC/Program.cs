using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

// Nuevo
using Microsoft.AspNetCore.DataProtection;
using System.IO;

using SistemaVotoMVC.Security;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers + Views (JSON flexible para la API)
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Nuevo
// Cookies compartidas con API (llaves dentro de la solución)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(
            Path.Combine(builder.Environment.ContentRootPath, "..", "SharedKeys")
        )
    )
    .SetApplicationName("SistemaVotoApp");

// 2. Autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SistemaVotoAuth";
        options.LoginPath = "/Aut/Login";           // Si no está logueado
        options.AccessDeniedPath = "/Aut/Login";    // Si no tiene rol
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// 3. Cliente HTTP para consumir la API
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardCookieHandler>();

builder.Services.AddHttpClient("SistemaVotoAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7062/");
})
.AddHttpMessageHandler<ForwardCookieHandler>();

// 4. HttpContextAccessor (claims, usuario, etc.)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 5. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANTE: Auth primero, luego Authorization
app.UseAuthentication();
app.UseAuthorization();

// 6. Ruta por defecto → Ventana inicial
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
