using System.ComponentModel.DataAnnotations;

namespace AgroDirecto.Web.Models;

public class CarritoItemViewModel
{
    public int DetalleCarritoId { get; set; }
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Agricultor { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Stock { get; set; }
}

public class CarritoViewModel
{
    public int CarritoId { get; set; }
    public List<CarritoItemViewModel> Items { get; set; } = new();

    public decimal Total => Items.Sum(i => i.Subtotal);
    public bool Vacio => Items.Count == 0;

    [Required(ErrorMessage = "Indica dónde entregamos el pedido")]
    [MaxLength(200, ErrorMessage = "Máximo 200 caracteres")]
    [Display(Name = "Dirección de entrega")]
    public string DireccionEntrega { get; set; } = string.Empty;
}

/* Una compra del cliente. Por dentro se reparte en un pedido por
   agricultor, porque cada uno entrega lo suyo por su cuenta. */
public class CompraViewModel
{
    public int CompraId { get; set; }
    public DateTime FechaCompra { get; set; }
    public decimal Total { get; set; }
    public string DireccionEntrega { get; set; } = string.Empty;
    public int Proveedores { get; set; }
    public int Items { get; set; }

    // Estado del pedido menos avanzado, sin contar los cancelados.
    public string Estado { get; set; } = string.Empty;

    public List<PedidoViewModel> Pedidos { get; set; } = new();
}

public class PedidoViewModel
{
    public int PedidoId { get; set; }
    public int CompraId { get; set; }
    public int AgricultorId { get; set; }
    public DateTime FechaPedido { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string DireccionEntrega { get; set; } = string.Empty;
    public int Items { get; set; }

    public string Cliente { get; set; } = string.Empty;
    public string TelefonoCliente { get; set; } = string.Empty;

    public string Agricultor { get; set; } = string.Empty;
    public string DistritoAgricultor { get; set; } = string.Empty;
    public string TelefonoAgricultor { get; set; } = string.Empty;

    public List<PedidoDetalleViewModel> Detalle { get; set; } = new();

    // Qué botones tiene sentido mostrarle al agricultor.
    public bool PuedeConfirmar => Estado == "Pendiente";
    public bool PuedeEntregar  => Estado == "Confirmado";
    public bool PuedeCancelar  => Estado == "Pendiente" || Estado == "Confirmado";
}

public class PedidoDetalleViewModel
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Agricultor { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
