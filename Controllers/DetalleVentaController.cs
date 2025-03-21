using IronMongeryTools.Data;
using IronMongeryTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using System.Data;
using IronMongeryTools.Services;

namespace IronMongeryTools.Controllers
{
    public class DetalleVentaController : Controller
    {
        // GET: ProductoController
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;

        public DetalleVentaController(IMemoryCache cache, IAccesoService accesoService, IEmailService emailService)
        {
            MemoriaCache = cache;
            _accesoService = accesoService;
            this.emailService = emailService;
        }

        private List<DetallesVenta> ObtenerDetalleVenta()
        {
            List<DetallesVenta> EfectoDetalleVenta;

            EfectoDetalleVenta = CargarDetalleVentaDesdeBaseDeDatos();

            if (EstaVacioElCache())
            {
                
                
                // Almacenar la lista cargada en el cache
                MemoriaCache.Set("ListaDetalleVenta", EfectoDetalleVenta);
            }
            else
            {
                // Recuperar las ventas desde el cache
                EfectoDetalleVenta = (List<DetallesVenta>)MemoriaCache.Get("ListaDetalleVenta");
            }

            return EfectoDetalleVenta;
        }

        private List<DetallesVenta> CargarDetalleVentaDesdeBaseDeDatos()
        {
            var listaDetalleVenta= new List<DetallesVenta>();

            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_DetallesVenta_Obtener", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                try
                {
                    oConexion.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaDetalleVenta.Add(new DetallesVenta
                            {
                                DetallesVentaID = reader["DetallesVentaID"]?.ToString(),
                                VentaID = reader["VentaID"]?.ToString(),
                                ProductoID = reader["ProductoID"]?.ToString(),
                                Cantidad = Convert.ToInt32(reader["Cantidad"]),
                                Subtotal = Convert.ToDouble(reader["Subtotal"]),
                                Fecha = (DateTime)reader["Fecha"]
                                
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al cargar los detalles desde la base de datos", ex);
                }
            }

            return listaDetalleVenta;
        }

        private DetallesVenta ObtenerDetalleVenta(string id)
        {

            List<DetallesVenta> laLista;
            laLista = ObtenerDetalleVenta();

            foreach (DetallesVenta detalleventa in laLista)
            {
                if (detalleventa.DetallesVentaID == id)
                    return detalleventa;
            }

            return null;

        }
        private bool EstaVacioElCache()
        {
            if (MemoriaCache.Get("ListaDetalleVenta") is null)
                return true;
            else
                return false;
        }

        // GET: DetalleVentaController
        public ActionResult Index()
        {
            List<DetallesVenta> laLista;
            laLista = ObtenerDetalleVenta();
            int totaldetalleventas = laLista.Count;
            ViewData["TotalDetalleVentas"] = totaldetalleventas;
            return View(laLista);
        }

        // GET: DetalleVentaController/Details/
        public ActionResult Details(string id)
        {
            DetallesVenta eldetalleventa;
            eldetalleventa = ObtenerDetalleVenta(id);
            return View(eldetalleventa);
        }

        // GET: DetalleVentaController/Create
        public ActionResult Create()
        {
            return View();
        }

        public static bool ProbarConexion(out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();
                    mensaje = "Conexión exitosa a la base de datos.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Error al conectar a la base de datos: {ex.Message}";
                return false;
            }
        }
        private bool Registrar(DetallesVenta detallesVenta, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();

                    string query = @"
                INSERT INTO DetallesVenta (DetallesVentaID, VentaID, ProductoID, Cantidad, Subtotal)

                VALUES (@DetallesVentaID, @VentaID, @ProductoID, @Cantidad, @Subtotal)";

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@DetallesVentaID", detallesVenta.DetallesVentaID);
                        cmd.Parameters.AddWithValue("@VentaID", detallesVenta.VentaID);
                        cmd.Parameters.AddWithValue("@ProductoID", detallesVenta.ProductoID);
                        cmd.Parameters.AddWithValue("@Cantidad", detallesVenta.Cantidad);
                        cmd.Parameters.AddWithValue("@Subtotal", detallesVenta.Subtotal);
                        int filasAfectadas = cmd.ExecuteNonQuery();

                        return filasAfectadas > 0;
                    }

                }
            }
            catch (SqlException ex)
            {
                mensaje = $"Error al registrar el detalle de venta: {ex.Message}";
                return false;
            }
        }

        public IActionResult ConfirmarCorreo(string token, string email)
        {
            bool result = _accesoService.ConfirmarCorreo(token, email);

            if (result)
            {
                return RedirectToAction("ConfirmacionExitosa"); // Página de confirmación exitosa
            }
            else
            {
                return BadRequest("No se pudo confirmar el correo.");
            }
        }

        // POST: DetalleVentaController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(DetallesVenta detallesventa)
        {
            try
            {
                List<DetallesVenta> laLista;
                laLista = ObtenerDetalleVenta();
                laLista.Add(detallesventa);
                string mensaje;
                bool r = ProbarConexion(out mensaje);
                bool conexionValida = ProbarConexion(out mensaje);
                if (!conexionValida)
                {
                    ModelState.AddModelError("", mensaje);
                    return View(detallesventa);
                }
                bool registroExitoso = Registrar(detallesventa, out mensaje);

                if(!registroExitoso)
                {
                    ModelState.AddModelError(string.Empty, mensaje);
                    return View(detallesventa);
                }else{
                    return RedirectToAction(nameof(Index));
                }

            }
            catch
            {
                return View();
            }
        }

        // GET: DetalleVentaController/Edit/5
        public ActionResult Edit(string id)
        {
            DetallesVenta eldetalleventa;
            eldetalleventa = ObtenerDetalleVenta(id);

            return View(eldetalleventa);
        }

        // POST: DetalleVentaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, DetallesVenta DetallesVenta)
        {
            try
            {
                var eldetalleventa = ObtenerDetalleVenta(id); // Obtener el detalle de venta
                if (eldetalleventa == null)
                {
                    return NotFound(); // Retorna un error 404 si no se encuentra el detalle de venta
                }

                // Actualizar datos
                eldetalleventa.DetallesVentaID = DetallesVenta.DetallesVentaID;
                eldetalleventa.VentaID = DetallesVenta.VentaID;
                eldetalleventa.ProductoID = DetallesVenta.ProductoID;
                eldetalleventa.Cantidad = DetallesVenta.Cantidad;
                eldetalleventa.Subtotal = DetallesVenta.Subtotal;

                // Aquí debes guardar los cambios en la base de datos
                bool actualizado = ActualizarDetalleVentaEnBaseDeDatos(eldetalleventa);
                if (actualizado)
                {
                    return RedirectToAction(nameof(Index)); // Redirige a la lista de detalle de venta
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar el detalle de venta.");
                    return View(DetallesVenta); // Devuelve la vista si no se puede actualizar
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(DetallesVenta); // Devuelve la vista si hay un error
            }
        }

        private bool ActualizarDetalleVentaEnBaseDeDatos(DetallesVenta detallesventa)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_DetallesVenta_Modificar", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Aquí pasas todos los parámetros necesarios
                cmd.Parameters.AddWithValue("@detallesventaid", detallesventa.DetallesVentaID);
                cmd.Parameters.AddWithValue("@ventaid", detallesventa.VentaID);
                cmd.Parameters.AddWithValue("@productoid", detallesventa.ProductoID);
                cmd.Parameters.AddWithValue("@subtotal", detallesventa.Subtotal);
                try
                {
                    oConexion.Open();
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    return filasAfectadas > 0; // Devuelve true si se actualizó al menos una fila
                }
                catch (Exception ex)
                {
                    // Aquí podrías loguear el error
                    return false; // Retorna false si hubo un error
                }
            }
        }

        // GET: DetalleVentaController/Delete/5
        public ActionResult Delete(string id)
        {
            DetallesVenta eldetalleventa;
            eldetalleventa = ObtenerDetalleVenta(id);
            return View(eldetalleventa);
        }

        // POST: DetalleVentaController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection, DetallesVenta cliente)
        {
            try
            {
                List<DetallesVenta> laLista;
                DetallesVenta eldetalleventa;
                eldetalleventa = ObtenerDetalleVenta(id);
                laLista = ObtenerDetalleVenta();
                laLista.Remove(eldetalleventa);
                EliminarDetalleVentaEnBaseDeDatos(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al eliminar el detalle de venta: " + ex.Message);
                return View();
            }
        }

        // Método para eliminar el detalle de venta de la base de datos usando el procedimiento almacenado
        private void EliminarDetalleVentaEnBaseDeDatos(string id)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.rutaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("usp_DetallesVenta_Eliminar", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetro al procedimiento
                    cmd.Parameters.AddWithValue("@DetallesVentaID", id);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}