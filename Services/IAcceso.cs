using System.Threading.Tasks;
using IronMongeryTools.Models;
using Microsoft.AspNetCore.Http;

namespace IronMongeryTools.Services
{
    public interface IAcceso
    {
        string Correo { get; set; }
        string EmailConfirmationToken { get; set; }

    }


}