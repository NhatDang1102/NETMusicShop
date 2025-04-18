using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository.Models;

namespace Repository.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> EmailExistsAsync(string email);
        Task AddTempUserAsync(TempUser user);
        Task<TempUser> GetTempUserByEmailAsync(string email);
        Task DeleteTempUserAsync(string email);
        Task<User> GetUserByEmailAsync(string email);

    }
}
