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


        public async Task SendWelcomeEmailAsync(string toEmail, string name, string role, string phone, string address)
        {
            var body = $@"
        Xin chào {name},<br/><br/>
        Cảm ơn bạn đã đăng ký tại <b>Music Shop</b>!<br/><br/>
        <u>Thông tin tài khoản của bạn:</u><br/>
        📧 Email: {toEmail}<br/>
        📱 SĐT: {phone}<br/>
        🏠 Địa chỉ: {address}<br/>
        🧾 Vai trò: {role}<br/><br/>
        Chúc bạn có những trải nghiệm tuyệt vời cùng chúng tôi!<br/><br/>
        Trân trọng,<br/>Music Shop Team";

            var mail = new MailMessage(_smtp.FromEmail, toEmail)
            {
                Subject = " Chào mừng đến với NET Nhật Music Shop!",
                Body = body,
                IsBodyHtml = true
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
