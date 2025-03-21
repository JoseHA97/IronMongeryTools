using IronMongeryTools.Data;
using IronMongeryTools.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using IronMongeryTools.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IronMongeryTools.Controllers
{
    public class ProveedorController : Controller
    {
        // GET: ProveedoresController
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;

        public ProveedorController(IMemoryCache cache, IAccesoService accesoService, IEmailService emailService)
        {
            MemoriaCache = cache;
            _accesoService = accesoService;
            this.emailService = emailService;
        }

        private List<Proveedores> ObtenerProveedor()
        {
            List<Proveedores> EfectoProveedor;

            if (EstaVacioElCache())
            {
                // Cargar proveedores desde la base de datos
                EfectoProveedor = CargarProveedorDesdeBaseDeDatos();

                // Almacenar la lista cargada en el cache
                MemoriaCache.Set("ListaProveedor", EfectoProveedor);
            }
            else
            {
                // Recuperar los proveedores desde el cache
                EfectoProveedor = (List<Proveedores>)MemoriaCache.Get("ListaProveedor");
            }

            return EfectoProveedor;
        }

        public List<Categoria> CargarCategoriasParaProveedor(string proveedorID)
        {
            var categorias = new List<Categoria>();

            using (SqlConnection conn = new SqlConnection(Conexion.rutaConexion))
            {
                conn.Open();

                // Consulta para obtener las categorías asociadas al proveedor
                string query = "SELECT CategoriaID, Nombre FROM Categorias WHERE ProveedorID = @ProveedorID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Agregar el parámetro ProveedorID
                    cmd.Parameters.AddWithValue("@ProveedorID", proveedorID);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Crear un objeto Categoria y agregarlo a la lista
                            categorias.Add(new Categoria
                            {
                                CategoriaID = Convert.ToInt32(reader["CategoriaID"]), // Obtener el CategoriaID
                                Nombre = reader["Nombre"].ToString(), // Obtener el Nombre de la categoría
                                ProveedorID = proveedorID // Asignar el ProveedorID (opcional, si lo necesitas en el objeto)

                            });
                        }
                    }
                }
            }

            return categorias;
        }

        public List<Proveedores> CargarProveedorDesdeBaseDeDatos()
        {
            var listaProveedor = new List<Proveedores>();

            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Proveedores_Obtener", oConexion)
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
                            var proveedor = new Proveedores
                            {
                                ProveedorID = reader["ProveedorID"]?.ToString(),
                                Nombre = reader["Nombre"]?.ToString(),
                                FechaNacimiento = reader["FechaNacimiento"]?.ToString(),
                                Telefono = reader["Telefono"]?.ToString(),
                                Correo = reader["Correo"]?.ToString(),
                                Direccion = reader["Direccion"]?.ToString(),
                                Token = reader["Token"]?.ToString(),
                                PasswordHash = reader["PasswordHash"]?.ToString(),
                                NewPasswordHash = reader["NewPasswordHash"]?.ToString(),
                                PasswordResetToken = reader["PasswordResetToken"]?.ToString(),
                                EmailConfirmationToken = reader["EmailConfirmationToken"]?.ToString(),
                                EmailConfirmed = reader["EmailConfirmed"] != DBNull.Value && Convert.ToBoolean(reader["EmailConfirmed"])
                            };

                            // Cargar las categorías asociadas al proveedor.
                            proveedor.Categorias = CargarCategoriasParaProveedor(proveedor.ProveedorID);

                            listaProveedor.Add(proveedor);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al cargar los proveedores desde la base de datos", ex);
                }
            }

            return listaProveedor;
        }

        private Proveedores ObtenerProveedor(string id)
        {

            List<Proveedores> laLista;
            laLista = ObtenerProveedor();

            foreach (Proveedores proveedores in laLista)
            {
                if (proveedores.ProveedorID == id)
                    return proveedores;
            }

            return null;

        }
        private bool EstaVacioElCache()
        {
            if (MemoriaCache.Get("ListaProveedor") is null)
                return true;
            else
                return false;
        }

        // GET: ProveedorController
        public ActionResult Index()
        {
            List<Proveedores> laLista;
            laLista = ObtenerProveedor();
            int totalproveedores = laLista.Count;
            // CargarProveedorDesdeBaseDeDatos();
            ViewData["TotalProveedores"] = totalproveedores;
            return View(laLista);
        }

        // GET: ProveedorController/Details/
        public ActionResult Details(string id)
        {
            Proveedores elProveedor;
            elProveedor = ObtenerProveedor(id);
            return View(elProveedor);
        }

        // GET: ProveedorController/Create
        public ActionResult Create()
        {
            var model = new Proveedores
            {
                // Al crear un nuevo proveedor, no hay ProveedorID todavía, pero se puede asignar una lista vacía de categorías.
                CategoriaList = new List<SelectListItem>()
            };

            return View(model);
        }

        // 🔹 Método para guardar el proveedor y obtener su ID
        private int GuardarProveedor(string nombre, string telefono, string correo, string direccion)
        {
            int proveedorID = 0;

            using (SqlConnection conn = new SqlConnection(Conexion.rutaConexion))
            {
                conn.Open();
                string sql = "INSERT INTO Proveedores (Nombre, Telefono, Correo, Direccion) OUTPUT INSERTED.ID VALUES (@Nombre, @Telefono, @Correo, @Direccion)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@Telefono", telefono);
                    cmd.Parameters.AddWithValue("@Correo", correo);
                    cmd.Parameters.AddWithValue("@Direccion", direccion);

                    // Obtener el ID del proveedor recién insertado
                    proveedorID = (int)cmd.ExecuteScalar();
                }
            }
            return proveedorID;
        }

        // 🔹 Método para guardar categorías asociadas a un proveedor
        private void GuardarCategoria(int proveedorID, string nombreCategoria)
        {
            using (SqlConnection conn = new SqlConnection(Conexion.rutaConexion))
            {
                conn.Open();
                string sql = "INSERT INTO Categorias (Nombre, ProveedorID) VALUES (@Nombre, @ProveedorID)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombreCategoria);
                    cmd.Parameters.AddWithValue("@ProveedorID", proveedorID);
                    cmd.ExecuteNonQuery();
                }
            }
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
        private bool Registrar(Proveedores proveedores, out string mensaje)
        {
            mensaje = string.Empty;

            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();

                    // 1. Insertar el proveedor
                    string queryProveedor = @"
                INSERT INTO Proveedores (ProveedorID, Nombre, FechaNacimiento, Telefono, Correo, Direccion, 
                                      Token, PasswordHash, NewPasswordHash, PasswordResetToken, 
                                      EmailConfirmationToken, EmailConfirmed)
                OUTPUT INSERTED.ProveedorID
                VALUES (@ProveedorID, @Nombre, @FechaNacimiento, @Telefono, @Correo, @Direccion, @Token,
                        @PasswordHash, @NewPasswordHash, @PasswordResetToken, @EmailConfirmationToken,
                        @EmailConfirmed)";

                    using (SqlCommand cmd = new SqlCommand(queryProveedor, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@ProveedorID", proveedores.ProveedorID);
                        cmd.Parameters.AddWithValue("@Nombre", proveedores.Nombre);
                        cmd.Parameters.AddWithValue("@FechaNacimiento", proveedores.FechaNacimiento);
                        cmd.Parameters.AddWithValue("@Telefono", proveedores.Telefono);
                        cmd.Parameters.AddWithValue("@Correo", proveedores.Correo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Direccion", proveedores.Direccion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Token", proveedores.Token ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PasswordHash", proveedores.PasswordHash ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NewPasswordHash", proveedores.NewPasswordHash ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PasswordResetToken", proveedores.PasswordResetToken ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailConfirmationToken", proveedores.EmailConfirmationToken ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailConfirmed", proveedores.EmailConfirmed);

                        // Obtener el ProveedorID del proveedor recién insertado
                        string proveedorID = cmd.ExecuteScalar().ToString();

                        // 2. Insertar las categorías asociadas al proveedor
                        if (proveedores.Categorias != null && proveedores.Categorias.Any())
                        {
                            foreach (var categoria in proveedores.Categorias)
                            {
                                string queryCategoria = @"
                            INSERT INTO Categorias (Nombre, ProveedorID)
                            VALUES (@Nombre, @ProveedorID)";

                                using (SqlCommand cmdCat = new SqlCommand(queryCategoria, oConexion))
                                {
                                    cmdCat.Parameters.AddWithValue("@Nombre", categoria.Nombre);
                                    cmdCat.Parameters.AddWithValue("@ProveedorID", proveedorID);
                                    cmdCat.ExecuteNonQuery();
                                }
                            }
                        }

                        return true;
                    }
                }
            }
            catch (SqlException ex)
            {
                mensaje = $"Error al registrar el proveedor: {ex.Message}";
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

        private void ActualizarCacheProveedores()
        {
            // Cargar proveedores desde la base de datos
            var proveedores = CargarProveedorDesdeBaseDeDatos();

            // Actualizar la caché
            MemoriaCache.Set("ListaProveedor", proveedores);
        }

        // POST: ProveedorController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Proveedores proveedores, string Categorias)
        {
            try
            {
                // Validaciones de correo y cédula
                if (!IsValidEmail(proveedores.Correo))
                {
                    ModelState.AddModelError("Correo", "El formato del correo electrónico no es válido.");
                    return View(proveedores);
                }

                if (!Validaciones.EsCedula(proveedores.ProveedorID))
                {
                    ModelState.AddModelError("Correo", "El formato de cédula no es válido.");
                    return View(proveedores);
                }

                // Encriptar contraseña y generar token
                proveedores.PasswordHash = BCrypt.Net.BCrypt.HashPassword(proveedores.PasswordHash);
                proveedores.EmailConfirmationToken = Guid.NewGuid().ToString();
                proveedores.EmailConfirmed = false;
                // Convertir la cadena de categorías en una lista
                if (!string.IsNullOrEmpty(Categorias))
                {
                    var categorias = Categorias.Split(',').Select(c => new Categoria
                    {
                        Nombre = c.Trim(),
                        ProveedorID = proveedores.ProveedorID
                    }).ToList();

                    proveedores.Categorias = categorias;
                }

                // Registrar el proveedor y sus categorías
                bool registroExitoso = Registrar(proveedores, out string mensaje);
                if (!registroExitoso)
                {
                    ModelState.AddModelError(string.Empty, mensaje);
                    return View(proveedores);
                }

                // Obtener el ProveedorID recién generado
                string proveedorID = proveedores.ProveedorID;
            
                // Actualizar el ViewBag para la lista de categorías (si es necesario en la vista de Create o Edit)
                ViewBag.CategoriaList = ObtenerListaCategorias(proveedorID);

                // Actualizar la caché de proveedores
                ActualizarCacheProveedores();

                // Generar enlace de confirmación
                string confirmationLink = Url.Action("ConfirmarCorreo", "Proveedor",
                    new { token = proveedores.EmailConfirmationToken, email = proveedores.Correo },
                    protocol: Request.Scheme);

                // Enviar correo de confirmación
                await emailService.SendEmailAsync(proveedores.Correo, "Confirmación de Correo",
                    $"Haz clic en el siguiente enlace para confirmar tu correo:" +
                    $"http://localhost:5283/Proveedor/ConfirmarCorreo?token={WebUtility.UrlEncode(proveedores.EmailConfirmationToken)}&email={WebUtility.UrlEncode(proveedores.Correo)}");

                return RedirectToAction("Index", "Proveedor");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(proveedores);
            }
        }

        private void RegistrarCategoria(string nombre, string proveedorID)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                string query = "INSERT INTO Categorias (Nombre, ProveedorID) VALUES (@Nombre, @ProveedorID)";

                using (SqlCommand cmd = new SqlCommand(query, oConexion))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@ProveedorID", proveedorID); // Añadir ProveedorID
                    oConexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private int ObtenerIdCategoria(string nombre)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                string query = "SELECT CategoriaID FROM Categorias WHERE Nombre = @Nombre";

                using (SqlCommand cmd = new SqlCommand(query, oConexion))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    oConexion.Open();
                    var result = cmd.ExecuteScalar();
                    return result != null ? (int)result : -1; // Retorna -1 si no se encuentra la categoría
                }
            }
        }

        private bool IsValidEmail(string email)
        {
            var emailPattern = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailPattern.IsMatch(email);
        }

        // GET: ProveedorController/Edit/5
        public ActionResult Edit(string id)
        {
            Proveedores elproveedor;
            elproveedor = ObtenerProveedor(id);
            elproveedor.CategoriaList = ObtenerListaCategorias(id).ToList();

            return View(elproveedor);
        }

        private IEnumerable<SelectListItem> ObtenerListaCategorias(string proveedorID)
        {
            var lista = new List<SelectListItem>();

            using (SqlConnection conn = new SqlConnection(Conexion.rutaConexion))
            {
                conn.Open();
                string query = "SELECT CategoriaID, Nombre FROM Categorias WHERE ProveedorID = @ProveedorID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProveedorID", proveedorID);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new SelectListItem
                            {
                                Value = reader["CategoriaID"].ToString(),
                                Text = reader["Nombre"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, Proveedores model, string nuevasCategorias)
        {
            try
            {
                // Obtener el proveedor existente a partir del ID
                var elProveedor = ObtenerProveedor(id);
                if (elProveedor == null)
                {
                    return NotFound();
                }

                // Actualizar los datos básicos
                elProveedor.Nombre = model.Nombre;
                elProveedor.FechaNacimiento = model.FechaNacimiento;
                elProveedor.Telefono = model.Telefono;
                elProveedor.Correo = model.Correo;
                elProveedor.Direccion = model.Direccion;

                bool actualizado = ActualizarProveedorEnBaseDeDatos(elProveedor);
                if (!actualizado)
                {
                    ModelState.AddModelError("", "Error al actualizar el proveedor.");
                    // Recargar la lista de categorías si es necesario
                    model.CategoriaList = ObtenerListaCategorias(elProveedor.ProveedorID);
                    return View(model);
                }

                // Actualizar las categorías asociadas al proveedor
                if (!string.IsNullOrEmpty(nuevasCategorias))
                {
                    var categorias = nuevasCategorias.Split(',').Select(c => c.Trim()).ToList();
                    ActualizarCategoriasProveedor(elProveedor.ProveedorID, categorias);
                }

                // Actualizar la caché
                ActualizarCacheProveedores();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(model);
            }
        }

        private void ActualizarCategoriasProveedor(string proveedorID, List<string> nuevasCategorias)
        {
            using (SqlConnection conn = new SqlConnection(Conexion.rutaConexion))
            {
                conn.Open();

                // Eliminar todas las categorías existentes para ese proveedor
                string deleteQuery = "DELETE FROM Categorias WHERE ProveedorID = @ProveedorID";
                using (SqlCommand cmdDel = new SqlCommand(deleteQuery, conn))
                {
                    cmdDel.Parameters.AddWithValue("@ProveedorID", proveedorID);
                    cmdDel.ExecuteNonQuery();
                }

                // Insertar las nuevas categorías
                if (nuevasCategorias != null && nuevasCategorias.Any())
                {
                    string insertQuery = "INSERT INTO Categorias (Nombre, ProveedorID) VALUES (@Nombre, @ProveedorID)";
                    foreach (var cat in nuevasCategorias)
                    {
                        using (SqlCommand cmdIns = new SqlCommand(insertQuery, conn))
                        {
                            cmdIns.Parameters.AddWithValue("@Nombre", cat);
                            cmdIns.Parameters.AddWithValue("@ProveedorID", proveedorID);
                            cmdIns.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private bool ActualizarProveedorEnBaseDeDatos(Proveedores proveedores)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Proveedores_Modificar", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Aquí pasas todos los parámetros necesarios
                cmd.Parameters.AddWithValue("@proveedorid", proveedores.ProveedorID);
                cmd.Parameters.AddWithValue("@nombre", proveedores.Nombre);
                cmd.Parameters.AddWithValue("@fechanacimiento", proveedores.FechaNacimiento);
                cmd.Parameters.AddWithValue("@telefono", proveedores.Telefono);
                cmd.Parameters.AddWithValue("@correo", proveedores.Correo);
                cmd.Parameters.AddWithValue("@direccion", proveedores.Direccion);

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
        #region Eliminar Proveedor

        // GET: ProveedorController/Delete/5
        public ActionResult Delete(string id)
        {
            Proveedores elproveedor;
            elproveedor = ObtenerProveedor(id);
            return View(elproveedor);
        }

        private void EliminarCategoriasProveedor(string proveedorID)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.rutaConexion))
            {
                string query = "DELETE FROM Categorias WHERE ProveedorID = @ProveedorID";

                using (SqlCommand cmd = new SqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@ProveedorID", proveedorID);
                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection, Proveedores cliente)
        {
            try
            {
                // Obtener el proveedor a eliminar
                Proveedores elproveedor = ObtenerProveedor(id);

                // Eliminar las categorías asociadas al proveedor
                EliminarCategoriasProveedor(id);

                // Eliminar el proveedor de la lista en memoria (si es necesario)
                List<Proveedores> laLista = ObtenerProveedor();
                laLista.Remove(elproveedor);

                // Eliminar el proveedor de la base de datos
                EliminarProveedorEnBaseDeDatos(id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al eliminar el proveedor: " + ex.Message);
                return View();
            }
        }

        private void EliminarProveedorEnBaseDeDatos(string id)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.rutaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("usp_Proveedores_Eliminar", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetro al procedimiento
                    cmd.Parameters.AddWithValue("@ProveedorID", id);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

    }
}