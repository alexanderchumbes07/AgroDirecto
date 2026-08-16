namespace AgroDirecto.Web.Models;

public class ReporteVentaViewModel
{
    public int PedidoId { get; set; }
    public DateTime FechaPedido { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public string Agricultor { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
}

public class ReporteResumenViewModel
{
    public decimal TotalVendido { get; set; }
    public decimal UnidadesVendidas { get; set; }
    public int Pedidos { get; set; }
}

public class ProductoVendidoViewModel
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Agricultor { get; set; } = string.Empty;
    public decimal UnidadesVendidas { get; set; }
    public decimal TotalVendido { get; set; }
}

public class VentaAgricultorViewModel
{
    public int AgricultorId { get; set; }
    public string Agricultor { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
    public int Pedidos { get; set; }
    public decimal UnidadesVendidas { get; set; }
    public decimal TotalVendido { get; set; }
}
