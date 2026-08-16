using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Data;
using AgroDirecto.Web.Hubs;

namespace AgroDirecto.Web.Controllers;

// Pedidos que le llegan al agricultor. Cada compra del cliente se reparte
// en un pedido por agricultor, así que aquí solo aparecen los suyos y
// solo él decide cuándo los confirma, entrega o cancela.
[Authorize(Roles = "Agricultor")]
public class PedidoAgricultorController : Controller
{
    private readonly ICarritoRepositorio _repo;
    private readonly IProductoRepositorio _productos;
    private readonly IHubContext<PedidosHub> _hub;

    public PedidoAgricultorController(ICarritoRepositorio repo, IProductoRepositorio productos,
                                      IHubContext<PedidosHub> hub)
    {
        _repo = repo;
        _productos = productos;
        _hub = hub;
    }

    // GET: /PedidoAgricultor
    public IActionResult Index()
    {
        int? agricultorId = AgricultorActual();
        if (agricultorId is null) return RedirectToAction("Index", "PanelAgricultor");

        var pedidos = _repo.ListarPedidosPorAgricultor(agricultorId.Value);

        // Cada pedido con sus productos, para que el agricultor sepa qué preparar.
        foreach (var pedido in pedidos)
            pedido.Detalle = _repo.ObtenerDetallePedido(pedido.PedidoId, agricultorId.Value);

        return View(pedidos);
    }

    // POST: /PedidoAgricultor/Confirmar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirmar(int id) => Mover(id, "Confirmado",
        "Pedido confirmado. Coordina la entrega con el cliente.");

    // POST: /PedidoAgricultor/Entregar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Entregar(int id) => Mover(id, "Entregado",
        "Pedido marcado como entregado.");

    // POST: /PedidoAgricultor/Cancelar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancelar(int id) => Mover(id, "Cancelado",
        "Pedido cancelado. El stock volvió a estar disponible.");

    /* El estado lo valida el procedimiento almacenado: comprueba que el
       pedido sea de este agricultor y que la transición tenga sentido.
       Esconder el botón no es seguridad; la regla vive en la base. */
    private IActionResult Mover(int id, string nuevoEstado, string mensaje)
    {
        int? agricultorId = AgricultorActual();
        if (agricultorId is null) return RedirectToAction("Index", "PanelAgricultor");

        try
        {
            var (clienteId, compraId) = _repo.CambiarEstadoPedido(id, agricultorId.Value, nuevoEstado);
            TempData["Exito"] = mensaje;

            /* Primero se guardó en la base; recién ahora se avisa. Si el
               aviso falla o el cliente tiene la página cerrada, no pasa
               nada: al recargar verá el estado correcto, porque la verdad
               está en SQL y no en la notificación. */
            if (clienteId > 0)
                _hub.Clients.Group(PedidosHub.GrupoCliente(clienteId))
                    .SendAsync("EstadoCambiado", new
                    {
                        compraId,
                        pedidoId = id,
                        estado = nuevoEstado,
                        agricultor = User.Identity?.Name ?? ""
                    });
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private int? AgricultorActual()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(id, out int usuarioId)) return null;

        return _productos.ObtenerAgricultorIdPorUsuario(usuarioId);
    }
}
