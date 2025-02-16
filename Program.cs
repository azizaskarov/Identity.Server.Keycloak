using Identity.Server.Keycloak.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Keycloak sozlamalarini yuklash
builder.Services.Configure<KeycloakSettings>(builder.Configuration.GetSection("Keycloak"));

// 🔹 HttpClient va Keycloak xizmatini ro‘yxatdan o‘tkazish
builder.Services.AddHttpClient<KeycloakService>();
builder.Services.AddScoped<KeycloakService>();

// 🔹 CORS ruxsat berish
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// 🔹 Controllers va Swagger qo‘shish
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔹 HTTPS majburiy emas (local dev uchun)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 CORS yoqish
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthentication(); // 🔹 Authentifikatsiya ishlatish
app.UseAuthorization();

app.MapControllers();

app.Run();