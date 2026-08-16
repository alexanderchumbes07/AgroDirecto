using System.Data;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public class DistritoRepositorio : IDistritoRepositorio
{
    private readonly ConexionBD _bd;

    public DistritoRepositorio(ConexionBD bd) => _bd = bd;

    public List<DistritoViewModel> Listar(string? buscar, int pagina, int tamano, out int total)
    {
        var lista = new List<DistritoViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Distrito_Listar", cn);
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

    public List<DistritoViewModel> ListarTodos()
    {
        var lista = new List<DistritoViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Distrito_ListarTodos", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(Mapear(dr));

        return lista;
    }

    public DistritoViewModel? ObtenerPorId(int id)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Distrito_ObtenerPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@DistritoId", id);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? Mapear(dr) : null;
    }

    public void Insertar(DistritoViewModel d)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Distrito_Insertar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Nombre", d.Nombre);
        cmd.Parameters.AddWithValue("@Provincia", (object?)d.Provincia ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Departamento", (object?)d.Departamento ?? DBNull.Value);

        var pId = new SqlParameter("@DistritoId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pId);

        cmd.ExecuteNonQuery();

        d.DistritoId = pId.Value == DBNull.Value ? 0 : (int)pId.Value;
    }

    public void Actualizar(DistritoViewModel d)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Distrito_Actualizar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@DistritoId", d.DistritoId);
        cmd.Parameters.AddWithValue("@Nombre", d.Nombre);
        cmd.Parameters.AddWithValue("@Provincia", (object?)d.Provincia ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Departamento", (object?)d.Departamento ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int id)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Distrito_Eliminar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@DistritoId", id);

        cmd.ExecuteNonQuery();
    }

    private static DistritoViewModel Mapear(SqlDataReader dr) => new()
    {
        DistritoId   = dr.GetInt32(0),
        Nombre       = dr.GetString(1),
        Provincia    = dr.IsDBNull(2) ? null : dr.GetString(2),
        Departamento = dr.IsDBNull(3) ? null : dr.GetString(3)
    };
}
