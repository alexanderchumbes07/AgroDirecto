using System.ComponentModel.DataAnnotations;

namespace AgroDirecto.Web.Models;

// Datos del usuario tal como salen de la base (uso interno, no de formulario).
public class UsuarioViewModel
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Estado { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
}

public class RegistroViewModel
{
    [Required(ErrorMessage = "Ingresa tus nombres")]
    [MaxLength(80)]
    [Display(Name = "Nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tus apellidos")]
    [MaxLength(80)]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu correo")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    [MaxLength(120)]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Ingresa una contraseña")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repite la contraseña")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir contraseña")]
    public string Confirmar { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige con qué perfil te registras")]
    [Display(Name = "Me registro como")]
    public int RolId { get; set; }
}

public class RolViewModel
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// Fila del mantenimiento de usuarios del administrador.
// Los campos de agricultor vienen vacíos si el usuario no lo es.
public class UsuarioListaViewModel
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaRegistro { get; set; }

    public int? AgricultorId { get; set; }
    public bool? Aprobado { get; set; }
    public string? NombreComercial { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";
    public bool EsAgricultor => AgricultorId.HasValue;
}
