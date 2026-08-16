using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

// El controlador depende de esta interfaz, no de la clase concreta.
public interface ICategoriaRepositorio
{
    // 'total' devuelve cuántas filas cumplen el filtro, para calcular las páginas.
    List<CategoriaViewModel> Listar(string? buscar, int pagina, int tamano, out int total);

    // Solo las activas, para los desplegables de publicación y filtros.
    List<CategoriaViewModel> ListarActivas();

    CategoriaViewModel? ObtenerPorId(int id);
    void Insertar(CategoriaViewModel c);
    void Actualizar(CategoriaViewModel c);
    void Eliminar(int id);
}
