using System;
using System.ComponentModel.DataAnnotations;
using IronMongeryTools.Services;

namespace IronMongeryTools.Models
{
public class Categoria
{
    public int CategoriaID { get; set; }
    public string Nombre { get; set; }
    public string ProveedorID { get; set; }
}
}