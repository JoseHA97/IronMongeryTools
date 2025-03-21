

namespace IronMongeryTools.Services
{
    public interface IAccesoService
    {
        Task<string> GenerarEnlaceConfirmacionCorreo(IAcceso usuario, HttpRequest request);
        Task EnviarCorreoConfirmacion(string correo, string enlace);

        bool ConfirmarCorreo(string token, string email);
    }

    
}