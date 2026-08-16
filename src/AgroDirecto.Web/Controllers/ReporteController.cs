using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgroDirecto.Web.Data;

namespace AgroDirecto.Web.Controllers;

// Reportes de ventas (módulo del Administrador).
[Authorize(Roles = "Administrador")]
public class ReporteController : Controller
{
    private readonly IReporteRepositorio _repo;

    private const int Tamano = 10;

    public ReporteController(IReporteRepositorio repo) => _repo = repo;

    // Los tres reportes que pide el alcance, cada uno en su pestaña.
    private static readonly string[] Pestanas = { "productos", "agricultores", "detalle" };

    // GET: /Reporte?desde=2026-08-01&hasta=2026-08-31&agricultorId=1&pagina=1&tab=detalle
    public IActionResult Index(DateTime? desde, DateTime? hasta, int? agricultorId,
                               int pagina = 1, string tab = "productos")
    {
        if (pagina < 1) pagina = 1;

        // La pestaña llega por la URL, así que al paginar o filtrar se
        // vuelve a la misma en la que estaba el usuario.
        if (!Pestanas.Contains(tab)) tab = "productos";
        ViewBag.Tab = tab;

        // Si el rango viene al revés, se corrige en lugar de devolver vacío.
        if (desde.HasValue && hasta.HasValue && desde > hasta)
            (desde, hasta) = (hasta, desde);

        var ventas = _repo.Ventas(desde, hasta, agricultorId, pagina, Tamano, out int total);

        ViewBag.Desde = desde;
        ViewBag.Hasta = hasta;
        ViewBag.AgricultorId = agricultorId;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)Tamano);
        ViewBag.Total = total;

        ViewBag.Resumen = _repo.Resumen(desde, hasta, agricultorId);
        ViewBag.MasVendidos = _repo.ProductosMasVendidos(desde, hasta, agricultorId);
        ViewBag.PorAgricultor = _repo.VentasPorAgricultor(desde, hasta);
        ViewBag.Agricultores = _repo.ListarAgricultores();

        return View(ventas);
    }
}
