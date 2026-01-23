using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Controladores y Vistas con JSON flexible
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // LOGICA: Esto permite que el DTO en C# (NombreCompleto) acepte 
        // el JSON de la API (nombreCompleto) sin usar [JsonPropertyName]
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// 2. Configuración de Autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Aut/Login";      // Ruta si el usuario no está logueado
        options.AccessDeniedPath = "/Home/Privacy"; // Ruta si el usuario no tiene permisos
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Duración de la sesión
    });

// 3. Configuración del Cliente HTTP para conectar con la API
builder.Services.AddHttpClient("SistemaVotoAPI", client =>
{
    // Asegúrate de que este puerto coincida con el de tu SistemaVotoAPI
    client.BaseAddress = new Uri("https://localhost:7062/");
});

// 4. Inyección de IHttpContextAccessor (Útil para acceder a la sesión en las vistas)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 5. Configuración del Pipeline de HTTP (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// LOGICA: El orden aquí es CRÍTICO. 
// Primero Authentication (quién eres) y luego Authorization (qué puedes hacer)
app.UseAuthentication();
app.UseAuthorization();

// 6. Configuración de la Ruta por Defecto (Apunta a tu nuevo AutController)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Aut}/{action=Login}/{id?}");

app.Run();