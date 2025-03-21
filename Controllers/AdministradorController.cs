using IronMongeryTools.Data;
using IronMongeryTools.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using IronMongeryTools.Services;

namespace IronMongeryTools.Controllers
{
    public class AdministradorController : Controller
    {
        // GET: AdministradorController
        
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;

        public AdministradorController(IMemoryCache cache, IAccesoService accesoService, IEmailService emailService)
        {
            MemoriaCache = cache;
            _accesoService = accesoService;
            this.emailService = emailService;
        }

        private List<Administrador> ObtenerAdministrador()
        {
            List<Administrador> EfectoAdministrador;

            if (EstaVacioElCache())
            {
                // Cargar administradores desde la base de datos
                EfectoAdministrador = CargarAdministradorDesdeBaseDeDatos();

                // Almacenar la lista cargada en el cache
                MemoriaCache.Set("ListaAdministrador", EfectoAdministrador);
            }
            else
            {
                // Recuperar los administradores desde el cache
                EfectoAdministrador = (List<Administrador>)MemoriaCache.Get("ListaAdministrador");
            }

            return EfectoAdministrador;
        }

        private List<Administrador> CargarAdministradorDesdeBaseDeDatos()
        {
            var listaAdministrador = new List<Administrador>();

            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Administradores_Obtener", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                try
                {
                    oConexion.Open();

                    using (var reader = cmd.ExecuteReader())
                    {

                        if (!reader.HasRows)
                        {
                            return new List<Administrador>(); // Devuelve lista vacía en lugar de error
                        }

                        while (reader.Read())
                        {
                            listaAdministrador.Add(new Administrador
                            {
                                AdministradorID = reader["AdministradorID"]?.ToString(),
                                Nombre = reader["Nombre"]?.ToString(),
                                FechaNacimiento = reader["FechaNacimiento"]?.ToString(),
                                Correo = reader["Correo"]?.ToString(),
                                Token = reader["Token"]?.ToString(),
                                PasswordHash = reader["PasswordHash"]?.ToString(),
                                NewPasswordHash = reader["NewPasswordHash"]?.ToString(),
                                PasswordResetToken = reader["PasswordResetToken"]?.ToString(),
                                EmailConfirmationToken = reader["EmailConfirmationToken"]?.ToString(),

                                // Manejo seguro de EmailConfirmed
                                EmailConfirmed = reader["EmailConfirmed"] != DBNull.Value && Convert.ToBoolean(reader["EmailConfirmed"])
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al cargar los administradores desde la base de datos", ex);
                }
            }

            return listaAdministrador;
        }

        private Administrador ObtenerAdministrador(string id)
        {
            List<Administrador> laLista;
            laLista = ObtenerAdministrador();

            foreach (Administrador usuario in laLista)
            {
                if (usuario.AdministradorID == id)
                    return usuario;
            }

            return null;

        }
        private bool EstaVacioElCache()
        {
            if (MemoriaCache.Get("ListaAdministrador") is null)
                return true;
            else
                return false;
        }

        // GET: AdministradorController
        [Authorize(Roles = "Administrador")]
        public ActionResult Index()
        {
            List<Administrador> laLista;
            laLista = ObtenerAdministrador();
            int totalAdministradores = laLista.Count;
            ViewData["TotalAdministradores"] = totalAdministradores;
            return View(laLista);
        }

        // GET: AdministradorController/Details/
        public ActionResult Details(string id)
        {
            Administrador elCliente;
            elCliente = ObtenerAdministrador(id);
            return View(elCliente);
        }

        // GET: AdministradorController/Create
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
        private bool Registrar(Administrador administrador, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();

                    string query = @"
                INSERT INTO Administrador (AdministradorID, Nombre, FechaNacimiento, Correo, 
                                      Token, PasswordHash, NewPasswordHash, PasswordResetToken, 
                                      EmailConfirmationToken, EmailConfirmed)

                VALUES (@AdministradorID, @Nombre, @FechaNacimiento, @Correo, @Token,
                        @PasswordHash, @NewPasswordHash, @PasswordResetToken, @EmailConfirmationToken,
                        @EmailConfirmed)";

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@AdministradorID", administrador.AdministradorID);
                        cmd.Parameters.AddWithValue("@Nombre", administrador.Nombre);
                        cmd.Parameters.AddWithValue("@FechaNacimiento", administrador.FechaNacimiento);
                        cmd.Parameters.AddWithValue("@Correo", administrador.Correo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Token", administrador.Token ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PasswordHash", administrador.PasswordHash ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NewPasswordHash", administrador.NewPasswordHash ?? (Object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PasswordResetToken", administrador.PasswordResetToken ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailConfirmationToken", administrador.EmailConfirmationToken ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailConfirmed", administrador.EmailConfirmed);

                        int filasAfectadas = cmd.ExecuteNonQuery();

                        return filasAfectadas > 0;
                    }

                }
            }
            catch (SqlException ex)
            {
                mensaje = $"Error al registrar el usuario: {ex.Message}";
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

        // POST: AdministradorController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Administrador administrador)
        {
            try
            {

                if (!IsValidEmail(administrador.Correo))
                {
                    ModelState.AddModelError("Correo", "El formato del correo electrónico no es válido.");
                    return View(administrador);
                }

                if (!Validaciones.EsAdministradorID(administrador.AdministradorID))
                {
                    ModelState.AddModelError("Correo", "El formato de cédula no es válido.");
                    return View(administrador);
                }

                List<Administrador> laLista;
                laLista = ObtenerAdministrador();
                laLista.Add(administrador);
                string mensaje;
                bool r = ProbarConexion(out mensaje);
                bool conexionValida = ProbarConexion(out mensaje);
                if (!conexionValida)
                {
                    ModelState.AddModelError("", mensaje);
                    return View(administrador);
                }

                // Encriptar contraseña y generar token
                administrador.PasswordHash = BCrypt.Net.BCrypt.HashPassword(administrador.PasswordHash);
                administrador.EmailConfirmationToken = Guid.NewGuid().ToString();
                administrador.EmailConfirmed = false;
                bool registroExitoso = Registrar(administrador, out mensaje);

                // Llama al método Registrar y verifica si fue exitoso.

                if (!registroExitoso)
                {
                    ModelState.AddModelError(string.Empty, mensaje);
                    return View(administrador);
                }

                // Generar enlace de confirmación
                string confirmationLink = Url.Action("ConfirmarCorreo", "Administrador",
                    new { token = administrador.EmailConfirmationToken, email = administrador.Correo },
                    protocol: Request.Scheme);

                // Enviar correo de confirmación
                await emailService.SendEmailAsync(administrador.Correo, "Confirmación de Correo",
                    $"Haz clic en el siguiente enlace para confirmar tu correo:" +
                    $"http://localhost:5263/Administrador/ConfirmarCorreo?token={WebUtility.UrlEncode(administrador.EmailConfirmationToken)}&email={WebUtility.UrlEncode(administrador.Correo)}");

                return RedirectToAction("Index", "Acceso");

            }
            catch
            {
                return View();
            }
        }

        private bool IsValidEmail(string email)
        {
            var emailPattern = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailPattern.IsMatch(email);
        }

        // GET: AdministradorController/Edit/
        public ActionResult Edit(string id)
        {
            Administrador eladministrador;
            eladministrador = ObtenerAdministrador(id);

            return View(eladministrador);
        }

        // POST: AdministradorController/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, Administrador Administrador)
        {
            try
            {
                var eladministrador = ObtenerAdministrador(id); // Obtener administrador
                if (eladministrador == null)
                {
                    return NotFound(); // Retorna un error 404 si no se encuentra el administrador
                }

                // Actualizar datos
                eladministrador.AdministradorID = Administrador.AdministradorID;
                eladministrador.Nombre = Administrador.Nombre;
                eladministrador.FechaNacimiento = Administrador.FechaNacimiento;
                eladministrador.Correo = Administrador.Correo;


                // Aquí debes guardar los cambios en la base de datos
                bool actualizado = ActualizarUsuarioEnBaseDeDatos(eladministrador);
                if (actualizado)
                {
                    return RedirectToAction(nameof(Index)); // Redirige a la lista de administradorers
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar el administrador.");
                    return View(Administrador); // Devuelve la vista si no se puede actualizar
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(Administrador); // Devuelve la vista si hay un error
            }
        }

        private bool ActualizarUsuarioEnBaseDeDatos(Administrador administrador)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Administrador_Modificar", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Aquí pasas todos los parámetros necesarios
                cmd.Parameters.AddWithValue("@administradorid", administrador.AdministradorID);
                cmd.Parameters.AddWithValue("@nombre", administrador.Nombre);
                cmd.Parameters.AddWithValue("@fechanacimiento", administrador.FechaNacimiento);
                cmd.Parameters.AddWithValue("@correo", administrador.Correo);

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

        // GET: AdministradorController/Delete/
        public ActionResult Delete(string id)
        {
            Administrador eladministrador;
            eladministrador = ObtenerAdministrador(id);
            return View(eladministrador);
        }

        // POST: AdministradorController/Delete/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection, Administrador cliente)
        {
            try
            {
                List<Administrador> laLista;
                Administrador eladministrador;
                eladministrador = ObtenerAdministrador(id);
                laLista = ObtenerAdministrador();
                laLista.Remove(eladministrador);
                EliminarAdministradorEnBaseDeDatos(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al eliminar el administrador: " + ex.Message);
                return View();
            }
        }

        // Método para eliminar un administrador de la base de datos usando el procedimiento almacenado
        private void EliminarAdministradorEnBaseDeDatos(string id)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.rutaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("usp_Administrador_Eliminar", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetro al procedimiento
                    cmd.Parameters.AddWithValue("@AdministradorID", id);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}