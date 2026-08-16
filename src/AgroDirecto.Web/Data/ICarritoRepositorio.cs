using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public interface ICarritoRepositorio
{
    int? ObtenerClienteIdPorUsuario(int usuarioId);
    string ObtenerDireccionCliente(int usuarioId);

    // Devuelve el carrito activo del cliente; lo crea si no existe.
    int ObtenerOCrearCarrito(int clienteId);

    CarritoViewModel Obtener(int clienteId);

    void Agregar(int carritoId, int productoId, decimal cantidad);
    void ActualizarCantidad(int detalleCarritoId, int carritoId, decimal cantidad);
    void Quitar(int detalleCarritoId, int carritoId);

    // Checkout: transacción que crea la compra, la reparte en un pedido por
    // agricultor y descuenta el stock. Devuelve el CompraId.
    int RegistrarPedido(int clienteId, string direccionEntrega);

    // Lo que ve el cliente: sus compras, con el detalle agrupado por proveedor.
    List<CompraViewModel> ListarComprasPorCliente(int clienteId);
    CompraViewModel? ObtenerCompra(int compraId, int? clienteId);

    // Lo que ve el agricultor: solo los pedidos que le tocan.
    List<PedidoViewModel> ListarPedidosPorAgricultor(int agricultorId);
    List<PedidoDetalleViewModel> ObtenerDetallePedido(int pedidoId, int? agricultorId);

    // Devuelve a quién hay que avisar del cambio, para la notificación
    // en tiempo real: el cliente dueño de la compra y el número de compra.
    (int ClienteId, int CompraId) CambiarEstadoPedido(int pedidoId, int agricultorId, string nuevoEstado);
}
