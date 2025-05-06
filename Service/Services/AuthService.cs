using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
        private readonly TikTokSettings _tiktokSettings;

        public AuthService(IAuthRepository authRepository, MailSender mailSender, MusicShopDBContext context, IOptions<JwtSettings> jwtOptions, IOptions<TikTokSettings> tiktokOptions)
        {
            _repo = authRepository;
            _mailSender = mailSender;
            _jwtSettings = jwtOptions.Value;
            _tiktokSettings = tiktokOptions.Value;
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
        public async Task<LoginResultDto> LoginWithTikTokAsync(TikTokLoginDto dto)
        {
            var client = new HttpClient();

            // 1. Đổi code lấy access_token
            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "client_key", _tiktokSettings.ClientKey },
        { "client_secret", _tiktokSettings.ClientSecret },
        { "code", dto.Code },
        { "grant_type", "authorization_code" },
        { "redirect_uri", _tiktokSettings.RedirectUri },
        { "code_verifier", dto.CodeVerifier }
    });

            var tokenResponse = await client.PostAsync("https://open.tiktokapis.com/v2/oauth/token/", tokenRequest);
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

            Console.WriteLine("[TikTok] Token JSON:\n" + tokenJson); // Log phản hồi TikTok

            var tokenResult = JsonSerializer.Deserialize<JsonElement>(tokenJson);

            if (!tokenResult.TryGetProperty("access_token", out var accessTokenElement))
            {
                var err = tokenResult.TryGetProperty("error_description", out var desc)
                    ? desc.GetString()
                    : "Không lấy được access_token từ TikTok.";
                throw new Exception($"[TikTok] Token Error: {err}");
            }

            var accessToken = accessTokenElement.GetString();

            // 2. Gửi request lấy user info
            var userInfoRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "https://open.tiktokapis.com/v2/user/info/?fields=open_id,email,display_name"
            );
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userResponse = await client.SendAsync(userInfoRequest);
            var userJsonStr = await userResponse.Content.ReadAsStringAsync();
            Console.WriteLine("[TikTok] User JSON:\n" + userJsonStr);

            var userJson = JsonSerializer.Deserialize<JsonElement>(userJsonStr);

            var userData = userJson.GetProperty("data").GetProperty("user");

            var email = userData.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            var name = userData.TryGetProperty("display_name", out var nameProp) ? nameProp.GetString() : "TikTok User";

            if (string.IsNullOrEmpty(email))
                throw new Exception("Tài khoản TikTok không cung cấp email.");

            // 3. Tạo user nếu chưa có
            var user = await _repo.GetUserByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    Name = name,
                    Role = "customer",
                    Status = "active",
                    CreatedAt = DateTime.Now
                };
                await _repo.CreateUserAsync(user);
            }

            // 4. Sinh JWT
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
