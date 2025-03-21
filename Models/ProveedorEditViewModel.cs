using System;
using System.ComponentModel.DataAnnotations;
using IronMongeryTools.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IronMongeryTools.Models
{
    public class ProveedorEditViewModel
    {
        // Datos básicos del proveedor
        public string ProveedorID { get; set; }
        public string Nombre { get; set; }
        public string FechaNacimiento { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string Direccion { get; set; }

        // Lista de categorías nuevas/seleccionadas
        public List<string> NuevasCategorias { get; set; }

        public IEnumerable<SelectListItem> CategoriaList { get; set; }
    }
}