using System.Net;
using Microsoft.AspNetCore.Mvc;
using IronMongeryTools.Models;
using IronMongeryTools.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using System.Data;
using IronMongeryTools.Services;
using System.Text.RegularExpressions;

namespace GuanacasteTours.Controllers
{

    public class AccesoController : Controller
    {
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;
        private readonly string _connectionString = Conexion.rutaConexion;

        public IActionResult Index()
        {
            return View();
        }

        public AccesoController(IMemoryCache cache, IEmailService emailService, IAccesoService accesoService)
        {
            MemoriaCache = cache;
            this.emailService = emailService;
            _accesoService = accesoService;
        }

        public IActionResult Registro()
        {
            return View();
        }

        public IActionResult OlvidoContraseña()
        {
            return View();
        }

        public class EmailNoConfirmadoException : Exception
        {
            public EmailNoConfirmadoException(string message) : base(message) { }
        }

        public class ContrasenaIncorrectaException : Exception
        {
            public ContrasenaIncorrectaException(string message) : base(message) { }
        }

        public class CorreoNoRegistradoException : Exception
        {
            public CorreoNoRegistradoException(string message) : base(message) { }
        }


        private async Task<(bool isValid, Usuarios usuario, Administrador administrador, Proveedores proveedores)>
         ValidarUsuarioAsync(string correo, string password)
        {
            Usuarios usuario = null;
            Administrador administrador = null;
            Proveedores proveedores = null;
            // valida el usuario ingresado
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    await oConexion.OpenAsync();

                    string query = @"
                SELECT 'Usuario' AS Tipo, UsuarioID, Correo, PasswordHash, EmailConfirmed FROM Usuarios WHERE Correo = @Correo
                UNION
                SELECT 'Administrador' AS Tipo, AdministradorID, Correo, PasswordHash, EmailConfirmed FROM Administrador WHERE Correo = @Correo
                UNION
                SELECT 'Proveedor' AS Tipo, ProveedorID, Correo, PasswordHash, EmailConfirmed FROM Proveedores WHERE Correo = @Correo";

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@Correo", correo);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string tipoUsuario = reader["Tipo"].ToString();
                                string correoDB = reader["Correo"].ToString();
                                string passwordHash = reader["PasswordHash"].ToString();
                                bool emailConfirmed = Convert.ToBoolean(reader["EmailConfirmed"]);


                                // Verificar el correo confirmado
                                if (!emailConfirmed)
                                {
                                    throw new EmailNoConfirmadoException("Debes confirmar tu correo antes de iniciar sesión.");
                                }

                                // Verificar la contraseña ingresada 
                                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                                {
                                    throw new ContrasenaIncorrectaException("Contraseña incorrecta.");
                                }

                                // Determinar la entidad se encontró
                                if (tipoUsuario == "Usuario")
                                {
                                    usuario = new Usuarios { Correo = correoDB, PasswordHash = passwordHash };
                                    HttpContext.Session.SetString("UserRole", "Usuario");
                                }
                                else if (tipoUsuario == "Administrador")
                                {
                                    administrador = new Administrador { Correo = correoDB, PasswordHash = passwordHash };
                                    HttpContext.Session.SetString("UserRole", "Administrador");
                                }
                                else if (tipoUsuario == "Proveedor")
                                {
                                    proveedores = new Proveedores { Correo = correoDB, PasswordHash = passwordHash };
                                    HttpContext.Session.SetString("UserRole", "Proveedor");
                                }

                                return (true, usuario, administrador, proveedores);
                            }
                            else
                            {
                                throw new CorreoNoRegistradoException("El correo no esta registrado");
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Manejar excepción de SQL
                throw new Exception("Ocurrió un error al acceder a la base de datos.", ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(string correo, string passwordHash)
        {
            try
            {
                // Validar las credenciales en las tres tablas
                var (isValid, usuario, administrador, proveedor) = await ValidarUsuarioAsync(correo, passwordHash);

                if (isValid)
                {
                    // Autenticar al usuario según el tipo de entidad
                    if (usuario != null)
                    {
                        await AutenticarUsuario(usuario, "Usuario");
                        HttpContext.Session.SetString("Rol", "Administrador"); 
                    }
                    else if (administrador != null)
                    {
                        await AutenticarUsuario(administrador, "Administrador");
                        HttpContext.Session.SetString("Rol", "Administrador"); 
                    }
                    else if (proveedor != null)
                    {
                        await AutenticarUsuario(proveedor, "Proveedor");
                        HttpContext.Session.SetString("Rol", "Administrador"); 
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Error"] = "Correo o contraseña incorrectos.";
                    return View();
                }

            }    //Captura el tipo de excepción 
            catch (CorreoNoRegistradoException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Acceso");
            }
            catch (EmailNoConfirmadoException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Acceso");
            }
            catch (ContrasenaIncorrectaException ex)
            {
                TempData["Error"] = ex.Message;
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al procesar tu solicitud.";
                return View();
            }
        }

        private async Task AutenticarUsuario(object usuario, string rol)
        {
            // Obtener el correo del usuario, administrador o proveedor
            string correo = usuario.GetType().GetProperty("Correo")?.GetValue(usuario)?.ToString();

            if (string.IsNullOrEmpty(correo))
            {
                throw new Exception("No se pudo obtener el correo del usuario.");
            }

            // Crear las claims (información del usuario)
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, correo),
        new Claim(ClaimTypes.Role, rol) // Asignar el rol
    };

            // Crear la identidad y las propiedades de autenticación
            var claimsIdentity = new ClaimsIdentity(claims, "CustomCookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true // La cookie persistirá después de cerrar el navegador
            };

            // Iniciar sesión
            await HttpContext.SignInAsync(
                "CustomCookieAuth", // Esquema de autenticación
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );
        }

        private bool RegistrarUsuarioEnBaseDeDatos(object usuario, out string mensaje)
        {
            mensaje = string.Empty;
            string query = string.Empty;
            string tipoEntidad = usuario.GetType().Name; // Detecta si es Usuario, Administrador 

            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();

                    // Construir la consulta SQL según el tipo de entidad
                    query = ConstruirQueryRegistro(tipoEntidad);
                    if (string.IsNullOrEmpty(query))
                    {
                        mensaje = "Tipo de usuario no válido.";
                        return false;
                    }

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        // Asignar parámetros dinámicamente
                        AsignarParametros(cmd, usuario, tipoEntidad);
                        // Generar enlace de confirmación

                        string confirmationLink = Url.Action("ConfirmarCorreo", "Acceso",
                            new { token = ((dynamic)usuario).EmailConfirmationToken, email = ((dynamic)usuario).Correo },
                            protocol: Request.Scheme);

                        // Enviar correo de confirmación
                        var correo = usuario.GetType().GetProperty("Correo")?.GetValue(usuario)?.ToString();
                        var emailConfirmationToken = usuario.GetType().GetProperty("EmailConfirmationToken")?.GetValue(usuario)?.ToString();

                        emailService.SendEmailAsync(correo, "Confirmación de Correo",
                           $"Haz clic en el siguiente enlace para confirmar tu correo:" +
                           $"http://localhost:5263/Acceso/ConfirmarCorreo?token={WebUtility.UrlEncode(emailConfirmationToken)}&email={WebUtility.UrlEncode(correo)}");

                        int filasAfectadas = cmd.ExecuteNonQuery();

                        return filasAfectadas > 0;

                    }

                }

            }
            catch (SqlException ex)
            {
                mensaje = $"Error al registrar el {tipoEntidad}: {ex.Message}";
                return false;
            }
        }

        private string ConstruirQueryRegistro(string tipoEntidad)
        {                       //Contruye el query según el tipo de entidad
            switch (tipoEntidad)
            {
                case "Usuarios":
                    return @"
                INSERT INTO Usuarios (UsuarioID, Nombre, FechaNacimiento, Correo, 
                                      Token, PasswordHash, NewPasswordHash, PasswordResetToken, EmailConfirmationToken, EmailConfirmed)
                VALUES (@UsuarioID, @Nombre, @FechaNacimiento, @Correo, @Token, @PasswordHash, 
                        @NewPasswordHash, @PasswordResetToken, @EmailConfirmationToken, @EmailConfirmed)";

                case "Administrador":
                    return @"
                INSERT INTO Administrador (AdministradorID, Nombre, FechaNacimiento, Correo, 
                                      Token, PasswordHash, NewPasswordHash, PasswordResetToken, EmailConfirmationToken, EmailConfirmed)
                VALUES (@AdministradorID, @Nombre, @FechaNacimiento, @Correo, @Token, @PasswordHash, 
                        @NewPasswordHash, @PasswordResetToken, @EmailConfirmationToken, @EmailConfirmed)";

                case "Proveedores":
                    return @"
                INSERT INTO Proveedores (ProveedorID, Nombre, FechaNacimiento, Telefono, Correo, 
                                      Direccion, Token, PasswordHash, NewPasswordHash, PasswordResetToken, EmailConfirmationToken, EmailConfirmed)
                VALUES (@ProveedorID, @Nombre, @FechaNacimiento, @Telefono, @Correo, @Direccion, @Token, @PasswordHash, 
                        @NewPasswordHash, @PasswordResetToken, @EmailConfirmationToken, @EmailConfirmed)";

                default:
                    throw new ArgumentException("Tipo de entidad no válido.");
            }
        }

        private void AsignarParametros(SqlCommand cmd, object usuario, string tipoEntidad)
        {                                   //asignación de parametros
            Type tipo = usuario.GetType();

            if (tipoEntidad == "Usuarios")
            {
                cmd.Parameters.AddWithValue("@UsuarioID", tipo.GetProperty("UsuarioID")?.GetValue(usuario) ?? DBNull.Value);
            }
            else if (tipoEntidad == "Administrador")
            {
                cmd.Parameters.AddWithValue("@AdministradorID", tipo.GetProperty("AdministradorID")?.GetValue(usuario) ?? DBNull.Value);
            }
            else if (tipoEntidad == "Proveedores")
            {
                cmd.Parameters.AddWithValue("@ProveedorID", tipo.GetProperty("ProveedorID")?.GetValue(usuario) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Telefono", tipo.GetProperty("Telefono")?.GetValue(usuario) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Direccion", tipo.GetProperty("Direccion")?.GetValue(usuario) ?? DBNull.Value);
            }

            // Parámetros comunes para todas las entidades:
            cmd.Parameters.AddWithValue("@Nombre", tipo.GetProperty("Nombre")?.GetValue(usuario) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FechaNacimiento", tipo.GetProperty("FechaNacimiento")?.GetValue(usuario) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Correo", tipo.GetProperty("Correo")?.GetValue(usuario) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Token", tipo.GetProperty("Token")?.GetValue(usuario) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PasswordHash", tipo.GetProperty("PasswordHash")?.GetValue(usuario) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NewPasswordHash", tipo.GetProperty("NewPasswordHash")?.GetValue(usuario) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PasswordResetToken", tipo.GetProperty("PasswordResetToken")?.GetValue(usuario) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmailConfirmationToken", tipo.GetProperty("EmailConfirmationToken")?.GetValue(usuario) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmailConfirmed", tipo.GetProperty("EmailConfirmed")?.GetValue(usuario) ?? DBNull.Value);

        }

        private bool IsValidEmail(string email)
        {
            var emailPattern = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailPattern.IsMatch(email);
        }

        public bool ConfirmarCorreo(string token, string email)
        {
            try
            {
                using (SqlConnection oConexion = new SqlConnection(_connectionString))
                {
                    oConexion.Open();

                    // Buscar en las tres tablas cuál es la entidad correspondiente
                    string queryVerificar = @"
            SELECT TOP 1 Tipo FROM (
                SELECT 'Usuario' AS Tipo FROM Usuarios WHERE Correo = @Correo AND EmailConfirmationToken = @Token
                UNION ALL
                SELECT 'Administrador' AS Tipo FROM Administrador WHERE Correo = @Correo AND EmailConfirmationToken = @Token
                UNION ALL
                SELECT 'Proveedor' AS Tipo FROM Proveedores WHERE Correo = @Correo AND EmailConfirmationToken = @Token
            ) AS T";

                    string tipo = null;
                    using (SqlCommand cmdVerificar = new SqlCommand(queryVerificar, oConexion))
                    {
                        cmdVerificar.Parameters.Add("@Correo", SqlDbType.NVarChar, 100).Value = email;
                        cmdVerificar.Parameters.Add("@Token", SqlDbType.NVarChar, 255).Value = token;

                        object result = cmdVerificar.ExecuteScalar();
                        if (result != null)
                        {
                            tipo = result.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(tipo))
                    {
                        return false; // No se encontró la entidad
                    }

                    // Determinar la tabla a actualizar según el tipo encontrado
                    string tabla = tipo switch
                    {
                        "Usuario" => "Usuarios",
                        "Administrador" => "Administrador",
                        "Proveedor" => "Proveedores",
                        _ => throw new Exception("Tipo de entidad no válido.")
                    };

                    // Actualizar el estado de confirmación en la tabla correspondiente
                    string queryActualizar = $@"
            UPDATE {tabla}
            SET EmailConfirmed = 1, EmailConfirmationToken = NULL
            WHERE Correo = @Correo AND EmailConfirmationToken = @Token";

                    using (SqlCommand cmdActualizar = new SqlCommand(queryActualizar, oConexion))
                    {
                        cmdActualizar.Parameters.Add("@Correo", SqlDbType.NVarChar, 100).Value = email;
                        cmdActualizar.Parameters.Add("@Token", SqlDbType.NVarChar, 255).Value = token;

                        int rowsAffected = cmdActualizar.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }


            }
            catch (Exception ex)
            {
                // Log del error si es necesario
                Console.WriteLine("Error al confirmar correo: " + ex.Message);
                return false;
            }

        }

        private bool UsuarioYaRegistrado(string correo)
        {
            using (var connection = new SqlConnection(Conexion.rutaConexion))
            {
                connection.Open();
                var query = "SELECT COUNT(*) FROM Usuarios WHERE Correo = @Correo";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Correo", correo);
                    var count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Registro(RegistroViewModel model)
        {
            try
            {
                // Verificar si el usuario ya está registrado
                if (UsuarioYaRegistrado(model.Correo))
                {
                    TempData["Error"] = "Ya estás registrado. No puedes registrarte nuevamente.";
                    return View(model);
                }
                object entidad;
                switch (model.TipoRegistro)
                {
                    case "Usuario":
                        entidad = new Usuarios
                        {

                            UsuarioID = model.UsuarioID,
                            Nombre = model.Nombre,
                            FechaNacimiento = model.FechaNacimiento,
                            Correo = model.Correo,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash),
                            EmailConfirmationToken = Guid.NewGuid().ToString(),
                            EmailConfirmed = false
                        };
                        break;
                    case "Administrador":
                        entidad = new Administrador
                        {
                            AdministradorID = model.AdministradorID,
                            Nombre = model.Nombre,
                            FechaNacimiento = model.FechaNacimiento,
                            Correo = model.Correo,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash),
                            EmailConfirmationToken = Guid.NewGuid().ToString(),
                            EmailConfirmed = false
                        };
                        break;
                    case "Proveedor":
                        entidad = new Proveedores
                        {
                            ProveedorID = model.ProveedorID,
                            Nombre = model.Nombre,
                            FechaNacimiento = model.FechaNacimiento,
                            Correo = model.Correo,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash),
                            EmailConfirmationToken = Guid.NewGuid().ToString(),
                            EmailConfirmed = false,
                            Telefono = model.Telefono,
                            Direccion = model.Direccion
                        };
                        break;
                    default:
                        TempData["Error"] = "Tipo de registro no válido.";
                        return View(model);
                }

                // Guardar la entidad en la base de datos
                string mensaje;
                bool resultado = RegistrarUsuarioEnBaseDeDatos(entidad, out mensaje);

                if (!resultado)
                {
                    TempData["Error"] = mensaje;
                    return View(model);
                }

                TempData["Mensaje"] = "Registro exitoso.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al registrar: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> OlvidoContraseña(OlvidoContraseñaViewModel model)
        {          //Recuperación de contraseña
            try
            {          
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    await oConexion.OpenAsync();

                    // Buscar el correo en las tres tablas
                    string query = @"
                SELECT 'Usuario' AS Tipo, UsuarioID AS ID, Correo FROM Usuarios WHERE Correo = @Correo
                UNION
                SELECT 'Administrador' AS Tipo, AdministradorID AS ID, Correo FROM Administrador WHERE Correo = @Correo
                UNION
                SELECT 'Proveedor' AS Tipo, ProveedorID AS ID, Correo FROM Proveedores WHERE Correo = @Correo";

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@Correo", model.Correo);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                TempData["Error"] = "No se encontró un usuario, administrador o proveedor con ese correo electrónico.";
                                return View(model);
                            }

                            // Leer los datos del registro encontrado
                            await reader.ReadAsync();
                            string tipoEntidad = reader["Tipo"].ToString();
                            string id = reader["ID"].ToString();
                            string correoDB = reader["Correo"].ToString();

                            // Cerrar el lector antes de ejecutar otra consulta
                            reader.Close();

                            // Generar un nuevo token de restablecimiento
                            string passwordResetToken = Guid.NewGuid().ToString();

                            // Actualizar el token en la tabla correspondiente
                            string updateQuery = tipoEntidad switch
                            {
                                "Usuario" => "UPDATE Usuarios SET PasswordResetToken = @PasswordResetToken WHERE UsuarioID = @ID",
                                "Administrador" => "UPDATE Administrador SET PasswordResetToken = @PasswordResetToken WHERE AdministradorID = @ID",
                                "Proveedor" => "UPDATE Proveedores SET PasswordResetToken = @PasswordResetToken WHERE ProveedorID = @ID",
                                _ => throw new Exception("Tipo de entidad no válido.")
                            };

                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, oConexion))
                            {
                                updateCmd.Parameters.AddWithValue("@PasswordResetToken", passwordResetToken);
                                updateCmd.Parameters.AddWithValue("@ID", id);

                                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                                if (rowsAffected <= 0)
                                {
                                    TempData["Error"] = "Error al actualizar el token de restablecimiento.";
                                    return View(model);
                                }
                            }

                            // Generar el enlace de restablecimiento
                            string resetLink = Url.Action(
                                "RestablecerContraseña",
                                "Acceso",
                                new { token = passwordResetToken, tipo = tipoEntidad },
                                protocol: Request.Scheme
                            );

                            // Enviar correo con el enlace de restablecimiento
                            await emailService.SendEmailAsync(
                                correoDB,
                                "Restablecimiento de Contraseña",
                                $"Haz clic en el siguiente enlace para restablecer tu contraseña: {resetLink}"
                            );

                            TempData["Mensaje"] = "Se ha enviado un correo con las instrucciones para restablecer tu contraseña.";
                            return RedirectToAction("SolicitudExitosa");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la solicitud: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult RestablecerContraseña(string token, string tipo)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tipo))
            {
                TempData["Error"] = "El enlace de restablecimiento no es válido.";
                return RedirectToAction("Index", "Acceso");
            }

            var model = new RestablecerContraseñaViewModel
            {
                PasswordResetToken = token,
                TipoUsuario = tipo
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RestablecerContraseña(RestablecerContraseñaViewModel model)
        {                       
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (model.NewPasswordHash != model.ConfirmNewPasswordHash)
                {
                    ModelState.AddModelError("", "Las contraseñas no coinciden.");
                    return View(model);
                }

                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    await oConexion.OpenAsync();

                    string tabla = model.TipoUsuario switch
                    {
                        "Usuario" => "Usuarios",
                        "Administrador" => "Administrador",
                        "Proveedor" => "Proveedores",
                        _ => throw new Exception("Tipo de entidad no válido.")
                    };

                    // Verificar si el token es válido
                    string query = $"SELECT PasswordHash FROM {tabla} WHERE PasswordResetToken = @Token";
                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@Token", model.PasswordResetToken);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                ModelState.AddModelError("", "El token no es válido o ha expirado.");
                                return View(model);
                            }

                            await reader.ReadAsync();
                            string currentPasswordHash = reader["PasswordHash"].ToString();
                            reader.Close();

                            // Verificar si la nueva contraseña es igual a la anterior
                            if (BCrypt.Net.BCrypt.Verify(model.NewPasswordHash, currentPasswordHash))
                            {
                                ModelState.AddModelError("", "La nueva contraseña no puede ser la misma que la anterior.");
                                return View(model);
                            }

                            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPasswordHash);

                            // Actualizar la contraseña
                            string updateQuery = $@"
                        UPDATE {tabla} 
                        SET PasswordHash = @PasswordHash, PasswordResetToken = NULL 
                        WHERE PasswordResetToken = @Token";

                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, oConexion))
                            {
                                updateCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                                updateCmd.Parameters.AddWithValue("@Token", model.PasswordResetToken);

                                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                                if (rowsAffected <= 0)
                                {
                                    ModelState.AddModelError("", "Error al actualizar la contraseña.");
                                    return View(model);
                                }
                            }

                            TempData["Mensaje"] = "Contraseña restablecida exitosamente.";
                            return RedirectToAction("Index", "Acceso");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al procesar la solicitud: {ex.Message}");
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()    // Cierra la sesión
        {
            await HttpContext.SignOutAsync("CustomCookieAuth");

            // Redirigir al inicio de sesión o a la página principal
            return RedirectToAction("Index", "Acceso");
        }

    }
}
