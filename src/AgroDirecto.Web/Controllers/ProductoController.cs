using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Data;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Controllers;

// Catálogo propio del agricultor: publica y gestiona sus productos.
// Cada acción trabaja SOLO sobre los productos de quien inició sesión;
// el AgricultorId nunca llega desde el formulario, se saca de la cookie.
[Authorize(Roles = "Agricultor")]
public class ProductoController : Controller
{
    private readonly IProductoRepositorio _repo;
    private readonly ICategoriaRepositorio _categorias;
    private readonly IUnidadMedidaRepositorio _unidades;

    private const int Tamano = 5;

    public ProductoController(IProductoRepositorio repo,
                              ICategoriaRepositorio categorias,
                              IUnidadMedidaRepositorio unidades)
    {
        _repo = repo;
        _categorias = categorias;
        _unidades = unidades;
    }

    // GET: /Producto?buscar=palta&pagina=1
    public IActionResult Index(string? buscar, int pagina = 1)
    {
        int? agricultorId = AgricultorActual();
        if (agricultorId is null) return SinPerfil();

        if (pagina < 1) pagina = 1;

        var productos = _repo.ListarPorAgricultor(agricultorId.Value, buscar, pagina, Tamano, out int total);

        ViewBag.Buscar = buscar;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)Tamano);
        ViewBag.Total = total;

        return View(productos);
    }

    // GET: /Producto/Registrar
    [HttpGet]
    public IActionResult Registrar()
    {
        if (AgricultorActual() is null) return SinPerfil();

        CargarListas();
        return View(new ProductoViewModel());
    }

    // POST: /Producto/Registrar
    [HttpPost]
    public IActionResult Registrar(ProductoViewModel modelo)
    {
        int? agricultorId = AgricultorActual();
        if (agricultorId is null) return SinPerfil();

        if (!ModelState.IsValid)
        {
            CargarListas();
            return View(modelo);
        }

        modelo.AgricultorId = agricultorId.Value;

        try
        {
            _repo.Insertar(modelo);
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            CargarListas();
            return View(modelo);
        }

        TempData["Exito"] = $"Producto '{modelo.Nombre}' publicado.";
        return RedirectToAction("Index");
    }

    // GET: /Producto/Editar/5
    [HttpGet]
    public IActionResult Editar(int id)
    {
        int? agricultorId = AgricultorActual();
        if (agricultorId is null) return SinPerfil();

        var p = _repo.ObtenerPorId(id);
        if (p is null) return NotFound();

        // No basta con proteger el POST: sin esto, un agricultor podría
        // ver el formulario con los datos de un producto ajeno.
        if (p.AgricultorId != agricultorId.Value) return Forbid();

        CargarListas();
        return View(p);
    }

    // POST: /Producto/Editar
    [HttpPost]
    public IActionResult Editar(ProductoViewModel modelo)
    {
        int? agricultorId = AgricultorActual();
        if (agricultorId is null) return SinPerfil();

        if (!ModelState.IsValid)
        {
            CargarListas();
            return View(modelo);
        }

        modelo.AgricultorId = agricultorId.Value;

        try
        {
            _repo.Actualizar(modelo);
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            CargarListas();
            return View(modelo);
        }

        TempData["Exito"] = $"Producto '{modelo.Nombre}' actualizado.";
        return RedirectToAction("Index");
    }

    // POST: /Producto/Eliminar/5
    [HttpPost]
    public IActionResult Eliminar(int id)
    {
        int? agricultorId = AgricultorActual();
        if (agricultorId is null) return SinPerfil();

        try
        {
            _repo.Eliminar(id, agricultorId.Value);
            TempData["Exito"] = "Producto eliminado.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    // ---------- Apoyo ----------

    private int? AgricultorActual()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(id, out int usuarioId)) return null;

        return _repo.ObtenerAgricultorIdPorUsuario(usuarioId);
    }

    private IActionResult SinPerfil()
    {
        TempData["Error"] = "Tu cuenta no tiene un perfil de agricultor asociado.";
        return RedirectToAction("Index", "PanelAgricultor");
    }

    private void CargarListas()
    {
        ViewBag.Categorias = _categorias.ListarActivas();
        ViewBag.Unidades = _unidades.ListarTodas();
    }
}
