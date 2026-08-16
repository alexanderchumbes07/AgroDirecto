using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public interface IReporteRepositorio
{
    List<ReporteVentaViewModel> Ventas(DateTime? desde, DateTime? hasta, int? agricultorId,
                                       int pagina, int tamano, out int total);

    ReporteResumenViewModel Resumen(DateTime? desde, DateTime? hasta, int? agricultorId);

    List<ProductoVendidoViewModel> ProductosMasVendidos(DateTime? desde, DateTime? hasta,
                                                        int? agricultorId, int top = 10);

    List<VentaAgricultorViewModel> VentasPorAgricultor(DateTime? desde, DateTime? hasta);

    List<(int AgricultorId, string Nombre)> ListarAgricultores();
}
