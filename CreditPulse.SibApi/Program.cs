using System.Text;
using CreditPulse.SibSimulador.Models;

// API de la SIB (simulada). Mismo contrato que CreditPulse.SibSimulador:
//   GET /api/perfil?dpi=...&nit=...&nombre=...   ->  XML del perfil crediticio
//   GET /health                                  ->  "ok" (para el healthcheck del contenedor)
// Llave opcional: si existe la variable de entorno SIB_API_KEY, cada petición a
// /api/perfil debe enviar el header "X-Api-Key" con ese valor.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string llaveConfigurada = Environment.GetEnvironmentVariable("SIB_API_KEY") ?? string.Empty;

app.MapGet("/health", () => Results.Text("ok", "text/plain"));

app.MapGet("/api/perfil", (HttpRequest request, string? dpi, string? nit, string? nombre) =>
{
    if (llaveConfigurada.Length > 0)
    {
        string recibida = request.Headers["X-Api-Key"].ToString();
        if (!CryptographicEquals(recibida, llaveConfigurada))
        {
            return Results.Text("Falta o es inválido el header \"X-Api-Key\".", "text/plain", Encoding.UTF8, StatusCodes.Status401Unauthorized);
        }
    }

    if (string.IsNullOrWhiteSpace(dpi))
    {
        return Results.Text("El parámetro 'dpi' es obligatorio.", "text/plain", Encoding.UTF8, StatusCodes.Status400BadRequest);
    }

    SibPerfilSimulado perfil = SibPerfilGenerator.Generar(dpi, nit ?? string.Empty, nombre ?? string.Empty);
    string xml = SibPerfilGenerator.SerializarXml(perfil);
    return Results.Text(xml, "application/xml", Encoding.UTF8);
});

app.Run();

static bool CryptographicEquals(string a, string b)
{
    return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
