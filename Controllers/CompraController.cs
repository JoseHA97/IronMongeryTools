using IronMongeryTools.Data;
using IronMongeryTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using IronMongeryTools.Services;
using System.Text.Json;
using System.Text;

namespace IronMongeryTools.Controllers
{
    
    public class CompraController : Controller
    {
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;

        public CompraController(IMemoryCache cache, IAccesoService accesoService, IEmailService emailService)
        {
            MemoriaCache = cache;
            _accesoService = accesoService;
            this.emailService = emailService;
        }

        public IActionResult Index()
        {
            // Obtener la lista de productos desde la base de datos
            List<Productos> productos;
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                var query = "SELECT ProductoID, Nombre, Precio, Stock FROM Productos";
                productos = new List<Productos>();
                using (SqlCommand cmd = new SqlCommand(query, oConexion))
                {
                    oConexion.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            productos.Add(new Productos
                            {
                                ProductoID = reader["ProductoID"].ToString(),
                                Nombre = reader["Nombre"].ToString(),
                                Precio = Convert.ToDouble(reader["Precio"]),
                                Stock = Convert.ToInt32(reader["Stock"])
                            });
                        }
                    }
                }
            }

            // Pasar la lista de productos a la vista
            return View(productos);
        }

        private async Task<List<ProductoCanasta>> DeserializarCanastaAsync(HttpRequest request)
        {
            // Habilitar la lectura múltiple del cuerpo de la solicitud
            request.EnableBuffering();

            // Si el stream no admite la operación Position, copiarlo a un MemoryStream
            if (!request.Body.CanSeek)
            {
                var memoryStream = new MemoryStream();
                await request.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                request.Body = memoryStream;
            }

            using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true);
            string requestBody = await reader.ReadToEndAsync();

            // Reiniciar la posición del stream para que otros métodos puedan leerlo
            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                TempData["ErrorMessage"] = "El cuerpo de la solicitud está vacío.";
                return new List<ProductoCanasta>(); // Retornar una lista vacía en lugar de null
            }

            try
            {
                var canasta = JsonSerializer.Deserialize<List<ProductoCanasta>>(requestBody, new JsonSerializerOptions());
                if (canasta == null || !canasta.Any())
                {
                    TempData["ErrorMessage"] = "La canasta está vacía.";
                    return new List<ProductoCanasta>(); // Retornar una lista vacía en lugar de null
                }

                Console.WriteLine("JSON deserializado correctamente");
                return canasta;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Error al deserializar JSON: " + ex.Message);
                TempData["ErrorMessage"] = "Error al procesar la solicitud. Intente de nuevo";
                return new List<ProductoCanasta>(); // Retornar una lista vacía en lugar de null
            }
        }

        private async Task<Usuarios> ObtenerUsuarioAsync(SqlConnection conexion, SqlTransaction transaction, string correoUsuario)
        {
            string queryUsuario = "SELECT UsuarioID FROM Usuarios WHERE Correo = @Correo";
            using (SqlCommand cmdUsuario = new SqlCommand(queryUsuario, conexion, transaction))
            {
                cmdUsuario.Parameters.AddWithValue("@Correo", correoUsuario);
                using (SqlDataReader readerUsuario = await cmdUsuario.ExecuteReaderAsync())
                {
                    if (await readerUsuario.ReadAsync())
                    {
                        return new Usuarios
                        {
                            UsuarioID = readerUsuario["UsuarioID"].ToString()
                        };
                    }
                }
            }
            return null;
        }

        private async Task<string> InsertarVentaAsync(SqlConnection conexion, SqlTransaction transaction, string usuarioID, List<ProductoCanasta> canasta)
        {
            string queryVenta = @"
                INSERT INTO Ventas (VentasID, UsuarioID, Fecha, Total)
                VALUES (@VentasID, @UsuarioID, @Fecha, @Total);";

            string ventaID = await GenerarNuevoVentasIDAsync(conexion, transaction);

            using (SqlCommand cmdVenta = new SqlCommand(queryVenta, conexion, transaction))
            {
                cmdVenta.Parameters.AddWithValue("@VentasID", ventaID);
                cmdVenta.Parameters.AddWithValue("@UsuarioID", usuarioID);
                cmdVenta.Parameters.AddWithValue("@Fecha", DateTime.Now);
                cmdVenta.Parameters.AddWithValue("@Total", canasta.Sum(item => item.Precio * item.Cantidad));

                await cmdVenta.ExecuteNonQueryAsync();
            }

            return ventaID;
        }

        private async Task ProcesarDetallesVentaAsync(SqlConnection conexion, SqlTransaction transaction, string ventaID, List<ProductoCanasta> canasta)
        {
            foreach (var item in canasta)
            {
                // Verificar stock disponible
                int stockActual = await VerificarStockAsync(conexion, transaction, item.ProductoID);

                if (stockActual < item.Cantidad)
                {
                    throw new Exception($"No hay suficiente stock para el producto: {item.Nombre}");
                }

                // Insertar el detalle de la venta
                await InsertarDetalleVentaAsync(conexion, transaction, ventaID, item);

                // Actualizar stock del producto
                await ActualizarStockAsync(conexion, transaction, item.ProductoID, item.Cantidad);

                // Registrar movimiento de inventario
                await RegistrarMovimientoInventarioAsync(conexion, transaction, item.ProductoID, item.Cantidad);
            }
        }

        private async Task<int> VerificarStockAsync(SqlConnection conexion, SqlTransaction transaction, string productoID)
        {
            string queryStock = "SELECT Stock FROM Productos WHERE ProductoID = @ProductoID";
            using (SqlCommand cmdStock = new SqlCommand(queryStock, conexion, transaction))
            {
                cmdStock.Parameters.AddWithValue("@ProductoID", productoID);
                return Convert.ToInt32(await cmdStock.ExecuteScalarAsync());
            }
        }

        private async Task InsertarDetalleVentaAsync(SqlConnection conexion, SqlTransaction transaction, string ventaID, ProductoCanasta item)
        {
            string queryDetalle = @"
                INSERT INTO DetallesVenta (DetallesVentaID, VentaID, ProductoID, Cantidad, Subtotal, Fecha)
                VALUES (@DetallesVentaID, @VentaID, @ProductoID, @Cantidad, @Subtotal, @Fecha);";

            string detallesVentaID = await GenerarNuevoDetallesVentasIDAsync(conexion, transaction);

            using (SqlCommand cmdDetalle = new SqlCommand(queryDetalle, conexion, transaction))
            {
                cmdDetalle.Parameters.AddWithValue("@DetallesVentaID", detallesVentaID);
                cmdDetalle.Parameters.AddWithValue("@VentaID", ventaID);
                cmdDetalle.Parameters.AddWithValue("@ProductoID", item.ProductoID);
                cmdDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                cmdDetalle.Parameters.AddWithValue("@Subtotal", item.Precio * item.Cantidad);
                cmdDetalle.Parameters.AddWithValue("@Fecha", DateTime.Now);

                await cmdDetalle.ExecuteNonQueryAsync();
            }
        }

        private async Task ActualizarStockAsync(SqlConnection conexion, SqlTransaction transaction, string productoID, int cantidad)
        {
            string queryUpdateStock = "UPDATE Productos SET Stock = Stock - @Cantidad WHERE ProductoID = @ProductoID";
            using (SqlCommand cmdUpdateStock = new SqlCommand(queryUpdateStock, conexion, transaction))
            {
                cmdUpdateStock.Parameters.AddWithValue("@Cantidad", cantidad);
                cmdUpdateStock.Parameters.AddWithValue("@ProductoID", productoID);

                await cmdUpdateStock.ExecuteNonQueryAsync();
            }
        }

        private async Task RegistrarMovimientoInventarioAsync(SqlConnection conexion, SqlTransaction transaction, string productoID, int cantidad)
        {
            string queryMovimiento = @"
                INSERT INTO MovimientosInventario (MovimientosInventarioID, ProductoID, Cantidad, Fecha, TipoMovimiento)
                VALUES (@MovimientosInventarioID, @ProductoID, @Cantidad, @Fecha, @TipoMovimiento);";

            string movimientosInventarioID = await GenerarNuevoMovimientosInventarioIDAsync(conexion, transaction);

            using (SqlCommand cmdMovimiento = new SqlCommand(queryMovimiento, conexion, transaction))
            {
                cmdMovimiento.Parameters.AddWithValue("@MovimientosInventarioID", movimientosInventarioID);
                cmdMovimiento.Parameters.AddWithValue("@ProductoID", productoID);
                cmdMovimiento.Parameters.AddWithValue("@Cantidad", -cantidad);
                cmdMovimiento.Parameters.AddWithValue("@Fecha", DateTime.Now);
                cmdMovimiento.Parameters.AddWithValue("@TipoMovimiento", "Venta");

                await cmdMovimiento.ExecuteNonQueryAsync();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Compra(List<ProductoCanasta> canasta)
        {
            try
            {
                var canastaDeserializada = await DeserializarCanastaAsync(Request);
                Console.WriteLine("Deserialización completada.");

                if (canastaDeserializada == null)
                {
                    TempData["ErrorMessage"] = "La canasta está vacía.";
                    return RedirectToAction("Index", "Compra");
                }

                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    await oConexion.OpenAsync();
                    SqlTransaction transaction = oConexion.BeginTransaction();

                    try
                    {
                        string correoUsuario = User.Identity.Name;
                        var usuario = await ObtenerUsuarioAsync(oConexion, transaction, correoUsuario);

                        if (usuario == null)
                        {
                            transaction.Rollback();
                            TempData["ErrorMessage"] = "Usuario no encontrado.";
                            return RedirectToAction("Index", "Compra");
                        }

                        string ventaID = await InsertarVentaAsync(oConexion, transaction, usuario.UsuarioID, canastaDeserializada);
                        await ProcesarDetallesVentaAsync(oConexion, transaction, ventaID, canastaDeserializada);

                        transaction.Commit();
                        TempData["SuccessMessage"] = "Compra realizada con éxito[controlador].";
                        return RedirectToAction("Index", "Compra"); // Redirigir correctamente
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "Error al procesar la compra: " + ex.Message;
                        return RedirectToAction("Index", "Compra");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al deserializar: " + ex.Message);
                TempData["ErrorMessage"] = "Error al procesar la solicitud.";
                return RedirectToAction("Index");
            }
        }
        private async Task ActualizarStockAsync(SqlConnection oConexion, SqlTransaction transaction, int productoID, int cantidad)
        {
            string queryUpdateStock = "UPDATE Productos SET Stock = Stock - @Cantidad WHERE ProductoID = @ProductoID";
            using (SqlCommand cmdUpdateStock = new SqlCommand(queryUpdateStock, oConexion, transaction))
            {
                cmdUpdateStock.Parameters.AddWithValue("@Cantidad", cantidad);
                cmdUpdateStock.Parameters.AddWithValue("@ProductoID", productoID);

                await cmdUpdateStock.ExecuteNonQueryAsync();
            }
        }

        private async Task<string> GenerarNuevoVentasIDAsync(SqlConnection oConexion, SqlTransaction transaction)
        {
            string query = "SELECT ISNULL(MAX(CAST(VentasID AS INT)), 0) + 1 FROM Ventas;";

            using (SqlCommand cmd = new SqlCommand(query, oConexion, transaction))
            {
                // Ejecutar la consulta y obtener el nuevo VentasID
                int ventaID = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return ventaID.ToString(); // Convertir a cadena de texto
            }
        }

        private async Task<string> GenerarNuevoDetallesVentasIDAsync(SqlConnection oConexion, SqlTransaction transaction)
        {
            string query = "SELECT ISNULL(MAX(CAST(DetallesVentaID AS INT)), 0) + 1 FROM DetallesVenta;";

            using (SqlCommand cmd = new SqlCommand(query, oConexion, transaction))
            {
                // Ejecutar la consulta y obtener el nuevo DetallesVentaID
                int detallesVentaID = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return detallesVentaID.ToString(); // Convertir a cadena de texto
            }
        }

        private async Task<string> GenerarNuevoMovimientosInventarioIDAsync(SqlConnection oConexion, SqlTransaction transaction)
        {
            string query = "SELECT ISNULL(MAX(CAST(MovimientosInventarioID AS INT)), 0) + 1 FROM MovimientosInventario;";

            using (SqlCommand cmd = new SqlCommand(query, oConexion, transaction))
            {
                // Ejecutar la consulta y obtener el nuevo MovimientosInventarioID
                int movimientosInventarioID = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return movimientosInventarioID.ToString(); // Convertir a cadena de texto
            }
        }

    }
}