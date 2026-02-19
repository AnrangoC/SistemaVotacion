using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
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

// 2. Cookies compartidas con API
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(
            Path.Combine(builder.Environment.ContentRootPath, "..", "SharedKeys")
        )
    )
    .SetApplicationName("SistemaVotoApp");

// 3. Autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SistemaVotoAuth";
        options.LoginPath = "/Aut/Login";
        options.AccessDeniedPath = "/Aut/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// 4. Cliente HTTP para consumir la API
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardCookieHandler>();

var apiBaseUrl =
    builder.Configuration["ApiSettings:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("ApiSettings__BaseUrl")
    ?? "https://localhost:7062/"; // fallback local

builder.Services.AddHttpClient("SistemaVotoAPI", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<ForwardCookieHandler>();

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

app.UseAuthentication();
app.UseAuthorization();

// 6. Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

