using System.ComponentModel.DataAnnotations;

namespace AgroDirecto.Web.Models;

public class UnidadMedidaViewModel
{
    public int UnidadMedidaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(40, ErrorMessage = "Máximo 40 caracteres")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La abreviatura es obligatoria")]
    [MaxLength(10, ErrorMessage = "Máximo 10 caracteres")]
    [Display(Name = "Abreviatura")]
    public string Abreviatura { get; set; } = string.Empty;
}
