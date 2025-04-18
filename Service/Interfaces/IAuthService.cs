using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository.DTOs;
using Repository.Models;
using Service.DTOs;

namespace Service.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> VerifyOtpAsync(OtpVerifyDto dto);
        Task<LoginResultDto> LoginAsync(LoginDto dto);

    }
}
