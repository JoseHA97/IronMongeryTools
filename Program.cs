using System.Text.Json;
using IronMongeryTools.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Habilitar sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();

// Registro del servicio de correo electrónico y acceso
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAccesoService, AccesoService>();
builder.Services.AddHttpContextAccessor();

// Configuración de la autenticación con cookies
builder.Services.AddAuthentication("CustomCookieAuth")
    .AddCookie("CustomCookieAuth", options =>
    {
        options.LoginPath = "/Acceso/Index";  // Ruta de inicio de sesión
        options.LogoutPath = "/Acceso/Logout"; // Ruta de cierre de sesión
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);  // Expiración de la cookie
        options.AccessDeniedPath = "/Home/AccessDenied";  // Ruta cuando el acceso es denegado
        options.Cookie.HttpOnly = true; // La cookie solo es accesible desde el servidor
        options.SlidingExpiration = true; // Renovar la cookie si el usuario está activo
    });

// Configuración de la autorización con roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("Usuario"));
    options.AddPolicy("AdminOrUser", policy => policy.RequireRole("Administrador", "Usuario"));
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Configurar middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");  // Controlador de errores
    app.UseHsts();  // Enforce HTTPS
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();  // Habilitar el middleware de sesión
app.UseAuthentication();  // Middleware de autenticación
app.UseAuthorization();   // Middleware de autorización

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Index}/{id?}");

app.Run();
