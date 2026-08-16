using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public interface IDistritoRepositorio
{
    List<DistritoViewModel> Listar(string? buscar, int pagina, int tamano, out int total);
    List<DistritoViewModel> ListarTodos();
    DistritoViewModel? ObtenerPorId(int id);
    void Insertar(DistritoViewModel d);
    void Actualizar(DistritoViewModel d);
    void Eliminar(int id);
}
