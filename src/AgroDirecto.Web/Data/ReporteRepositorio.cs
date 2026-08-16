using System.Data;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public class ReporteRepositorio : IReporteRepositorio
{
    private readonly ConexionBD _bd;

    public ReporteRepositorio(ConexionBD bd) => _bd = bd;

    public List<ReporteVentaViewModel> Ventas(DateTime? desde, DateTime? hasta, int? agricultorId,
                                              int pagina, int tamano, out int total)
    {
        var lista = new List<ReporteVentaViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Reporte_Ventas", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        AgregarFiltros(cmd, desde, hasta, agricultorId);
        cmd.Parameters.AddWithValue("@Pagina", pagina);
        cmd.Parameters.AddWithValue("@Tamano", tamano);

        var pTotal = new SqlParameter("@Total", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pTotal);

        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                lista.Add(new ReporteVentaViewModel
                {
                    PedidoId       = dr.GetInt32(dr.GetOrdinal("PedidoId")),
                    FechaPedido    = dr.GetDateTime(dr.GetOrdinal("FechaPedido")),
                    Estado         = dr.GetString(dr.GetOrdinal("Estado")),
                    Producto       = dr.GetString(dr.GetOrdinal("Producto")),
                    Categoria      = dr.GetString(dr.GetOrdinal("Categoria")),
                    Cantidad       = dr.GetDecimal(dr.GetOrdinal("Cantidad")),
                    Unidad         = dr.GetString(dr.GetOrdinal("Unidad")),
                    PrecioUnitario = dr.GetDecimal(dr.GetOrdinal("PrecioUnitario")),
                    Subtotal       = dr.GetDecimal(dr.GetOrdinal("Subtotal")),
                    Agricultor     = dr.GetString(dr.GetOrdinal("Agricultor")),
                    Cliente        = dr.GetString(dr.GetOrdinal("Cliente"))
                });
        }

        total = pTotal.Value == DBNull.Value ? 0 : (int)pTotal.Value;
        return lista;
    }

    public ReporteResumenViewModel Resumen(DateTime? desde, DateTime? hasta, int? agricultorId)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Reporte_Resumen", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        AgregarFiltros(cmd, desde, hasta, agricultorId);

        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return new ReporteResumenViewModel();

        return new ReporteResumenViewModel
        {
            TotalVendido     = dr.GetDecimal(0),
            UnidadesVendidas = dr.GetDecimal(1),
            Pedidos          = dr.GetInt32(2)
        };
    }

    public List<ProductoVendidoViewModel> ProductosMasVendidos(DateTime? desde, DateTime? hasta,
                                                               int? agricultorId, int top = 10)
    {
        var lista = new List<ProductoVendidoViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Reporte_ProductosMasVendidos", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        AgregarFiltros(cmd, desde, hasta, agricultorId);
        cmd.Parameters.AddWithValue("@Top", top);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(new ProductoVendidoViewModel
            {
                ProductoId       = dr.GetInt32(dr.GetOrdinal("ProductoId")),
                Producto         = dr.GetString(dr.GetOrdinal("Producto")),
                Categoria        = dr.GetString(dr.GetOrdinal("Categoria")),
                Agricultor       = dr.GetString(dr.GetOrdinal("Agricultor")),
                UnidadesVendidas = dr.GetDecimal(dr.GetOrdinal("UnidadesVendidas")),
                TotalVendido     = dr.GetDecimal(dr.GetOrdinal("TotalVendido"))
            });

        return lista;
    }

    public List<VentaAgricultorViewModel> VentasPorAgricultor(DateTime? desde, DateTime? hasta)
    {
        var lista = new List<VentaAgricultorViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Reporte_VentasPorAgricultor", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@FechaInicio", (object?)desde ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FechaFin", (object?)hasta ?? DBNull.Value);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(new VentaAgricultorViewModel
            {
                AgricultorId     = dr.GetInt32(dr.GetOrdinal("AgricultorId")),
                Agricultor       = dr.GetString(dr.GetOrdinal("Agricultor")),
                Distrito         = dr.GetString(dr.GetOrdinal("Distrito")),
                Pedidos          = dr.GetInt32(dr.GetOrdinal("Pedidos")),
                UnidadesVendidas = dr.GetDecimal(dr.GetOrdinal("UnidadesVendidas")),
                TotalVendido     = dr.GetDecimal(dr.GetOrdinal("TotalVendido"))
            });

        return lista;
    }

    public List<(int AgricultorId, string Nombre)> ListarAgricultores()
    {
        var lista = new List<(int, string)>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Agricultor_ListarTodos", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add((dr.GetInt32(0), dr.GetString(1)));

        return lista;
    }

    private static void AgregarFiltros(SqlCommand cmd, DateTime? desde, DateTime? hasta, int? agricultorId)
    {
        cmd.Parameters.AddWithValue("@FechaInicio", (object?)desde ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FechaFin", (object?)hasta ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AgricultorId", (object?)agricultorId ?? DBNull.Value);
    }
}
