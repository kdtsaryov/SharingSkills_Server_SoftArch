using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;

namespace SharingSkills_HSE_backend.Other
{
    /// <summary>
    /// Класс работы с почтой
    /// </summary>
    public class Mail
    {
        /// <summary>
        /// Отправка письма на почту
        /// </summary>
        /// <param name="email">Почта получателя</param>
        /// <param name="subject">Тема письма</param>
        /// <param name="message">Сообщение</param>
        /// <returns></returns>
        public static async Task SendEmailAsync(string email, string subject, string message)
        {
            // Создание самого письма
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Обмен навыками", "sharing-skills@sharing-skills.xyz"));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("Plain") { Text = message };

            // Подключение к почтовому сервису, с которого будет отправляться почта
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.yandex.ru", 465, true);
                await client.AuthenticateAsync("sharing-skills@sharing-skills.xyz", "admin1337");
                // Отправка письма
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}
