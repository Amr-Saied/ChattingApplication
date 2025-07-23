using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ChattingApplicationProject.Data;
using ChattingApplicationProject.DTO;
using ChattingApplicationProject.Interfaces;
using ChattingApplicationProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ChattingApplicationProject.Services
{
    public class UserService : IUserService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public UserService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AppUser>> GetUsers()
        {
            return await _context.Users.Include(u => u.Photos).ToListAsync();
        }

        public async Task<AppUser> GetUserById(int id)
        {
            return await _context.Users.Include(u => u.Photos).FirstOrDefaultAsync(x => x.Id == id);
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
            return await _context
                .Users.Include(u => u.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username.ToLower());
        }

        // New DTO methods
        public async Task<IEnumerable<MemeberDTO>> GetUsersDTO()
        {
            var users = await _context.Users.Include(u => u.Photos).ToListAsync();
            return _mapper.Map<IEnumerable<MemeberDTO>>(users);
        }

        public async Task<MemeberDTO> GetUserByIdDTO(int id)
        {
            var user = await _context
                .Users.Include(u => u.Photos)
                .FirstOrDefaultAsync(x => x.Id == id);
            return _mapper.Map<MemeberDTO>(user);
        }

        public async Task<MemeberDTO> GetUserByUsernameDTO(string username)
        {
            var user = await _context
                .Users.Include(u => u.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username.ToLower());
            return _mapper.Map<MemeberDTO>(user);
        }
    }
}
