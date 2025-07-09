using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattingApplicationProject.Controllers.Interfaces;
using ChattingApplicationProject.Data;
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
    }
}
