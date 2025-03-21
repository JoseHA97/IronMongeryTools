using System;
using System.ComponentModel.DataAnnotations;
using IronMongeryTools.Services;
namespace IronMongeryTools.Models
{
    public class RestablecerContraseñaViewModel
    {
        public string Email { get; set; }  // Identificador del usuario
        public string PasswordResetToken { get; set; }
        public string NewPasswordHash { get; set; }
        public string ConfirmNewPasswordHash { get; set; }
        public string TipoUsuario { get; set; } // Puede ser "Usuario", "Administrador" o "Proveedor"
    }
}