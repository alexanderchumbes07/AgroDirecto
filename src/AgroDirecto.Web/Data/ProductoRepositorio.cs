using System.Data;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public class ProductoRepositorio : IProductoRepositorio
{
    private readonly ConexionBD _bd;

    public ProductoRepositorio(ConexionBD bd) => _bd = bd;

    public int? ObtenerAgricultorIdPorUsuario(int usuarioId)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Agricultor_ObtenerPorUsuario", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? dr.GetInt32(0) : null;
    }

    // ---------- FASE 4: productos del agricultor ----------

    public List<ProductoViewModel> ListarPorAgricultor(int agricultorId, string? buscar,
                                                       int pagina, int tamano, out int total)
    {
        var lista = new List<ProductoViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Producto_ListarPorAgricultor", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@AgricultorId", agricultorId);
        cmd.Parameters.AddWithValue("@Buscar",
            string.IsNullOrWhiteSpace(buscar) ? DBNull.Value : buscar);
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
                lista.Add(MapearPropio(dr));
        }
        // El reader debe cerrarse antes de leer el parámetro OUTPUT.

        total = pTotal.Value == DBNull.Value ? 0 : (int)pTotal.Value;
        return lista;
    }

    public ProductoViewModel? ObtenerPorId(int productoId)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Producto_ObtenerPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ProductoId", productoId);

        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return null;

        var p = MapearPropio(dr);
        p.Agricultor = dr.GetString(dr.GetOrdinal("Agricultor"));
        p.Distrito   = dr.GetString(dr.GetOrdinal("Distrito"));
        return p;
    }

    public void Insertar(ProductoViewModel p)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Producto_Insertar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        AgregarParametrosComunes(cmd, p);

        var pId = new SqlParameter("@ProductoId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pId);

        cmd.ExecuteNonQuery();

        p.ProductoId = pId.Value == DBNull.Value ? 0 : (int)pId.Value;
    }

    public void Actualizar(ProductoViewModel p)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Producto_Actualizar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ProductoId", p.ProductoId);
        AgregarParametrosComunes(cmd, p);

        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int productoId, int agricultorId)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Producto_Eliminar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ProductoId", productoId);
        cmd.Parameters.AddWithValue("@AgricultorId", agricultorId);

        cmd.ExecuteNonQuery();
    }

    // ---------- FASE 5: catálogo público ----------

    public List<ProductoViewModel> Catalogo(string? buscar, int? categoriaId, int? distritoId,
                                            decimal? precioMax, int pagina, int tamano, out int total)
    {
        var lista = new List<ProductoViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Producto_Catalogo", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Buscar",
            string.IsNullOrWhiteSpace(buscar) ? DBNull.Value : buscar);
        cmd.Parameters.AddWithValue("@CategoriaId", (object?)categoriaId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DistritoId", (object?)distritoId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PrecioMax", (object?)precioMax ?? DBNull.Value);
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
                lista.Add(new ProductoViewModel
                {
                    ProductoId  = dr.GetInt32(dr.GetOrdinal("ProductoId")),
                    Nombre      = dr.GetString(dr.GetOrdinal("Nombre")),
                    Descripcion = Texto(dr, "Descripcion"),
                    Precio      = dr.GetDecimal(dr.GetOrdinal("Precio")),
                    Stock       = dr.GetDecimal(dr.GetOrdinal("Stock")),
                    MontoMinimo = dr.GetDecimal(dr.GetOrdinal("MontoMinimo")),
                    ImagenUrl   = Texto(dr, "ImagenUrl"),
                    CategoriaId = dr.GetInt32(dr.GetOrdinal("CategoriaId")),
                    Categoria   = dr.GetString(dr.GetOrdinal("Categoria")),
                    Unidad      = dr.GetString(dr.GetOrdinal("Unidad")),
                    Agricultor  = dr.GetString(dr.GetOrdinal("Agricultor")),
                    Distrito    = dr.GetString(dr.GetOrdinal("Distrito"))
                });
        }

        total = pTotal.Value == DBNull.Value ? 0 : (int)pTotal.Value;
        return lista;
    }

    // ---------- Apoyo ----------

    private static void AgregarParametrosComunes(SqlCommand cmd, ProductoViewModel p)
    {
        cmd.Parameters.AddWithValue("@AgricultorId", p.AgricultorId);
        cmd.Parameters.AddWithValue("@CategoriaId", p.CategoriaId);
        cmd.Parameters.AddWithValue("@UnidadMedidaId", p.UnidadMedidaId);
        cmd.Parameters.AddWithValue("@Nombre", p.Nombre);
        cmd.Parameters.AddWithValue("@Descripcion", (object?)p.Descripcion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Precio", p.Precio);
        cmd.Parameters.AddWithValue("@Stock", p.Stock);
        cmd.Parameters.AddWithValue("@MontoMinimo", p.MontoMinimo);
        cmd.Parameters.AddWithValue("@ImagenUrl", (object?)p.ImagenUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Estado", p.Estado);
    }

    private static ProductoViewModel MapearPropio(SqlDataReader dr) => new()
    {
        ProductoId     = dr.GetInt32(dr.GetOrdinal("ProductoId")),
        AgricultorId   = dr.GetInt32(dr.GetOrdinal("AgricultorId")),
        CategoriaId    = dr.GetInt32(dr.GetOrdinal("CategoriaId")),
        UnidadMedidaId = dr.GetInt32(dr.GetOrdinal("UnidadMedidaId")),
        Nombre         = dr.GetString(dr.GetOrdinal("Nombre")),
        Descripcion    = Texto(dr, "Descripcion"),
        Precio         = dr.GetDecimal(dr.GetOrdinal("Precio")),
        Stock          = dr.GetDecimal(dr.GetOrdinal("Stock")),
        MontoMinimo = dr.GetDecimal(dr.GetOrdinal("MontoMinimo")),
        ImagenUrl      = Texto(dr, "ImagenUrl"),
        Estado         = dr.GetBoolean(dr.GetOrdinal("Estado")),
        Categoria      = dr.GetString(dr.GetOrdinal("Categoria")),
        Unidad         = dr.GetString(dr.GetOrdinal("Unidad"))
    };

    // Lee una columna de texto que admite NULL
    private static string? Texto(SqlDataReader dr, string columna)
    {
        int i = dr.GetOrdinal(columna);
        return dr.IsDBNull(i) ? null : dr.GetString(i);
    }
}
