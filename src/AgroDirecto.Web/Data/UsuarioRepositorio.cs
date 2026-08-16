using System.Data;
using Microsoft.Data.SqlClient;
using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly ConexionBD _bd;

    public UsuarioRepositorio(ConexionBD bd) => _bd = bd;

    public List<RolViewModel> ListarRoles()
    {
        var lista = new List<RolViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Rol_Listar", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            lista.Add(new RolViewModel
            {
                RolId  = dr.GetInt32(0),
                Nombre = dr.GetString(1)
            });

        return lista;
    }

    public UsuarioViewModel? ObtenerPorEmail(string email)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Usuario_ObtenerPorEmail", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Email", email);

        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return null;

        return new UsuarioViewModel
        {
            UsuarioId    = dr.GetInt32(0),
            RolId        = dr.GetInt32(1),
            Rol          = dr.GetString(2),
            Nombres      = dr.GetString(3),
            Apellidos    = dr.GetString(4),
            Email        = dr.GetString(5),
            PasswordHash = dr.GetString(6),
            Estado       = dr.GetBoolean(7)
        };
    }

    public int Registrar(RegistroViewModel r, string passwordHash)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Usuario_Registrar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@RolId", r.RolId);
        cmd.Parameters.AddWithValue("@Nombres", r.Nombres);
        cmd.Parameters.AddWithValue("@Apellidos", r.Apellidos);
        cmd.Parameters.AddWithValue("@Email", r.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        cmd.Parameters.AddWithValue("@Telefono", (object?)r.Telefono ?? DBNull.Value);

        var pId = new SqlParameter("@UsuarioId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pId);

        cmd.ExecuteNonQuery();

        return pId.Value == DBNull.Value ? 0 : (int)pId.Value;
    }

    // ---------- Mantenimiento del administrador ----------

    public List<UsuarioListaViewModel> Listar(string? buscar, int? rolId,
                                              int pagina, int tamano, out int total)
    {
        var lista = new List<UsuarioListaViewModel>();

        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Usuario_Listar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Buscar",
            string.IsNullOrWhiteSpace(buscar) ? DBNull.Value : buscar);
        cmd.Parameters.AddWithValue("@RolId", (object?)rolId ?? DBNull.Value);
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
            {
                int iAgr = dr.GetOrdinal("AgricultorId");
                int iApr = dr.GetOrdinal("Aprobado");
                int iCom = dr.GetOrdinal("NombreComercial");
                int iTel = dr.GetOrdinal("Telefono");

                lista.Add(new UsuarioListaViewModel
                {
                    UsuarioId       = dr.GetInt32(dr.GetOrdinal("UsuarioId")),
                    RolId           = dr.GetInt32(dr.GetOrdinal("RolId")),
                    Rol             = dr.GetString(dr.GetOrdinal("Rol")),
                    Nombres         = dr.GetString(dr.GetOrdinal("Nombres")),
                    Apellidos       = dr.GetString(dr.GetOrdinal("Apellidos")),
                    Email           = dr.GetString(dr.GetOrdinal("Email")),
                    Telefono        = dr.IsDBNull(iTel) ? null : dr.GetString(iTel),
                    Estado          = dr.GetBoolean(dr.GetOrdinal("Estado")),
                    FechaRegistro   = dr.GetDateTime(dr.GetOrdinal("FechaRegistro")),
                    AgricultorId    = dr.IsDBNull(iAgr) ? null : dr.GetInt32(iAgr),
                    Aprobado        = dr.IsDBNull(iApr) ? null : dr.GetBoolean(iApr),
                    NombreComercial = dr.IsDBNull(iCom) ? null : dr.GetString(iCom)
                });
            }
        }

        total = pTotal.Value == DBNull.Value ? 0 : (int)pTotal.Value;
        return lista;
    }

    public void CambiarEstado(int usuarioId, bool estado)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Usuario_CambiarEstado", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
        cmd.Parameters.AddWithValue("@Estado", estado);

        cmd.ExecuteNonQuery();
    }

    public void CambiarAprobacion(int agricultorId, bool aprobado)
    {
        using var cn = _bd.ObtenerConexion();
        cn.Open();

        using var cmd = new SqlCommand("usp_Agricultor_CambiarAprobacion", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@AgricultorId", agricultorId);
        cmd.Parameters.AddWithValue("@Aprobado", aprobado);

        cmd.ExecuteNonQuery();
    }
}
