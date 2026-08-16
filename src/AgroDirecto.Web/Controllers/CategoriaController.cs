using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Data;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Controllers;

// CRUD de Categorías (módulo del Administrador).
[Authorize(Roles = "Administrador")]
public class CategoriaController : Controller
{
    private readonly ICategoriaRepositorio _repo;

    private const int Tamano = 5;   // filas por página

    public CategoriaController(ICategoriaRepositorio repo) => _repo = repo;

    // GET: /Categoria?buscar=frutas&pagina=1
    public IActionResult Index(string? buscar, int pagina = 1)
    {
        if (pagina < 1) pagina = 1;

        var categorias = _repo.Listar(buscar, pagina, Tamano, out int total);

        ViewBag.Buscar = buscar;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)Tamano);
        ViewBag.Total = total;

        return View(categorias);
    }

    // GET: /Categoria/Detalle/5
    public IActionResult Detalle(int id)
    {
        var c = _repo.ObtenerPorId(id);
        return c == null ? NotFound() : View(c);
    }

    // GET: /Categoria/Registrar
    [HttpGet]
    public IActionResult Registrar() => View(new CategoriaViewModel());

    // POST: /Categoria/Registrar
    [HttpPost]
    public IActionResult Registrar(CategoriaViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        try
        {
            _repo.Insertar(modelo);
        }
        catch (SqlException ex)
        {
            // Los procedimientos lanzan RAISERROR con mensajes para el usuario.
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        TempData["Exito"] = $"Categoría '{modelo.Nombre}' registrada.";
        return RedirectToAction("Index");
    }

    // GET: /Categoria/Editar/5
    [HttpGet]
    public IActionResult Editar(int id)
    {
        var c = _repo.ObtenerPorId(id);
        return c == null ? NotFound() : View(c);
    }

    // POST: /Categoria/Editar
    [HttpPost]
    public IActionResult Editar(CategoriaViewModel modelo)
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

        TempData["Exito"] = $"Categoría '{modelo.Nombre}' actualizada.";
        return RedirectToAction("Index");
    }

    // POST: /Categoria/Eliminar/5
    [HttpPost]
    public IActionResult Eliminar(int id)
    {
        try
        {
            _repo.Eliminar(id);
            TempData["Exito"] = "Categoría eliminada.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }
}
