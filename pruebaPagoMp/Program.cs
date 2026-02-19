using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using pruebaPagoMp.Data;
using pruebaPagoMp.Services;
using pruebaPagoMp.Models;
using pruebaPagoMp.Security;
using pruebaPagoMp.Services.Carritos;


using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ SERVICES

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// Services
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICarritoService, CarritoService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SecurityLogger>();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<CampoProtegidoService>();
builder.Services.AddScoped<ICarritoService, CarritoService>();
builder.Services.AddScoped<pruebaPagoMp.Services.Pagos.IMercadoPagoService, pruebaPagoMp.Services.Pagos.MercadoPagoService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<pruebaPagoMp.Services.Ventas.IVentasService, pruebaPagoMp.Services.Ventas.VentasService>();




// 🔐 AUTH + JWT

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddAuthorization();

// 2️⃣ BUILD

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // 1) asegurate que exista el rol AdminVentas
    var rol = await db.Roles.FirstOrDefaultAsync(r => r.Nombre == "AdminVentas");
    if (rol == null)
    {
        rol = new Rol { Nombre = "AdminVentas", Descripcion = "Admin de ventas mostrador" };
        db.Roles.Add(rol);
        await db.SaveChangesAsync();
    }

    // 2) crear usuario si no existe
    var email = "adminventas@demo.com";
    var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

    if (user == null)
    {
        user = new Usuario
        {
            Email = email,
            NombreCompleto = "Admin Ventas",
            Telefono = "000",
            Activo = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            DigitoVerificador = "seed" // ajusta si tu sistema lo calcula
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = user.Id, RolId = rol.Id });
        await db.SaveChangesAsync();
    }
}

// 3️⃣ MIDDLEWARE

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


var hash = BCrypt.Net.BCrypt.HashPassword("123456");
Console.WriteLine(hash);







//Codigo Seed de Roles
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new Rol { Nombre = "Cliente" },
            new Rol { Nombre = "AdminVentas" },
            new Rol { Nombre = "AdminCompras" },
            new Rol { Nombre = "AdminGeneral" }
        );

        context.SaveChanges();
    }
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.Usuarios.Any(u => u.Email == "admin@admin.com"))
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

        var admin = new Usuario
        {
            Email = "admin@admin.com",
            NombreCompleto = "Administrador General",
            PasswordHash = passwordHash,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            DigitoVerificador = "INIT"
        };

        context.Usuarios.Add(admin);
        context.SaveChanges();

        var rolAdmin = context.Roles.First(r => r.Nombre == "AdminGeneral");

        context.UsuarioRoles.Add(new UsuarioRol
        {
            UsuarioId = admin.Id,
            RolId = rolAdmin.Id
        });

        context.SaveChanges();
    }
}








app.UseRouting();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

