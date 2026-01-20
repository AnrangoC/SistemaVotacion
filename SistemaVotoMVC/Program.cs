using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Esencial para que funcione el Login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Aut/Login";
        options.AccessDeniedPath = "/Home/Privacy";
    });

builder.Services.AddHttpClient("SistemaVotoAPI", client => {
    client.BaseAddress = new Uri("https://localhost:7062/"); // Tu puerto de la API
});

var app = builder.Build();

// ... otros middlewares
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication(); // Debe ir antes de Authorization
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Aut}/{action=Login}/{id?}");
app.Run();