using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;
using Repository.Models;

namespace Repository.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly MusicShopDBContext _context;
        public AuthRepository(MusicShopDBContext context)
        {
            _context = context;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email) || await _context.TempUsers.AnyAsync(u => u.Email == email);
        }

        public async Task AddTempUserAsync(TempUser user)
        {
            await _context.TempUsers.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<TempUser> GetTempUserByEmailAsync(string email)
        {
            return await _context.TempUsers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task DeleteTempUserAsync(string email)
        {
            var user = await GetTempUserByEmailAsync(email);
            if (user != null)
            {
                _context.TempUsers.Remove(user);
                await _context.SaveChangesAsync();
            }

        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

    }
}