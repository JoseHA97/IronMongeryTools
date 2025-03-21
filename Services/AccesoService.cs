using Microsoft.Data.SqlClient;
using IronMongeryTools.Data;

namespace IronMongeryTools.Services
{
    public class AccesoService : IAccesoService
    {
        private readonly IEmailService _emailService;
        private readonly LinkGenerator _linkGenerator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _connectionString = Conexion.rutaConexion;

        public AccesoService(IEmailService emailService, LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
        {
            _emailService = emailService;
            _linkGenerator = linkGenerator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GenerarEnlaceConfirmacionCorreo(IAcceso usuario, HttpRequest request)
        {
            string confirmationLink = _linkGenerator.GetUriByAction(
                _httpContextAccessor.HttpContext,
                action: "ConfirmarCorreo",
                controller: "Usuario",
                values: new { token = usuario.EmailConfirmationToken, email = usuario.Correo },
                scheme: request.Scheme
            );

            return confirmationLink;
        }

        public async Task EnviarCorreoConfirmacion(string correo, string enlace)
        {
            string mensaje = $"Haz clic en el siguiente enlace para confirmar tu correo: {enlace}";
            await _emailService.SendEmailAsync(correo, "Confirmación de Correo", mensaje);
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
                    SELECT 'Usuario' AS Tipo
                    FROM Usuarios 
                    WHERE Correo = @Correo AND EmailConfirmationToken = @Token
                    UNION
                    SELECT 'Administrador' AS Tipo
                    FROM Administrador
                    WHERE Correo = @Correo AND EmailConfirmationToken = @Token
                    UNION
                    SELECT 'Proveedor' AS Tipo
                    FROM Proveedores
                    WHERE Correo = @Correo AND EmailConfirmationToken = @Token
                ) AS T";

                    string tipo = null;
                    using (SqlCommand cmdVerificar = new SqlCommand(queryVerificar, oConexion))
                    {
                        cmdVerificar.Parameters.AddWithValue("@Correo", email);
                        cmdVerificar.Parameters.AddWithValue("@Token", token);
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
                        cmdActualizar.Parameters.AddWithValue("@Correo", email);
                        cmdActualizar.Parameters.AddWithValue("@Token", token);

                        int rowsAffected = cmdActualizar.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception)
            {
                // Manejo de errores (puedes agregar log o más detalles aquí)
                return false;
            }
        }
    }
}