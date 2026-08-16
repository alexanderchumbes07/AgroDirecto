using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public interface IUsuarioRepositorio
{
    List<RolViewModel> ListarRoles();

    UsuarioViewModel? ObtenerPorEmail(string email);

    // Crea el usuario y su perfil (Agricultor o Cliente) en una transacción.
    int Registrar(RegistroViewModel r, string passwordHash);

    // ----- Mantenimiento del administrador -----
    List<UsuarioListaViewModel> Listar(string? buscar, int? rolId, int pagina, int tamano, out int total);
    void CambiarEstado(int usuarioId, bool estado);
    void CambiarAprobacion(int agricultorId, bool aprobado);
}
