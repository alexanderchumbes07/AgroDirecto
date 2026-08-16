using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Data;
using AgroDirecto.Web.Hubs;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Controllers;

// Carrito y checkout. El ClienteId sale siempre de la cookie de sesión,
// nunca del formulario: si no, cualquiera podría comprar a nombre de otro.
[Authorize(Roles = "Cliente")]
public class CarritoController : Controller
{
    private readonly ICarritoRepositorio _repo;
    private readonly IHubContext<PedidosHub> _hub;
    private readonly IProductoRepositorio _productoRepo;
    private readonly IDistritoRepositorio _distritoRepo;

    public CarritoController(ICarritoRepositorio repo, IHubContext<PedidosHub> hub,
        IProductoRepositorio productoRepo, IDistritoRepositorio distritoRepo)
    {
        _repo = repo;
        _hub = hub;
        _productoRepo = productoRepo;
        _distritoRepo = distritoRepo;
    }

    // GET: /Carrito
    public IActionResult Index()
    {
        int? clienteId = ClienteActual();
        if (clienteId is null) return SinPerfil();

        var carrito = _repo.Obtener(clienteId.Value);
        carrito.DireccionEntrega = _repo.ObtenerDireccionCliente(UsuarioActual());
        carrito.Distritos = _distritoRepo.ListarTodos();

        return View(carrito);
    }

    // POST: /Carrito/Agregar
    [HttpPost]
    public IActionResult Agregar(int productoId, decimal cantidad = 1)
    {
        int? clienteId = ClienteActual();
        if (clienteId is null) return SinPerfil();

        var producto = _productoRepo.ObtenerPorId(productoId);
        if (producto != null && (cantidad * producto.Precio) < producto.MontoMinimo)
        {
            TempData["Error"] = $"Para llevar {producto.Nombre} debes superar el mínimo de S/ {producto.MontoMinimo:N2}.";
            return RedirectToAction("Index", "Catalogo"); 
        }

        try
        {
            int carritoId = _repo.ObtenerOCrearCarrito(clienteId.Value);
            _repo.Agregar(carritoId, productoId, cantidad);
            TempData["Exito"] = "Producto agregado al carrito.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    // POST: /Carrito/Actualizar
    [HttpPost]
    public IActionResult Actualizar(int detalleCarritoId, decimal cantidad)
    {
        int? clienteId = ClienteActual();
        if (clienteId is null) return SinPerfil();

        try
        {
            int carritoId = _repo.ObtenerOCrearCarrito(clienteId.Value);
            _repo.ActualizarCantidad(detalleCarritoId, carritoId, cantidad);
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    // POST: /Carrito/Quitar
    [HttpPost]
    public IActionResult Quitar(int detalleCarritoId)
    {
        int? clienteId = ClienteActual();
        if (clienteId is null) return SinPerfil();

        int carritoId = _repo.ObtenerOCrearCarrito(clienteId.Value);
        _repo.Quitar(detalleCarritoId, carritoId);

        TempData["Exito"] = "Producto quitado del carrito.";
        return RedirectToAction("Index");
    }

    // POST: /Carrito/Confirmar  → el checkout
    [HttpPost]
    public IActionResult Confirmar(int? distritoId, string direccionEntrega, string? referencia)
    {
        int? clienteId = ClienteActual();
        if (clienteId is null) return SinPerfil();

        if (distritoId is null)
        {
            TempData["Error"] = "Selecciona el distrito de entrega.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(direccionEntrega))
        {
            TempData["Error"] = "Indica una dirección de entrega.";
            return RedirectToAction("Index");
        }

        int compraId;
        try
        {
            compraId = _repo.RegistrarPedido(clienteId.Value, direccionEntrega.Trim(),
                distritoId, referencia?.Trim());
        }
        catch (SqlException ex)
        {
            // Aquí caen los avisos del procedimiento: carrito vacío o
            // stock insuficiente porque alguien compró antes.
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }

        /* La compra se repartió en un pedido por agricultor: se avisa a cada
           uno del suyo, para que le aparezca sin tener que recargar. */
        var compra = _repo.ObtenerCompra(compraId, clienteId.Value);
        if (compra is not null)
        {
            foreach (var pedido in compra.Pedidos)
                _hub.Clients.Group(PedidosHub.GrupoAgricultor(pedido.AgricultorId))
                    .SendAsync("PedidoNuevo", new
                    {
                        pedidoId = pedido.PedidoId,
                        cliente = User.Identity?.Name ?? "Un cliente",
                        total = pedido.Total,
                        items = pedido.Detalle.Count
                    });
        }

        TempData["Exito"] = $"¡Pedido #{compraId} registrado! Cada agricultor te contactará "
                          + "para coordinar la entrega de sus productos.";
        return RedirectToAction("Detalle", "Pedido", new { id = compraId });
    }

    // ---------- Apoyo ----------

    private int UsuarioActual()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out int usuarioId) ? usuarioId : 0;
    }

    private int? ClienteActual()
    {
        int usuarioId = UsuarioActual();
        return usuarioId == 0 ? null : _repo.ObtenerClienteIdPorUsuario(usuarioId);
    }

    private IActionResult SinPerfil()
    {
        TempData["Error"] = "Tu cuenta no tiene un perfil de cliente asociado.";
        return RedirectToAction("Index", "PanelCliente");
    }
}
