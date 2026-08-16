using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Data;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Controllers;

// CRUD de Unidades de Medida (módulo del Administrador).
[Authorize(Roles = "Administrador")]
public class UnidadMedidaController : Controller
{
    private readonly IUnidadMedidaRepositorio _repo;

    private const int Tamano = 5;

    public UnidadMedidaController(IUnidadMedidaRepositorio repo) => _repo = repo;

    // GET: /UnidadMedida?buscar=kilo&pagina=1
    public IActionResult Index(string? buscar, int pagina = 1)
    {
        if (pagina < 1) pagina = 1;

        var unidades = _repo.Listar(buscar, pagina, Tamano, out int total);

        ViewBag.Buscar = buscar;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)Tamano);
        ViewBag.Total = total;

        return View(unidades);
    }

    // GET: /UnidadMedida/Detalle/5
    public IActionResult Detalle(int id)
    {
        var u = _repo.ObtenerPorId(id);
        return u == null ? NotFound() : View(u);
    }

    // GET: /UnidadMedida/Registrar
    [HttpGet]
    public IActionResult Registrar() => View(new UnidadMedidaViewModel());

    // POST: /UnidadMedida/Registrar
    [HttpPost]
    public IActionResult Registrar(UnidadMedidaViewModel modelo)
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

        TempData["Exito"] = $"Unidad '{modelo.Nombre}' registrada.";
        return RedirectToAction("Index");
    }

    // GET: /UnidadMedida/Editar/5
    [HttpGet]
    public IActionResult Editar(int id)
    {
        var u = _repo.ObtenerPorId(id);
        return u == null ? NotFound() : View(u);
    }

    // POST: /UnidadMedida/Editar
    [HttpPost]
    public IActionResult Editar(UnidadMedidaViewModel modelo)
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

        TempData["Exito"] = $"Unidad '{modelo.Nombre}' actualizada.";
        return RedirectToAction("Index");
    }

    // POST: /UnidadMedida/Eliminar/5
    [HttpPost]
    public IActionResult Eliminar(int id)
    {
        try
        {
            _repo.Eliminar(id);
            TempData["Exito"] = "Unidad de medida eliminada.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }
}
