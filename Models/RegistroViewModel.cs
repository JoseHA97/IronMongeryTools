using System;
using System.ComponentModel.DataAnnotations;
using IronMongeryTools.Services;
namespace IronMongeryTools.Models
{
    public class RegistroViewModel
    {
 
        public string AdministradorID { get; set; } 

 
        public string Cedula { get; set; }
        public string UsuarioID { get; set; }
        public string ProveedorID { get; set; }
        public string TipoRegistro { get; set; } // Usuario, Administrador, Proveedor
        public string Nombre { get; set; }
        public string FechaNacimiento { get; set; }
        public string Correo { get; set; }
        public string PasswordHash { get; set; }

        public string EmailConfirmationToken { get; set; }
        public bool EmailConfirmed { get; set; }
    
        public string Telefono { get; set; } // Solo para Proveedor
        public string Direccion { get; set; } // Solo para Proveedor
    }
}