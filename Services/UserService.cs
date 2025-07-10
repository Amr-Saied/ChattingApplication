using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattingApplicationProject.Data;
using ChattingApplicationProject.Interfaces;
using ChattingApplicationProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ChattingApplicationProject.Services
{
    public class UserService : IUserService
    {
        private readonly DataContext _context;

        public UserService(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AppUser>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<AppUser> GetUserById(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }

        public async Task<AppUser> AddUser(AppUser user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<AppUser> GetUserByUsername(string username)
        {
            return await _context.Users.SingleOrDefaultAsync(x => x.UserName == username.ToLower());
        }

    }
}
