using System;
using System.ComponentModel.DataAnnotations;
using IronMongeryTools.Services;

namespace IronMongeryTools.Models
{
    public class Usuarios: IAcceso
    {
        [Key]

        [Required(ErrorMessage = "El ID es obligatorio.")]
        [RegularExpression(@"^\d-\d{4}-\d{4}$", ErrorMessage = "El formato de ID debe ser 0-0000-0000")]
        [Display(Name = "Usuario ID")]
        public string UsuarioID { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(60, ErrorMessage = "El nombre no puede tener más de 60 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Solo se permiten letras y espacios.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [RegularExpression(@"^(0[1-9]|[12][0-9]|3[01])/(0[1-9]|1[0-2])/\d{4}$",
        ErrorMessage = "Formato incorrecto. Use dd/MM/yyyy.")]
        [Display(Name = "Fecha de Nacimiento")]
        public string FechaNacimiento { get; set; }
        public string Correo { get; set; }
        [Required]
        public string Token { get; set; }
        [Required]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string PasswordHash { get; set; }
        [Required]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string NewPasswordHash { get; set; }
        public string PasswordResetToken { get; set; } // Token de restablecimiento de contraseña
        public string EmailConfirmationToken { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
