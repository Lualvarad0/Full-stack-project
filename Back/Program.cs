 // Aseg�rate de que la ruta sea correcta

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios
builder.Services.AddControllers();
/*

*/
// Configuraci�n de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar el pipeline de la aplicaci�n
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Habilitar redirecci�n de HTTP a HTTPS
app.UseAuthorization(); // Habilitar autorizaci�n

app.MapControllers(); // Mapear controladores

app.Run(); // Ejecutar la aplicaci�n
