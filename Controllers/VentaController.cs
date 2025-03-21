using IronMongeryTools.Data;
using IronMongeryTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using System.Data;

using IronMongeryTools.Services;

namespace IronMongeryTools.Controllers
{
    
    public class VentaController : Controller
    {
        // GET: ProductoController
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;

        public VentaController(IMemoryCache cache, IAccesoService accesoService, IEmailService emailService)
        {
            MemoriaCache = cache;
            _accesoService = accesoService;
            this.emailService = emailService;
        }

        private List<Ventas> ObtenerVentas()
        {
            List<Ventas> EfectoVenta;
            EfectoVenta = CargarVentaDesdeBaseDeDatos();
            if (EstaVacioElCache())
            {
                // Cargar productos desde la base de datos
                

                // Almacenar la lista cargada en el cache
                MemoriaCache.Set("ListaVenta", EfectoVenta);
            }
            else
            {
                // Recuperar los productos desde el cache
                EfectoVenta = (List<Ventas>)MemoriaCache.Get("ListaVenta");
            }

            return EfectoVenta;
        }

        private List<Ventas> CargarVentaDesdeBaseDeDatos()
        {
            var listaVenta = new List<Ventas>();

            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Ventas_Obtener", oConexion)
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
                            listaVenta.Add(new Ventas
                            {
                                VentasID = reader["VentasID"]?.ToString(),
                                UsuarioID = reader["UsuarioID"]?.ToString(),
                                Fecha = (DateTime)reader["Fecha"],
                                Total = Convert.ToDouble(reader["Total"])
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al cargar las ventas desde la base de datos", ex);
                }
            }

            return listaVenta;
        }

        private Ventas ObtenerVentas(string id)
        {

            List<Ventas> laLista;
            laLista = ObtenerVentas();

            foreach (Ventas ventas in laLista)
            {
                if (ventas.VentasID == id)
                    return ventas;
            }

            return null;

        }
        private bool EstaVacioElCache()
        {
            if (MemoriaCache.Get("ListaVenta") is null)
                return true;
            else
                return false;
        }

        // GET: VentaController
        public ActionResult Index()
        {
            List<Ventas> laLista;
            laLista = ObtenerVentas();
            int totalventas = laLista.Count;
            ViewData["TotalVentas"] = totalventas;
            return View(laLista);
        }

        // GET: VentaController/Details/
        public ActionResult Details(string id)
        {
            Ventas laventa;
            laventa = ObtenerVentas(id);
            return View(laventa);
        }

        // GET: VentaController/Create
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
        private bool Registrar(Ventas ventas, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();

                    string query = @"
                INSERT INTO Ventas (VentasID, UsuarioID, Fecha, Total)

                VALUES (@VentasID, @UsuarioID, @Fecha, @Total)";

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@VentasID", ventas.VentasID);
                        cmd.Parameters.AddWithValue("@UsuarioID", ventas.UsuarioID);
                        cmd.Parameters.AddWithValue("@Fecha", ventas.Fecha);
                        cmd.Parameters.AddWithValue("@Total", ventas.Total);
                        int filasAfectadas = cmd.ExecuteNonQuery();

                        return filasAfectadas > 0;
                    }

                }
            }
            catch (SqlException ex)
            {
                mensaje = $"Error al registrar la venta: {ex.Message}";
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

        // POST: ProductoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Ventas ventas)
        {
            try
            {
                List<Ventas> laLista;
                laLista = ObtenerVentas();
                laLista.Add(ventas);
                string mensaje;
                bool r = ProbarConexion(out mensaje);
                bool conexionValida = ProbarConexion(out mensaje);
                if (!conexionValida)
                {
                    ModelState.AddModelError("", mensaje);
                    return View(ventas);
                }

                bool registroExitoso = Registrar(ventas, out mensaje);

                if (!registroExitoso)
                {
                    ModelState.AddModelError(string.Empty, mensaje);
                    return View(ventas);
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }

                //return RedirectToAction("Index", "Acceso");

            }
            catch
            {
                return View();
            }
        }

        // GET: ProductoController/Edit/5
        public ActionResult Edit(string id)
        {
            Ventas laventa;
            laventa = ObtenerVentas(id);

            return View(laventa);
        }

        // POST: VentasController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, Ventas Ventas)
        {
            try
            {
                var laventa = ObtenerVentas(id); // Obtener la venta
                if (laventa == null)
                {
                    return NotFound(); // Retorna un error 404 si no se encuentra la venta
                }

                // Actualizar datos
                laventa.VentasID = Ventas.VentasID;
                laventa.UsuarioID = Ventas.UsuarioID;
                laventa.Fecha = Ventas.Fecha;
                laventa.Total = Ventas.Total;

                // Aquí debes guardar los cambios en la base de datos
                bool actualizado = ActualizarVentaEnBaseDeDatos(laventa);
                if (actualizado)
                {
                    return RedirectToAction(nameof(Index)); // Redirige a la lista de ventas
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar la venta.");
                    return View(Ventas); // Devuelve la vista si no se puede actualizar
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(Ventas); // Devuelve la vista si hay un error
            }
        }

        private bool ActualizarVentaEnBaseDeDatos(Ventas ventas)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Ventas_Modificar", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Aquí pasas todos los parámetros necesarios
                cmd.Parameters.AddWithValue("@ventasid", ventas.VentasID);
                cmd.Parameters.AddWithValue("@usuarioid", ventas.UsuarioID);
                cmd.Parameters.AddWithValue("@fecha", ventas.Fecha);
                cmd.Parameters.AddWithValue("@total", ventas.Total);
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

        // GET: VentaController/Delete/5
        public ActionResult Delete(string id)
        {
            Ventas laventa;
            laventa = ObtenerVentas(id);
            return View(laventa);
        }

        // POST: VentaController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection, Ventas cliente)
        {
            try
            {
                List<Ventas> laLista;
                Ventas laventa;
                laventa = ObtenerVentas(id);
                laLista = ObtenerVentas();
                laLista.Remove(laventa);
                EliminarVentaEnBaseDeDatos(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al eliminar la venta: " + ex.Message);
                return View();
            }
        }

        // Método para eliminar una venta de la base de datos usando el procedimiento almacenado
        private void EliminarVentaEnBaseDeDatos(string id)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.rutaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("usp_Ventas_Eliminar", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetro al procedimiento
                    cmd.Parameters.AddWithValue("@VentasID", id);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}