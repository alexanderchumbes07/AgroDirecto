using System.ComponentModel.DataAnnotations;

namespace AgroDirecto.Web.Models;

public class DistritoViewModel
{
    public int DistritoId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(80, ErrorMessage = "Máximo 80 caracteres")]
    [Display(Name = "Distrito")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(80, ErrorMessage = "Máximo 80 caracteres")]
    [Display(Name = "Provincia")]
    public string? Provincia { get; set; }

    [MaxLength(80, ErrorMessage = "Máximo 80 caracteres")]
    [Display(Name = "Departamento")]
    public string? Departamento { get; set; }
}
