using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Repository.DTOs;
using Repository.Interfaces;
using Repository.Models;
using Repository.Repositories;
using Service.DTOs;
using Service.Helpers;
using Service.Interfaces;

namespace Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly MailSender _mailSender;
        private readonly MusicShopDBContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IAuthRepository authRepository, MailSender mailSender, MusicShopDBContext context, IOptions<JwtSettings> jwtOptions)
        {
            _repo = authRepository;
            _mailSender = mailSender;
            _jwtSettings = jwtOptions.Value;

            _context = context;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            if (await _repo.EmailExistsAsync(dto.Email))
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

            await _repo.AddTempUserAsync(tempUser);

            await _mailSender.SendOtpEmailAsync(dto.Email, otp);
            Console.WriteLine($"[DEBUG] OTP gửi đến email {dto.Email}: {tempUser.OtpCode}");

            return "vui long xac minh otp.";
        }

        public async Task<string> VerifyOtpAsync(OtpVerifyDto dto)
        {
            var temp = await _repo.GetTempUserByEmailAsync(dto.Email);
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

            await _repo.DeleteTempUserAsync(dto.Email);
            await _mailSender.SendWelcomeEmailAsync(
         temp.Email,
         temp.Name,
         temp.Role,
         temp.Phone,
         temp.Address
);


            return "xac thuc thanh cong. tk da tao.";
        }

        public async Task<LoginResultDto> LoginAsync(LoginDto dto)
        {
            var user = await _repo.GetUserByEmailAsync(dto.Email);

            if (user == null || user.Status != "active")
                throw new Exception("Tài khoản không tồn tại hoặc bị khóa.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                throw new Exception("Mật khẩu sai");

            var claims = new[]
            {
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Name ?? ""),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new LoginResultDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Role = user.Role,
                Name = user.Name
            };
        }


public async Task<LoginResultDto> FirebaseLoginAsync(FirebaseLoginDto dto)
    {
        var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(dto.IdToken);
        var email = decodedToken.Claims["email"].ToString();

        var user = await _repo.GetUserByEmailAsync(email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = decodedToken.Claims.ContainsKey("name") ? decodedToken.Claims["name"].ToString() : "Unknown",
                Role = "customer",
                Status = "active",
                CreatedAt = DateTime.Now
            };

            await _repo.CreateUserAsync(user);
        }

        var claims = new[]
        {
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Role, user.Role)
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddHours(2), signingCredentials: creds);

        return new LoginResultDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Role = user.Role,
            Name = user.Name
        };
    }

}
}
