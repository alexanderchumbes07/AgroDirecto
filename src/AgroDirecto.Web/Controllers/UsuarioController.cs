using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Data;

namespace AgroDirecto.Web.Controllers;

// Mantenimiento de usuarios (módulo del Administrador).
// No se crean ni se eliminan usuarios desde aquí, ni se tocan
// contraseñas: solo se activan cuentas y se aprueban agricultores.
[Authorize(Roles = "Administrador")]
public class UsuarioController : Controller
{
    private readonly IUsuarioRepositorio _repo;

    private const int Tamano = 8;

    public UsuarioController(IUsuarioRepositorio repo) => _repo = repo;

    // GET: /Usuario?buscar=rosa&rolId=2&pagina=1
    public IActionResult Index(string? buscar, int? rolId, int pagina = 1)
    {
        if (pagina < 1) pagina = 1;

        var usuarios = _repo.Listar(buscar, rolId, pagina, Tamano, out int total);

        ViewBag.Buscar = buscar;
        ViewBag.RolId = rolId;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)Tamano);
        ViewBag.Total = total;
        ViewBag.Roles = _repo.ListarRoles();

        return View(usuarios);
    }

    // POST: /Usuario/CambiarEstado
    [HttpPost]
    public IActionResult CambiarEstado(int usuarioId, bool estado, string? buscar, int? rolId, int pagina = 1)
    {
        try
        {
            _repo.CambiarEstado(usuarioId, estado);
            TempData["Exito"] = estado ? "Cuenta activada." : "Cuenta desactivada.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index", new { buscar, rolId, pagina });
    }

    // POST: /Usuario/CambiarAprobacion
    [HttpPost]
    public IActionResult CambiarAprobacion(int agricultorId, bool aprobado, string? buscar, int? rolId, int pagina = 1)
    {
        try
        {
            _repo.CambiarAprobacion(agricultorId, aprobado);
            TempData["Exito"] = aprobado
                ? "Agricultor aprobado. Ya puede vender en la plataforma."
                : "Se retiró la aprobación al agricultor.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index", new { buscar, rolId, pagina });
    }
}
