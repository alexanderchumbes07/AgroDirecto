using System.ComponentModel.DataAnnotations;

namespace AgroDirecto.Web.Models;

public class CategoriaViewModel
{
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(60, ErrorMessage = "Máximo 60 caracteres")]
    [Display(Name = "Nombre de la categoría")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Máximo 200 caracteres")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Activa")]
    public bool Estado { get; set; } = true;
}
