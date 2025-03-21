using System.ComponentModel.DataAnnotations;
using System;
using IronMongeryTools.Services;
namespace IronMongeryTools.Models
{
    public class OlvidoContraseñaViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string Correo { get; set; }
    }
}