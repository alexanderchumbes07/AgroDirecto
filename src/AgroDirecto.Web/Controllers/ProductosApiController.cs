using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgroDirecto.Web.Data;

namespace AgroDirecto.Web.Controllers;

// Web API del catálogo. La página /Catalogo lo llama con AJAX y recibe
// JSON, sin recargar. Hereda de ControllerBase porque no devuelve vistas.
//
// Lleva el mismo [Authorize] que CatalogoController y no es opcional:
// aquí viaja la lista de precios. Si solo se protegiera la página, un
// agricultor entraría directo a /api/productosapi y leería el JSON.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cliente,Administrador")]
public class ProductosApiController : ControllerBase
{
    private readonly IProductoRepositorio _repo;

    public ProductosApiController(IProductoRepositorio repo) => _repo = repo;

    // GET: /api/productosapi?buscar=palta&categoriaId=2&distritoId=1&precioMax=10&pagina=1
    [HttpGet]
    public IActionResult Get(string? buscar, int? categoriaId, int? distritoId,
                             decimal? precioMax, int pagina = 1, int tamano = 8)
    {
        if (pagina < 1) pagina = 1;
        if (tamano < 1 || tamano > 48) tamano = 8;

        var productos = _repo.Catalogo(buscar, categoriaId, distritoId, precioMax,
                                       pagina, tamano, out int total);

        return Ok(new
        {
            total,
            pagina,
            totalPaginas = (int)Math.Ceiling(total / (double)tamano),
            productos = productos.Select(p => new
            {
                p.ProductoId,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.Stock,
                p.MontoMinimo,
                p.Unidad,
                p.Categoria,
                p.Agricultor,
                p.Distrito,
                p.ImagenUrl
            })
        });
    }
}
