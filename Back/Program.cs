 // Asegúrate de que la ruta sea correcta

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios
builder.Services.AddControllers();
/*

*/
// Configuración de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar el pipeline de la aplicación
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Habilitar redirección de HTTP a HTTPS
app.UseAuthorization(); // Habilitar autorización

app.MapControllers(); // Mapear controladores

app.Run(); // Ejecutar la aplicación
