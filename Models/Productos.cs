using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IronMongeryTools.Models
{
    public class Productos
    {
        [Key]

        [Required(ErrorMessage = "El ID es obligatorio.")]
        [Display(Name = "Producto ID")]
        public string ProductoID { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(60, ErrorMessage = "El nombre no puede tener más de 60 caracteres.")]
        public string Nombre { get; set; }

        [Display(Name = "Categoria ID")]
        public string CategoriaID { get; set; }
        public double Precio { get; set; }
        public int Stock { get; set; }

        [Display(Name = "Proveedor ID")]
        public string ProveedorID { get; set; }

        [Display(Name = "Nombre de categoria ID")]
        public string CategoriaNombre { get; set; }

        public string SelectedCategoriaID { get; set; }

        public IEnumerable<SelectListItem> CategoriaList { get; set; }

        public string NuevaCategoria { get; set; }

        public List<Categoria> Categorias { get; set; } = new List<Categoria>();

        public int Cantidad { get; set; }

        public string DetallesVentaID { get; set; }

        public string Fecha { get; set; }

    }
}