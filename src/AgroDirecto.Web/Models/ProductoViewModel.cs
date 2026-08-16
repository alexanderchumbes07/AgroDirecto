using System.ComponentModel.DataAnnotations;

namespace AgroDirecto.Web.Models;

public class ProductoViewModel
{
    public int ProductoId { get; set; }
    public int AgricultorId { get; set; }

    [Required(ErrorMessage = "Elige una categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Elige una categoría")]
    [Display(Name = "Categoría")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "Elige una unidad de medida")]
    [Range(1, int.MaxValue, ErrorMessage = "Elige una unidad de medida")]
    [Display(Name = "Unidad de medida")]
    public int UnidadMedidaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(120, ErrorMessage = "Máximo 120 caracteres")]
    [Display(Name = "Nombre del producto")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(400, ErrorMessage = "Máximo 400 caracteres")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(0.01, 99999, ErrorMessage = "El precio debe ser mayor a 0")]
    [Display(Name = "Precio (S/)")]
    public decimal Precio { get; set; }

    [Range(0, 99999, ErrorMessage = "El stock no puede ser negativo")]
    [Display(Name = "Stock disponible")]
    public decimal Stock { get; set; }

    [Range(0, 99999, ErrorMessage = "El monto mínimo no puede ser negativo")]
    [Display(Name = "Monto mínimo (S/)")]
    public decimal MontoMinimo { get; set; }

    [MaxLength(300, ErrorMessage = "Máximo 300 caracteres")]
    [Display(Name = "URL de la imagen")]
    public string? ImagenUrl { get; set; }

    [Display(Name = "Publicado")]
    public bool Estado { get; set; } = true;

    // Campos que solo se leen (vienen de los JOIN), no del formulario
    public string Categoria { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Agricultor { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
}
