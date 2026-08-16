using System.Data;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public class UnidadMedidaRepositorio : IUnidadMedidaRepositorio
{
    private readonly ConexionBD _bd;

    public UnidadMedidaRepositorio(ConexionBD bd) => _bd = bd;

    public List<UnidadMedidaViewModel> Listar(string? buscar, int pagina, int tamano, out int total)
    {
        var lista = new List<UnidadMedidaViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_UnidadMedida_Listar", cn);
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
        // El reader debe cerrarse antes de leer el parámetro OUTPUT.

        total = pTotal.Value == DBNull.Value ? 0 : (int)pTotal.Value;
        return lista;
    }

    public List<UnidadMedidaViewModel> ListarTodas()
    {
        var lista = new List<UnidadMedidaViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_UnidadMedida_ListarTodas", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(Mapear(dr));

        return lista;
    }

    public UnidadMedidaViewModel? ObtenerPorId(int id)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_UnidadMedida_ObtenerPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UnidadMedidaId", id);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? Mapear(dr) : null;
    }

    public void Insertar(UnidadMedidaViewModel u)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_UnidadMedida_Insertar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Nombre", u.Nombre);
        cmd.Parameters.AddWithValue("@Abreviatura", u.Abreviatura);

        var pId = new SqlParameter("@UnidadMedidaId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pId);

        cmd.ExecuteNonQuery();

        u.UnidadMedidaId = pId.Value == DBNull.Value ? 0 : (int)pId.Value;
    }

    public void Actualizar(UnidadMedidaViewModel u)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_UnidadMedida_Actualizar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UnidadMedidaId", u.UnidadMedidaId);
        cmd.Parameters.AddWithValue("@Nombre", u.Nombre);
        cmd.Parameters.AddWithValue("@Abreviatura", u.Abreviatura);

        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int id)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_UnidadMedida_Eliminar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UnidadMedidaId", id);

        cmd.ExecuteNonQuery();
    }

    private static UnidadMedidaViewModel Mapear(SqlDataReader dr) => new()
    {
        UnidadMedidaId = dr.GetInt32(0),
        Nombre         = dr.GetString(1),
        Abreviatura    = dr.GetString(2)
    };
}
