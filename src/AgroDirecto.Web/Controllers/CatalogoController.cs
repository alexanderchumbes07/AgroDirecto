using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgroDirecto.Web.Data;

namespace AgroDirecto.Web.Controllers;

// El catálogo es para comprar, así que solo lo ve quien puede comprar.
// El Agricultor queda fuera a propósito: ahí están los precios de sus
// competidores y podría bajarlos para quitarles la venta.
// El Administrador entra para poder supervisar lo que se publica.
// La vista solo pinta el armazón y los filtros; los productos los trae
// el Web API por AJAX.
[Authorize(Roles = "Cliente,Administrador")]
public class CatalogoController : Controller
{
    private readonly ICategoriaRepositorio _categorias;
    private readonly IDistritoRepositorio _distritos;

    public CatalogoController(ICategoriaRepositorio categorias, IDistritoRepositorio distritos)
    {
        _categorias = categorias;
        _distritos = distritos;
    }

    // GET: /Catalogo
    public IActionResult Index()
    {
        ViewBag.Categorias = _categorias.ListarActivas();
        ViewBag.Distritos = _distritos.ListarTodos();
        return View();
    }
}
