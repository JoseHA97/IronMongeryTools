using System;
using System.ComponentModel.DataAnnotations;

namespace IronMongeryTools.Models
{
    public class Ventas
    {
        [Key]

        [Required(ErrorMessage = "El ID es obligatorio.")]
        [Display(Name = "Ventas ID")]
        public string VentasID { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(60, ErrorMessage = "El nombre no puede tener más de 60 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Solo se permiten letras y espacios.")]
        [Display(Name = "Usuario ID")]
        public string UsuarioID { get; set; }

        public DateTime Fecha { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = false)]
        public double Total { get; set; }
        
    }
}