using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

/*
 * The React app runs on its own port (4000), so the browser needs permission
 * to call this API from there (CORS).
 */
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactClient", policy =>
        policy.WithOrigins("http://localhost:4000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    /* API description and a test page at /scalar/v1 */
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("ReactClient");
app.UseAuthorization();
app.MapControllers();

app.Run();