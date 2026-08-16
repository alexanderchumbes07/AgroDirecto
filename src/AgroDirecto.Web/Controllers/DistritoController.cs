using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Data;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Controllers;

// CRUD de Distritos (módulo del Administrador).
[Authorize(Roles = "Administrador")]
public class DistritoController : Controller
{
    private readonly IDistritoRepositorio _repo;

    private const int Tamano = 5;

    public DistritoController(IDistritoRepositorio repo) => _repo = repo;

    // GET: /Distrito?buscar=lima&pagina=1
    public IActionResult Index(string? buscar, int pagina = 1)
    {
        if (pagina < 1) pagina = 1;

        var distritos = _repo.Listar(buscar, pagina, Tamano, out int total);

        ViewBag.Buscar = buscar;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)Tamano);
        ViewBag.Total = total;

        return View(distritos);
    }

    // GET: /Distrito/Detalle/5
    public IActionResult Detalle(int id)
    {
        var d = _repo.ObtenerPorId(id);
        return d == null ? NotFound() : View(d);
    }

    // GET: /Distrito/Registrar
    [HttpGet]
    public IActionResult Registrar() => View(new DistritoViewModel());

    // POST: /Distrito/Registrar
    [HttpPost]
    public IActionResult Registrar(DistritoViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _repo.Insertar(modelo);
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        TempData["Exito"] = $"Distrito '{modelo.Nombre}' registrado.";
        return RedirectToAction("Index");
    }

    // GET: /Distrito/Editar/5
    [HttpGet]
    public IActionResult Editar(int id)
    {
        var d = _repo.ObtenerPorId(id);
        return d == null ? NotFound() : View(d);
    }

    // POST: /Distrito/Editar
    [HttpPost]
    public IActionResult Editar(DistritoViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _repo.Actualizar(modelo);
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        TempData["Exito"] = $"Distrito '{modelo.Nombre}' actualizado.";
        return RedirectToAction("Index");
    }

    // POST: /Distrito/Eliminar/5
    [HttpPost]
    public IActionResult Eliminar(int id)
    {
        try
        {
            _repo.Eliminar(id);
            TempData["Exito"] = "Distrito eliminado.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }
}
