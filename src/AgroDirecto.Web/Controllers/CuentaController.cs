using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Data;
using AgroDirecto.Web.Models;
using AgroDirecto.Web.Seguridad;

namespace AgroDirecto.Web.Controllers;

// Registro, inicio y cierre de sesión.
public class CuentaController : Controller
{
    private readonly IUsuarioRepositorio _repo;

    public CuentaController(IUsuarioRepositorio repo) => _repo = repo;

    // ---------- LOGIN ----------

    [HttpGet]
    public IActionResult Login(string? volverA = null)
    {
        ViewBag.VolverA = volverA;
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? volverA = null)
    {
        ViewBag.VolverA = volverA;
        if (!ModelState.IsValid) return View(modelo);

        var usuario = _repo.ObtenerPorEmail(modelo.Email);

        // Mismo mensaje si el correo no existe o si la clave está mal:
        // así no se le confirma a nadie qué correos están registrados.
        if (usuario is null || !Password.Verificar(modelo.Password, usuario.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(modelo);
        }

        if (!usuario.Estado)
        {
            ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Comunícate con el administrador.");
            return View(modelo);
        }

        await IniciarSesion(usuario);

        if (!string.IsNullOrEmpty(volverA) && Url.IsLocalUrl(volverA))
            return Redirect(volverA);

        return RedirectToAction("Index", DestinoSegunRol(usuario.Rol));
    }

    // ---------- REGISTRO ----------

    // GET: /Cuenta/Registro?perfil=Agricultor
    // El parámetro llega desde los botones de la portada ("Soy agricultor" /
    // "Quiero comprar") y deja el perfil ya marcado en el formulario.
    [HttpGet]
    public IActionResult Registro(string? perfil = null)
    {
        CargarPerfiles();

        var modelo = new RegistroViewModel();

        if (!string.IsNullOrWhiteSpace(perfil))
        {
            var rol = _repo.ListarRoles()
                .FirstOrDefault(r => r.Nombre.Equals(perfil, StringComparison.OrdinalIgnoreCase)
                                     && r.Nombre != "Administrador");

            if (rol is not null) modelo.RolId = rol.RolId;
        }

        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Registro(RegistroViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            CargarPerfiles();
            return View(modelo);
        }

        try
        {
            _repo.Registrar(modelo, Password.Cifrar(modelo.Password));
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            CargarPerfiles();
            return View(modelo);
        }

        // Se inicia sesión automáticamente tras registrarse
        var usuario = _repo.ObtenerPorEmail(modelo.Email);
        if (usuario is not null)
        {
            await IniciarSesion(usuario);
            TempData["Exito"] = $"¡Bienvenido, {usuario.Nombres}! Tu cuenta fue creada.";
            return RedirectToAction("Index", DestinoSegunRol(usuario.Rol));
        }

        return RedirectToAction("Login");
    }

    // ---------- CERRAR SESIÓN ----------

    [HttpPost]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccesoDenegado() => View();

    // ---------- Apoyo ----------

    private async Task IniciarSesion(UsuarioViewModel usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol)
        };

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));
    }

    // Cada perfil aterriza en su propia sección al iniciar sesión.
    private static string DestinoSegunRol(string rol) => rol switch
    {
        "Administrador" => "PanelAdmin",
        "Agricultor"    => "PanelAgricultor",
        "Cliente"       => "PanelCliente",
        _               => "Home"
    };

    private void CargarPerfiles()
    {
        // Solo Cliente y Agricultor: el Administrador no se registra solo.
        ViewBag.Perfiles = _repo.ListarRoles()
            .Where(r => r.Nombre != "Administrador")
            .ToList();
    }
}
