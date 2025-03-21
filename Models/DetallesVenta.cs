using System;
using System.ComponentModel.DataAnnotations;

namespace IronMongeryTools.Models
{
    public class DetallesVenta
    {
        [Key]

        [Required(ErrorMessage = "El ID es obligatorio.")]
        [Display(Name = "Detalle de venta ID")]
        public string DetallesVentaID { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(60, ErrorMessage = "El nombre no puede tener más de 60 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Solo se permiten letras y espacios.")]
        [Display(Name = "Venta ID")]
        public string VentaID { get; set; }
        
        [Display(Name = "Producto ID")]
        public string ProductoID { get; set; }
        public int Cantidad { get; set; }
        public double Subtotal { get; set; }
        public DateTime Fecha { get; set; }
    }
}
