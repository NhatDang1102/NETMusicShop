using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Service.Helpers
{
    public class MailSender
    {
        private readonly SmtpSettings _smtp;

        public MailSender(IOptions<SmtpSettings> smtpOptions)
        {
            _smtp = smtpOptions.Value;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var mail = new MailMessage(_smtp.FromEmail, toEmail)
            {
                Subject = "Xác nhận đăng ký tài khoản - Music Shop",
                Body = $"Mã xác nhận của bạn là: {otp}\nOTP sẽ hết hạn trong 5 phút.",
                IsBodyHtml = false
            };

            using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtp.FromEmail, _smtp.AppPassword)
            };

            await smtpClient.SendMailAsync(mail);
        }
    }
}
