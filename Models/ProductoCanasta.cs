using System;
using System.ComponentModel.DataAnnotations;
using IronMongeryTools.Services;
using System.Text.Json.Serialization;

namespace IronMongeryTools.Models
{
    public class ProductoCanasta
    {
        [JsonPropertyName("productoID")]
        public string ProductoID { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("precio")]
        public decimal Precio { get; set; }

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }

        [JsonPropertyName("detallesventaid")]
        public string DetallesVentaID { get; set; }

        [JsonPropertyName("fecha")]
        public string Fecha { get; set; }
        [JsonPropertyName("stock")]
        public int Stock { get; set; }
    }
}