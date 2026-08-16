using Microsoft.AspNetCore.Authentication.Cookies;
using AgroDirecto.Web.Data;
using AgroDirecto.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// SignalR: notificaciones del servidor al navegador sin recargar.
// Viene incluido en ASP.NET Core, no hace falta ningún paquete NuGet.
builder.Services.AddSignalR();

// Inyección de dependencias: los repositorios se registran contra su interfaz.
builder.Services.AddScoped<ConexionBD>();
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IUnidadMedidaRepositorio, UnidadMedidaRepositorio>();
builder.Services.AddScoped<IDistritoRepositorio, DistritoRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
builder.Services.AddScoped<ICarritoRepositorio, CarritoRepositorio>();
builder.Services.AddScoped<IReporteRepositorio, ReporteRepositorio>();

// Autenticación por cookie: al iniciar sesión se guarda una cookie
// firmada con el rol del usuario, y [Authorize] la lee en cada petición.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.ReturnUrlParameter = "volverA";
        opciones.ExpireTimeSpan = TimeSpan.FromHours(4);
        opciones.SlidingExpiration = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();

// El orden importa: primero se identifica al usuario, luego se
// comprueba si tiene permiso.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Rutas por atributo: las usa el Web API del catálogo (/api/productosapi)
app.MapControllers();

// Dirección a la que se conecta el navegador para recibir avisos.
// Va después de UseAuthentication: el Hub necesita saber quién es el
// usuario para meterlo en su grupo.
app.MapHub<PedidosHub>("/hub/pedidos");

app.Run();
