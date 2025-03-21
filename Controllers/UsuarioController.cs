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
    public class UsuarioController : Controller
    {
        // GET: ClienteController1
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;

        public UsuarioController(IMemoryCache cache, IAccesoService accesoService, IEmailService emailService)
        {
            MemoriaCache = cache;
            _accesoService = accesoService;
            this.emailService = emailService;
        }

        private List<Usuarios> ObtenerUsuarios()
        {
            List<Usuarios> EfectoUsuario;
            // Cargar usuarios desde la base de datos
                EfectoUsuario = CargarUsuariosDesdeBaseDeDatos();

            if (EstaVacioElCache())
            {
                

                // Almacenar la lista cargada en el cache
                MemoriaCache.Set("ListaUsuario", EfectoUsuario);
            }
            else
            {
                // Recuperar los usuarios desde el cache
                EfectoUsuario = (List<Usuarios>)MemoriaCache.Get("ListaUsuario");
                
            }

            return EfectoUsuario;
        }

        private List<Usuarios> CargarUsuariosDesdeBaseDeDatos()
        {
            var listaUsuarios = new List<Usuarios>();

            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Usuarios_Obtener", oConexion)
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
                            listaUsuarios.Add(new Usuarios
                            {
                                UsuarioID = reader["UsuarioID"]?.ToString(),
                                Nombre = reader["Nombre"]?.ToString(),
                                FechaNacimiento = reader["FechaNacimiento"]?.ToString(),
                                Correo = reader["Correo"]?.ToString(),
                                Token = reader["Token"]?.ToString(),
                                PasswordHash = reader["PasswordHash"]?.ToString(),
                                NewPasswordHash = reader["NewPasswordHash"]?.ToString(),
                                PasswordResetToken = reader["PasswordResetToken"]?.ToString(),
                                EmailConfirmationToken = reader["EmailConfirmationToken"]?.ToString(),
                                /* ! */
                                EmailConfirmed = reader["EmailConfirmed"]?.GetType() == typeof(bool) ? (bool)reader["EmailConfirmed"] : false
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Aquí puedes registrar el error si es necesario.
                    throw new Exception("Error al cargar los usuarios desde la base de datos", ex);
                }
            }

            return listaUsuarios;
        }

        private Usuarios ObtenerUsuarios(string id)
        {

            List<Usuarios> laLista;
            laLista = ObtenerUsuarios();

            foreach (Usuarios usuario in laLista)
            {
                if (usuario.UsuarioID == id)
                    return usuario;
            }

            return null;

        }
        private bool EstaVacioElCache()
        {
            if (MemoriaCache.Get("ListaUsuario") is null)
                return true;
            else
                return false;
        }

        // GET: ClienteController
        public ActionResult Index()
        {
            List<Usuarios> laLista;
            laLista = ObtenerUsuarios();
            int totalUsuarios = laLista.Count;
            ViewData["TotalUsuarios"] = totalUsuarios;
            return View(laLista);
        }

        // GET: ClienteController/Details/5
        public ActionResult Details(string id)
        {
            Usuarios elCliente;
            elCliente = ObtenerUsuarios(id);
            return View(elCliente);
        }

        // GET: ClienteController/Create
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
        private bool Registrar(Usuarios usuario, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();

                    string query = @"
                INSERT INTO Usuarios (UsuarioID, Nombre, FechaNacimiento, Correo,
                                      Token, PasswordHash, NewPasswordHash, PasswordResetToken, 
                                      EmailConfirmationToken, EmailConfirmed)

                VALUES (@UsuarioID, @Nombre, @FechaNacimiento, @Correo, @Token,
                        @PasswordHash, @NewPasswordHash, @PasswordResetToken, @EmailConfirmationToken,
                        @EmailConfirmed)";

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@UsuarioID", usuario.UsuarioID);
                        cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);
                        cmd.Parameters.AddWithValue("@FechaNacimiento", usuario.FechaNacimiento);
                        cmd.Parameters.AddWithValue("@Correo", usuario.Correo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Token", usuario.Token ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PasswordHash", usuario.PasswordHash ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NewPasswordHash", usuario.NewPasswordHash ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PasswordResetToken", usuario.PasswordResetToken ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailConfirmationToken", usuario.EmailConfirmationToken ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailConfirmed", usuario.EmailConfirmed);

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

        // POST: ClienteController/Create
        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Usuarios usuario)
        {
            try
            {

                if (!IsValidEmail(usuario.Correo))
                {
                    ModelState.AddModelError("Correo", "El formato del correo electrónico no es válido.");
                    return View(usuario);
                }

                if (!Validaciones.EsCedula(usuario.UsuarioID))
                {
                    ModelState.AddModelError("Correo", "El formato de cédula no es válido.");
                    return View(usuario);
                }

                List<Usuarios> laLista;
                laLista = ObtenerUsuarios();
                laLista.Add(usuario);
                string mensaje;
                bool r = ProbarConexion(out mensaje);
                bool conexionValida = ProbarConexion(out mensaje);
                if (!conexionValida)
                {
                    ModelState.AddModelError("", mensaje);
                    return View(usuario);
                }

                // Encriptar contraseña y generar token
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.PasswordHash);
                usuario.EmailConfirmationToken = Guid.NewGuid().ToString();
                usuario.EmailConfirmed = false;
                bool registroExitoso = Registrar(usuario, out mensaje);

                // Llama al método Registrar y verifica si fue exitoso.


                if (!registroExitoso)
                {
                    ModelState.AddModelError(string.Empty, mensaje);
                    return View(usuario);
                }

                // Generar enlace de confirmación
                string confirmationLink = Url.Action("ConfirmarCorreo", "Usuario",
                    new { token = usuario.EmailConfirmationToken, email = usuario.Correo },
                    protocol: Request.Scheme);

                // Enviar correo de confirmación
                await emailService.SendEmailAsync(usuario.Correo, "Confirmación de Correo",
                    $"Haz clic en el siguiente enlace para confirmar tu correo:" +
                    $"http://localhost:5283/Usuario/ConfirmarCorreo?token={WebUtility.UrlEncode(usuario.EmailConfirmationToken)}&email={WebUtility.UrlEncode(usuario.Correo)}");

                
                return RedirectToAction("Index","Usuario");

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

        // GET: ClienteController/Edit/5
        public ActionResult Edit(string id)
        {
            Usuarios elusuario;
            elusuario = ObtenerUsuarios(id);

            return View(elusuario);
        }

        // POST: ClienteController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, Usuarios Usuario)
        {
            try
            {
                var elusuario = ObtenerUsuarios(id); // Obtener usuario
                if (elusuario == null)
                {
                    return NotFound(); // Retorna un error 404 si no se encuentra el usuario
                }

                // Actualizar datos
                elusuario.UsuarioID = Usuario.UsuarioID;
                elusuario.Nombre = Usuario.Nombre;
                elusuario.FechaNacimiento = Usuario.FechaNacimiento;
                elusuario.Correo = Usuario.Correo;

                // Aquí debes guardar los cambios en la base de datos
                bool actualizado = ActualizarUsuarioEnBaseDeDatos(elusuario);
                if (actualizado)
                {
                    return RedirectToAction(nameof(Index)); // Redirige a la lista de usuarios
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar el usuario.");
                    return View(Usuario); // Devuelve la vista si no se puede actualizar
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(Usuario); // Devuelve la vista si hay un error
            }
        }

        private bool ActualizarUsuarioEnBaseDeDatos(Usuarios usuario)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Usuarios_Modificar", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Aquí pasas todos los parámetros necesarios
                cmd.Parameters.AddWithValue("@usuarioid", usuario.UsuarioID);
                cmd.Parameters.AddWithValue("@nombre", usuario.Nombre);
                cmd.Parameters.AddWithValue("@fechanacimiento", usuario.FechaNacimiento);
                cmd.Parameters.AddWithValue("@correo", usuario.Correo);

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

        // GET: ClienteController/Delete/5
        public ActionResult Delete(string id)
        {
            Usuarios elusuario;
            elusuario = ObtenerUsuarios(id);
            return View(elusuario);
        }
        [Authorize]
        // POST: ClienteController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection, Usuarios cliente)
        {
            try
            {
                List<Usuarios> laLista;
                Usuarios elusuario;
                elusuario = ObtenerUsuarios(id);
                laLista = ObtenerUsuarios();
                laLista.Remove(elusuario);
                EliminarUsuarioEnBaseDeDatos(id);
                if (User.IsInRole("Administrador"))
                {
                    // Si es administrador, redirigir al Index de usuarios
                    TempData["Mensaje"] = "Usuario eliminado correctamente.";
                    return RedirectToAction("Index", "Usuario"); // Redirigir al Index de usuarios
                }
                else
                {
                    // Si es un usuario normal, redirigir a la página de inicio de sesión
                    TempData["Mensaje"] = "Tu cuenta ha sido eliminada correctamente.";
                    return RedirectToAction("Index", "Acceso"); // Redirigir al Index de Acceso (inicio de sesión)
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al eliminar el usuario: " + ex.Message);
                return View();
            }
        }

        // Método para eliminar un usuario de la base de datos usando el procedimiento almacenado
        private void EliminarUsuarioEnBaseDeDatos(string cedula)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.rutaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("usp_Usuarios_Eliminar", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetro al procedimiento
                    cmd.Parameters.AddWithValue("@UsuarioID", cedula);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}