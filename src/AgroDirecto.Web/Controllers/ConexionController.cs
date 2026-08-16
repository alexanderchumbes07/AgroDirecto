using Microsoft.AspNetCore.Mvc;
using AgroDirecto.Web.Data;

namespace AgroDirecto.Web.Controllers;

// Pantalla de diagnóstico: sirve para que quien clona el proyecto
// compruebe que su cadena de conexión funciona antes de nada.
//
// Muestra el nombre del servidor y de la base, así que SOLO responde
// en modo desarrollo. Publicada, sería regalarle a cualquiera parte de
// la configuración del servidor. No está en el menú: se entra a mano.
public class ConexionController : Controller
{
    private readonly ConexionBD _bd;
    private readonly IHostEnvironment _entorno;

    public ConexionController(ConexionBD bd, IHostEnvironment entorno)
    {
        _bd = bd;
        _entorno = entorno;
    }

    // GET: /Conexion
    public IActionResult Index()
    {
        // Fuera de desarrollo esta pantalla no existe.
        if (!_entorno.IsDevelopment()) return NotFound();

        bool ok = _bd.ProbarConexion(out string mensaje);

        ViewBag.Exito = ok;
        ViewBag.Mensaje = mensaje;

        if (ok)
        {
            try { ViewBag.Total = _bd.ContarCategorias(); }
            catch (Exception ex) { ViewBag.Mensaje += $" | Pero falló la consulta: {ex.Message}"; }
        }

        return View();
    }
}
