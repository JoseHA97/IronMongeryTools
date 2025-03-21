
using System.Net;
using System.Net.Mail;

namespace IronMongeryTools.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var fromEmail = "james321@example.com";
                var fromPassword = "xxxx xxxx xxxx xxxx";

                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential(fromEmail, fromPassword);
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage(fromEmail, to, subject, body))
                    {
                        mailMessage.IsBodyHtml = true;
                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"SMTP Error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                throw;
            }
        }

    }
}