using Microsoft.EntityFrameworkCore;
using Webapi.Data;

var builder = WebApplication.CreateBuilder(args);
// El proveedor de configuración lee ConnectionStrings__DefaultConnection del entorno.
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"];
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection; consulte .env.example.");
connection = DatabaseConnection.Normalize(connection);
// Scaffold resuelve Name=ConnectionStrings:DefaultConnection desde esta configuracion.
builder.Configuration["ConnectionStrings:DefaultConnection"] = connection;
builder.Services.AddDbContext<VeterinariaContext>(options => options.UseNpgsql(connection));
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5227"];
if (origins.Length == 0 || origins.Any(origin => origin == "*" ||
    !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
    (uri.Scheme != "http" && uri.Scheme != "https") || uri.AbsolutePath != "/" ||
    uri.Query.Length > 0 || uri.Fragment.Length > 0 || origin.EndsWith('/')))
    throw new InvalidOperationException("CORS requiere orígenes HTTP(S) concretos sin ruta ni barra final.");
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseExceptionHandler();
// Render termina HTTPS en su proxy. El contenedor escucha HTTP internamente.
// En el perfil local HTTP no hay certificado/puerto HTTPS que redirigir.
if (app.Environment.IsDevelopment() && builder.Configuration["LocalDatabase:Enabled"] != "true")
    app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("Frontend");
// Swagger se publica también en producción por requisito del laboratorio.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Veterinaria API v1");
    options.RoutePrefix = string.Empty;
});
app.MapControllers();
app.Run();
