using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository.DTOs;
using Repository.Interfaces;
using Repository.Models;
using Service.Helpers;
using Service.Interfaces;

namespace Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly MailSender _mailSender;
        private readonly MusicShopDBContext _context;
        public AuthService(IAuthRepository authRepository, MailSender mailSender, MusicShopDBContext context)
        {
            _authRepository = authRepository;
            _mailSender = mailSender;
            _context = context;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            if (await _authRepository.EmailExistsAsync(dto.Email))
                return "Email đã tồn tại.";

            var otp = new Random().Next(100000, 999999).ToString();

            var tempUser = new TempUser
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password), 
                Name = dto.Name,
                Phone = dto.Phone,
                Address = dto.Address,
                Role = dto.Role ?? "customer",
                OtpCode = otp,
                OtpExpiresAt = DateTime.Now.AddMinutes(5),
                CreatedAt = DateTime.Now
            };

            await _authRepository.AddTempUserAsync(tempUser);

            await _mailSender.SendOtpEmailAsync(dto.Email, otp);
            Console.WriteLine($"[DEBUG] OTP gửi đến email {dto.Email}: {tempUser.OtpCode}");

            return "vui long xac minh otp.";
        }

        public async Task<string> VerifyOtpAsync(OtpVerifyDto dto)
        {
            var temp = await _authRepository.GetTempUserByEmailAsync(dto.Email);
            if (temp == null) return "Email không tồn tại hoặc đã xác minh.";
            if (temp.OtpCode != dto.OtpCode) return "Mã OTP sai.";
            if (temp.OtpExpiresAt < DateTime.Now) return "ma otp heest han.";

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = temp.Email,
                Password = temp.Password,
                Name = temp.Name,
                Role = temp.Role,
                Status = "active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _authRepository.DeleteTempUserAsync(dto.Email);

            return "xac thuc thanh cong. tk da tao.";
        }
    }
}
