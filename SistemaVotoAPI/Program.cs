using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

// Arreglo para que PostgreSQL acepte las fechas de las elecciones en la nube
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ---------------- DB ----------------
builder.Services.AddDbContext<APIVotosDbContext>(options =>
{
    var envConnection = Environment.GetEnvironmentVariable("DefaultConnection");

    if (!string.IsNullOrEmpty(envConnection))
    {
        options.UseNpgsql(envConnection); // Conexión remota Render
        Console.WriteLine("Usando base de datos RENDER");
    }
    else
    {
        var localConnection = builder.Configuration.GetConnectionString("APIVotosDbContext.postgresql")
            ?? builder.Configuration.GetConnectionString("DefaultConnection"); // Backup

        if (string.IsNullOrEmpty(localConnection))
        {
            throw new InvalidOperationException("No se encontró cadena de conexión ni en Render ni en Local");
        }

        options.UseNpgsql(localConnection); // Conexión local
        Console.WriteLine("Usando base de datos LOCAL");
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

// ---------------- SWAGGER ----------------
app.UseSwagger(); // Activado para ver en la nube
app.UseSwaggerUI();

// ---------------- CREACIÓN DB ----------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<APIVotosDbContext>();
        context.Database.EnsureCreated(); // Crea las tablas de votación automáticamente
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error creando la Base de Datos: " + ex.Message);
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();