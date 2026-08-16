using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public interface IUnidadMedidaRepositorio
{
    List<UnidadMedidaViewModel> Listar(string? buscar, int pagina, int tamano, out int total);
    List<UnidadMedidaViewModel> ListarTodas();
    UnidadMedidaViewModel? ObtenerPorId(int id);
    void Insertar(UnidadMedidaViewModel u);
    void Actualizar(UnidadMedidaViewModel u);
    void Eliminar(int id);
}
