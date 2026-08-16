using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgroDirecto.Web.Data;

namespace AgroDirecto.Web.Controllers;

// Compras del cliente. Cada compra puede repartirse en varios pedidos,
// uno por agricultor, pero el cliente la ve como una sola.
[Authorize(Roles = "Cliente")]
public class PedidoController : Controller
{
    private readonly ICarritoRepositorio _repo;

    public PedidoController(ICarritoRepositorio repo) => _repo = repo;

    // GET: /Pedido
    public IActionResult Index()
    {
        int? clienteId = ClienteActual();
        if (clienteId is null) return RedirectToAction("Index", "PanelCliente");

        return View(_repo.ListarComprasPorCliente(clienteId.Value));
    }

    // GET: /Pedido/Detalle/5
    public IActionResult Detalle(int id)
    {
        int? clienteId = ClienteActual();
        if (clienteId is null) return RedirectToAction("Index", "PanelCliente");

        // El procedimiento comprueba que la compra sea suya; si no lo es,
        // no devuelve nada y para este cliente simplemente no existe.
        var compra = _repo.ObtenerCompra(id, clienteId.Value);
        if (compra is null) return NotFound();

        return View(compra);
    }

    private int? ClienteActual()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(id, out int usuarioId)) return null;

        return _repo.ObtenerClienteIdPorUsuario(usuarioId);
    }
}
