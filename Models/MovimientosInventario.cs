using System;
using System.ComponentModel.DataAnnotations;

namespace IronMongeryTools.Models
{
    public class MovimientosInventario
    {
        [Key]

        [Required(ErrorMessage = "El ID es obligatorio.")]
        [Display(Name = "Movimiento de Inventario ID")]
        public string MovimientosInventarioID { get; set; }

        [Display(Name = "Producto ID")] 
        public string ProductoID {get; set; }

        [Display(Name = "Tipo de movimiento")] 
        public string TipoMovimiento { get; set; }
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; }
    }
}
