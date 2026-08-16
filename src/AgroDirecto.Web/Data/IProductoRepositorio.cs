using AgroDirecto.Web.Models;

namespace AgroDirecto.Web.Data;

public interface IProductoRepositorio
{
    // Devuelve el AgricultorId del usuario que inició sesión, o null si no tiene perfil.
    int? ObtenerAgricultorIdPorUsuario(int usuarioId);

    List<ProductoViewModel> ListarPorAgricultor(int agricultorId, string? buscar,
                                                int pagina, int tamano, out int total);

    ProductoViewModel? ObtenerPorId(int productoId);

    void Insertar(ProductoViewModel p);
    void Actualizar(ProductoViewModel p);
    void Eliminar(int productoId, int agricultorId);

    // Catálogo público, con filtros. Lo consume el Web API por AJAX.
    List<ProductoViewModel> Catalogo(string? buscar, int? categoriaId, int? distritoId,
                                     decimal? precioMax, int pagina, int tamano, out int total);
}
