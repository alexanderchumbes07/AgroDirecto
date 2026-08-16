using System.Data;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

// Acceso a datos de Categoría con ADO.NET y procedimientos almacenados.
public class CategoriaRepositorio : ICategoriaRepositorio
{
    private readonly ConexionBD _bd;

    public CategoriaRepositorio(ConexionBD bd) => _bd = bd;

    // ---------- LEER ----------

    public List<CategoriaViewModel> Listar(string? buscar, int pagina, int tamano, out int total)
    {
        var lista = new List<CategoriaViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Categoria_Listar", cn);
        cmd.CommandType = CommandType.StoredProcedure;

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
                lista.Add(Mapear(dr));
        }
        // El 'using' de arriba lleva llaves a propósito: el valor de un
        // parámetro OUTPUT solo está disponible con el DataReader ya
        // cerrado. Si se lee antes, @Total llega vacío.

        total = pTotal.Value == DBNull.Value ? 0 : (int)pTotal.Value;
        return lista;
    }

    public List<CategoriaViewModel> ListarActivas()
    {
        var lista = new List<CategoriaViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Categoria_ListarActivas", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(Mapear(dr));

        return lista;
    }

    public CategoriaViewModel? ObtenerPorId(int id)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Categoria_ObtenerPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@CategoriaId", id);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? Mapear(dr) : null;
    }

    // ---------- ESCRIBIR ----------

    public void Insertar(CategoriaViewModel c)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Categoria_Insertar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Nombre", c.Nombre);
        cmd.Parameters.AddWithValue("@Descripcion", (object?)c.Descripcion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Estado", c.Estado);

        var pId = new SqlParameter("@CategoriaId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pId);

        cmd.ExecuteNonQuery();

        c.CategoriaId = pId.Value == DBNull.Value ? 0 : (int)pId.Value;
    }

    public void Actualizar(CategoriaViewModel c)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Categoria_Actualizar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@CategoriaId", c.CategoriaId);
        cmd.Parameters.AddWithValue("@Nombre", c.Nombre);
        cmd.Parameters.AddWithValue("@Descripcion", (object?)c.Descripcion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Estado", c.Estado);

        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int id)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Categoria_Eliminar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@CategoriaId", id);

        cmd.ExecuteNonQuery();
    }

    private static CategoriaViewModel Mapear(SqlDataReader dr) => new()
    {
        CategoriaId = dr.GetInt32(0),
        Nombre      = dr.GetString(1),
        Descripcion = dr.IsDBNull(2) ? null : dr.GetString(2),
        Estado      = dr.GetBoolean(3)
    };
}
