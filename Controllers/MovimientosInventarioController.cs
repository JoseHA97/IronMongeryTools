using IronMongeryTools.Data;
using IronMongeryTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using System.Data;

using IronMongeryTools.Services;

namespace IronMongeryTools.Controllers
{
    public class MovimientosInventarioController : Controller
    {
        // GET: MovimientosInventarioController
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;

        public MovimientosInventarioController(IMemoryCache cache, IAccesoService accesoService, IEmailService emailService)
        {
            MemoriaCache = cache;
            _accesoService = accesoService;
            this.emailService = emailService;
        }

        private List<MovimientosInventario> ObtenerMovimientoInventario()
        {
            List<MovimientosInventario> EfectoMovimientoInventario;
            EfectoMovimientoInventario = CargarMovimientoInventarioDesdeBaseDeDatos();

            if (EstaVacioElCache())
            {
                
                // Almacenar la lista cargada en el cache
                MemoriaCache.Set("ListaMovimientoInventario", EfectoMovimientoInventario);
            }
            else
            {
                // Recuperar el movimiento inventario desde el cache
                EfectoMovimientoInventario = (List<MovimientosInventario>)MemoriaCache.Get("ListaMovimientoInventario");
            }

            return EfectoMovimientoInventario;
        }

        private List<MovimientosInventario> CargarMovimientoInventarioDesdeBaseDeDatos()
        {
            var listaMovimientoInventario= new List<MovimientosInventario>();

            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_MovimientosInventario_Obtener", oConexion)
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
                            listaMovimientoInventario.Add(new MovimientosInventario
                            {
                                MovimientosInventarioID = reader["MovimientosInventarioID"]?.ToString(),
                                ProductoID = reader["ProductoID"]?.ToString(),
                                TipoMovimiento = reader["TipoMovimiento"]?.ToString(),
                                Cantidad = Convert.ToInt32(reader["Cantidad"]),
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

            return listaMovimientoInventario;
        }

        private MovimientosInventario ObtenerMovimientoInventario(string id)
        {

            List<MovimientosInventario> laLista;
            laLista = ObtenerMovimientoInventario();

            foreach (MovimientosInventario movimientoinventario in laLista)
            {
                if (movimientoinventario.MovimientosInventarioID == id)
                    return movimientoinventario;
            }

            return null;

        }
        private bool EstaVacioElCache()
        {
            if (MemoriaCache.Get("ListaMovimientoInventario") is null)
                return true;
            else
                return false;
        }

        // GET: MovimientosInventarioController
        public ActionResult Index()
        {
            List<MovimientosInventario> laLista;
            laLista = ObtenerMovimientoInventario();
            int totalmovimientoinventario = laLista.Count;
            ViewData["TotalMovimientoInventario"] = totalmovimientoinventario;
            return View(laLista);
        }

        // GET: MovimientosInventarioController/Details/
        public ActionResult Details(string id)
        {
            MovimientosInventario elmovimientoinventario;
            elmovimientoinventario = ObtenerMovimientoInventario(id);
            return View(elmovimientoinventario);
        }

        // GET: MovimientosInventarioController/Create
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
        private bool Registrar(MovimientosInventario movimientosInventario, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();

                    string query = @"
                INSERT INTO MovimientosInventario (MovimientosInventarioID, ProductosID, TipoMovimiento, Cantidad)

                VALUES (@MovimientosInventarioID, @ProductosID, @TipoMovimiento, @Cantidad)";

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@movimientosinventarioid", movimientosInventario.MovimientosInventarioID);
                        cmd.Parameters.AddWithValue("@productoid", movimientosInventario.ProductoID);
                        cmd.Parameters.AddWithValue("@tipomovimiento", movimientosInventario.TipoMovimiento);
                        cmd.Parameters.AddWithValue("@Cantidad", movimientosInventario.Cantidad);
                        int filasAfectadas = cmd.ExecuteNonQuery();

                        return filasAfectadas > 0;
                    }

                }
            }
            catch (SqlException ex)
            {
                mensaje = $"Error al registrar el movimiento de inventario: {ex.Message}";
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

        // POST: MovimientosInventarioController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(MovimientosInventario movimientosInventario)
        {
            try
            {
                List<MovimientosInventario> laLista;
                laLista = ObtenerMovimientoInventario();
                laLista.Add(movimientosInventario);
                string mensaje;
                bool r = ProbarConexion(out mensaje);
                bool conexionValida = ProbarConexion(out mensaje);
                if (!conexionValida)
                {
                    ModelState.AddModelError("", mensaje);
                    return View(movimientosInventario);
                }
                bool registroExitoso = Registrar(movimientosInventario, out mensaje);

                if(!registroExitoso)
                {
                    ModelState.AddModelError(string.Empty, mensaje);
                    return View(movimientosInventario);
                }else{
                    return RedirectToAction(nameof(Index));
                }

            }
            catch
            {
                return View();
            }
        }

        // GET: MovimientosInventarioController/Edit/5
        public ActionResult Edit(string id)
        {
            MovimientosInventario elmovimientoinventario;
            elmovimientoinventario = ObtenerMovimientoInventario(id);

            return View(elmovimientoinventario);
        }

        // POST: MovimientosInventarioController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, MovimientosInventario MovimientosInventario)
        {
            try
            {
                var elmovimientoinventario = ObtenerMovimientoInventario(id); // Obtener el detalle de venta
                if (elmovimientoinventario == null)
                {
                    return NotFound(); // Retorna un error 404 si no se encuentra el movimiento de inventario
                }

                // Actualizar datos
                elmovimientoinventario.MovimientosInventarioID = MovimientosInventario.MovimientosInventarioID;
                elmovimientoinventario.ProductoID = MovimientosInventario.ProductoID;
                elmovimientoinventario.TipoMovimiento = MovimientosInventario.TipoMovimiento;
                elmovimientoinventario.Cantidad = MovimientosInventario.Cantidad;

                // Aquí debes guardar los cambios en la base de datos
                bool actualizado = ActualizarMovimientoInventarioEnBaseDeDatos(elmovimientoinventario);
                if (actualizado)
                {
                    return RedirectToAction(nameof(Index)); // Redirige a la lista de movimiento de inventario
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar el detalle de venta.");
                    return View(MovimientosInventario); // Devuelve la vista si no se puede actualizar
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(MovimientosInventario); // Devuelve la vista si hay un error
            }
        }

        private bool ActualizarMovimientoInventarioEnBaseDeDatos(MovimientosInventario movimientosInventario)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_MovimientosInventario_Modificar", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Aquí pasas todos los parámetros necesarios
                cmd.Parameters.AddWithValue("@movimientosinventarioid", movimientosInventario.MovimientosInventarioID);
                cmd.Parameters.AddWithValue("@productoid", movimientosInventario.ProductoID);
                cmd.Parameters.AddWithValue("@tipomovimiento", movimientosInventario.TipoMovimiento);
                cmd.Parameters.AddWithValue("@cantidad", movimientosInventario.Cantidad);
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

        // GET: MovimientosInventarioController/Delete/5
        public ActionResult Delete(string id)
        {
            MovimientosInventario elmovimientoinventario;
            elmovimientoinventario = ObtenerMovimientoInventario(id);
            return View(elmovimientoinventario);
        }

        // POST: MovimientosInventarioController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection, MovimientosInventario cliente)
        {
            try
            {
                List<MovimientosInventario> laLista;
                MovimientosInventario elmovimientoinventario;
                elmovimientoinventario = ObtenerMovimientoInventario(id);
                laLista = ObtenerMovimientoInventario();
                laLista.Remove(elmovimientoinventario);
                EliminarMovimientoInventarioEnBaseDeDatos(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al eliminar el movimiento inventario: " + ex.Message);
                return View();
            }
        }

        // Método para eliminar el movimiento inventario de la base de datos usando el procedimiento almacenado
        private void EliminarMovimientoInventarioEnBaseDeDatos(string id)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.rutaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("usp_MovimientosInventario_Eliminar", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetro al procedimiento
                    cmd.Parameters.AddWithValue("@MovimientosInventarioID", id);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}