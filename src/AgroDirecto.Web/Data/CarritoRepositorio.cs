using System.Data;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public class CarritoRepositorio : ICarritoRepositorio
{
    private readonly ConexionBD _bd;

    public CarritoRepositorio(ConexionBD bd) => _bd = bd;

    public int? ObtenerClienteIdPorUsuario(int usuarioId)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Cliente_ObtenerPorUsuario", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? dr.GetInt32(0) : null;
    }

    public string ObtenerDireccionCliente(int usuarioId)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Cliente_ObtenerPorUsuario", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? dr.GetString(dr.GetOrdinal("Direccion")) : string.Empty;
    }

    public int ObtenerOCrearCarrito(int clienteId)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Carrito_ObtenerOCrear", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ClienteId", clienteId);

        var pId = new SqlParameter("@CarritoId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pId);

        cmd.ExecuteNonQuery();

        return (int)pId.Value;
    }

    public CarritoViewModel Obtener(int clienteId)
    {
        var carrito = new CarritoViewModel { CarritoId = ObtenerOCrearCarrito(clienteId) };

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Carrito_Listar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@CarritoId", carrito.CarritoId);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            int iImg = dr.GetOrdinal("ImagenUrl");

            carrito.Items.Add(new CarritoItemViewModel
            {
                DetalleCarritoId = dr.GetInt32(dr.GetOrdinal("DetalleCarritoId")),
                ProductoId       = dr.GetInt32(dr.GetOrdinal("ProductoId")),
                Cantidad         = dr.GetDecimal(dr.GetOrdinal("Cantidad")),
                PrecioUnitario   = dr.GetDecimal(dr.GetOrdinal("PrecioUnitario")),
                Subtotal         = dr.GetDecimal(dr.GetOrdinal("Subtotal")),
                Nombre           = dr.GetString(dr.GetOrdinal("Nombre")),
                Stock            = dr.GetDecimal(dr.GetOrdinal("Stock")),
                ImagenUrl        = dr.IsDBNull(iImg) ? null : dr.GetString(iImg),
                Unidad           = dr.GetString(dr.GetOrdinal("Unidad")),
                Agricultor       = dr.GetString(dr.GetOrdinal("Agricultor"))
            });
        }

        return carrito;
    }

    public void Agregar(int carritoId, int productoId, decimal cantidad)
    {
        Ejecutar("usp_Carrito_Agregar", cmd =>
        {
            cmd.Parameters.AddWithValue("@CarritoId", carritoId);
            cmd.Parameters.AddWithValue("@ProductoId", productoId);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
        });
    }

    public void ActualizarCantidad(int detalleCarritoId, int carritoId, decimal cantidad)
    {
        Ejecutar("usp_Carrito_ActualizarCantidad", cmd =>
        {
            cmd.Parameters.AddWithValue("@DetalleCarritoId", detalleCarritoId);
            cmd.Parameters.AddWithValue("@CarritoId", carritoId);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
        });
    }

    public void Quitar(int detalleCarritoId, int carritoId)
    {
        Ejecutar("usp_Carrito_Quitar", cmd =>
        {
            cmd.Parameters.AddWithValue("@DetalleCarritoId", detalleCarritoId);
            cmd.Parameters.AddWithValue("@CarritoId", carritoId);
        });
    }

    // ---------- Checkout ----------

    public int RegistrarPedido(int clienteId, string direccionEntrega, int? distritoId, string? referencia)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Pedido_Registrar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ClienteId", clienteId);
        cmd.Parameters.AddWithValue("@DireccionEntrega", direccionEntrega);
        cmd.Parameters.AddWithValue("@DistritoId", (object?)distritoId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Referencia", (object?)referencia ?? DBNull.Value);

        var pId = new SqlParameter("@CompraId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pId);

        cmd.ExecuteNonQuery();

        return pId.Value == DBNull.Value ? 0 : (int)pId.Value;
    }

    // ---------- Compras (cliente) ----------

    public List<CompraViewModel> ListarComprasPorCliente(int clienteId)
    {
        var lista = new List<CompraViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Compra_ListarPorCliente", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ClienteId", clienteId);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(new CompraViewModel
            {
                CompraId         = dr.GetInt32(dr.GetOrdinal("CompraId")),
                FechaCompra      = dr.GetDateTime(dr.GetOrdinal("FechaCompra")),
                Total            = dr.GetDecimal(dr.GetOrdinal("Total")),
                DireccionEntrega = Texto(dr, "DireccionEntrega"),
                Distrito         = Texto(dr, "Distrito"),
                Referencia       = Texto(dr, "Referencia"),
                Proveedores      = dr.GetInt32(dr.GetOrdinal("Proveedores")),
                Items            = dr.GetInt32(dr.GetOrdinal("Items")),
                Estado           = dr.GetString(dr.GetOrdinal("Estado"))
            });

        return lista;
    }

    /* El procedimiento devuelve una fila por producto arrastrando los datos
       del pedido y del agricultor. Aquí se rearma el árbol:
       compra -> pedidos (uno por proveedor) -> productos. */
    public CompraViewModel? ObtenerCompra(int compraId, int? clienteId)
    {
        CompraViewModel? compra = null;
        var pedidos = new Dictionary<int, PedidoViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Compra_ObtenerDetalle", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@CompraId", compraId);
        cmd.Parameters.AddWithValue("@ClienteId", (object?)clienteId ?? DBNull.Value);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            compra ??= new CompraViewModel
            {
                CompraId         = dr.GetInt32(dr.GetOrdinal("CompraId")),
                FechaCompra      = dr.GetDateTime(dr.GetOrdinal("FechaCompra")),
                Total            = dr.GetDecimal(dr.GetOrdinal("TotalCompra")),
                DireccionEntrega = Texto(dr, "DireccionEntrega"),
                Distrito         = Texto(dr, "Distrito"),
                Referencia       = Texto(dr, "Referencia")
            };

            int pedidoId = dr.GetInt32(dr.GetOrdinal("PedidoId"));

            if (!pedidos.TryGetValue(pedidoId, out var pedido))
            {
                pedido = new PedidoViewModel
                {
                    PedidoId           = pedidoId,
                    CompraId           = compra.CompraId,
                    AgricultorId       = dr.GetInt32(dr.GetOrdinal("AgricultorId")),
                    Total              = dr.GetDecimal(dr.GetOrdinal("TotalPedido")),
                    Estado             = dr.GetString(dr.GetOrdinal("Estado")),
                    Agricultor         = dr.GetString(dr.GetOrdinal("Agricultor")),
                    DistritoAgricultor = Texto(dr, "DistritoAgricultor"),
                    TelefonoAgricultor = Texto(dr, "TelefonoAgricultor")
                };
                pedidos.Add(pedidoId, pedido);
                compra.Pedidos.Add(pedido);
            }

            pedido.Detalle.Add(new PedidoDetalleViewModel
            {
                ProductoId     = dr.GetInt32(dr.GetOrdinal("ProductoId")),
                Nombre         = dr.GetString(dr.GetOrdinal("Producto")),
                ImagenUrl      = Texto(dr, "ImagenUrl"),
                Cantidad       = dr.GetDecimal(dr.GetOrdinal("Cantidad")),
                Unidad         = dr.GetString(dr.GetOrdinal("Unidad")),
                PrecioUnitario = dr.GetDecimal(dr.GetOrdinal("PrecioUnitario")),
                Subtotal       = dr.GetDecimal(dr.GetOrdinal("Subtotal")),
                Agricultor     = dr.GetString(dr.GetOrdinal("Agricultor"))
            });
        }

        if (compra != null)
        {
            compra.Proveedores = compra.Pedidos.Count;
            compra.Items = compra.Pedidos.Sum(p => p.Detalle.Count);
        }

        return compra;
    }

    // ---------- Pedidos (agricultor) ----------

    public List<PedidoViewModel> ListarPedidosPorAgricultor(int agricultorId)
    {
        var lista = new List<PedidoViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Pedido_ListarPorAgricultor", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@AgricultorId", agricultorId);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(new PedidoViewModel
            {
                PedidoId         = dr.GetInt32(dr.GetOrdinal("PedidoId")),
                CompraId         = dr.GetInt32(dr.GetOrdinal("CompraId")),
                FechaPedido      = dr.GetDateTime(dr.GetOrdinal("FechaPedido")),
                Estado           = dr.GetString(dr.GetOrdinal("Estado")),
                Cliente          = dr.GetString(dr.GetOrdinal("Cliente")),
                TelefonoCliente  = Texto(dr, "TelefonoCliente"),
                DireccionEntrega  = Texto(dr, "DireccionEntrega"),
                DistritoEntrega   = Texto(dr, "Distrito"),
                ReferenciaEntrega = Texto(dr, "Referencia"),
                Total             = dr.GetDecimal(dr.GetOrdinal("Total")),
                Items             = dr.GetInt32(dr.GetOrdinal("Items"))
            });

        return lista;
    }

    public List<PedidoDetalleViewModel> ObtenerDetallePedido(int pedidoId, int? agricultorId)
    {
        var lista = new List<PedidoDetalleViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Pedido_ObtenerDetalle", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@PedidoId", pedidoId);
        cmd.Parameters.AddWithValue("@AgricultorId", (object?)agricultorId ?? DBNull.Value);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(new PedidoDetalleViewModel
            {
                ProductoId     = dr.GetInt32(dr.GetOrdinal("ProductoId")),
                Nombre         = dr.GetString(dr.GetOrdinal("Producto")),
                ImagenUrl      = Texto(dr, "ImagenUrl"),
                Cantidad       = dr.GetDecimal(dr.GetOrdinal("Cantidad")),
                Unidad         = dr.GetString(dr.GetOrdinal("Unidad")),
                PrecioUnitario = dr.GetDecimal(dr.GetOrdinal("PrecioUnitario")),
                Subtotal       = dr.GetDecimal(dr.GetOrdinal("Subtotal"))
            });

        return lista;
    }

    // El agricultorId sale de la sesión, nunca del formulario: el
    // procedimiento lo usa para comprobar que el pedido es suyo.
    // Devuelve el cliente y la compra afectados, para poder avisarle.
    public (int ClienteId, int CompraId) CambiarEstadoPedido(int pedidoId, int agricultorId, string nuevoEstado)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Pedido_CambiarEstado", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@PedidoId", pedidoId);
        cmd.Parameters.AddWithValue("@AgricultorId", agricultorId);
        cmd.Parameters.AddWithValue("@NuevoEstado", nuevoEstado);

        var pCliente = new SqlParameter("@ClienteId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var pCompra  = new SqlParameter("@CompraId",  SqlDbType.Int) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pCliente);
        cmd.Parameters.Add(pCompra);

        cmd.ExecuteNonQuery();

        return (pCliente.Value == DBNull.Value ? 0 : (int)pCliente.Value,
                pCompra.Value  == DBNull.Value ? 0 : (int)pCompra.Value);
    }

    // ---------- Apoyo ----------

    // Columnas de texto que pueden venir en NULL
    private static string Texto(SqlDataReader dr, string columna)
    {
        int i = dr.GetOrdinal(columna);
        return dr.IsDBNull(i) ? string.Empty : dr.GetString(i);
    }

    private void Ejecutar(string procedimiento, Action<SqlCommand> parametros)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand(procedimiento, cn);
        cmd.CommandType = CommandType.StoredProcedure;
        parametros(cmd);

        cmd.ExecuteNonQuery();
    }
}
