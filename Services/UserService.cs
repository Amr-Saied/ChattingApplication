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

        public async Task<MemeberDTO> UpdateUserDTO(int id, MemeberDTO user)
        {
            var userToUpdate = await _context.Users.FindAsync(id);
            _mapper.Map(user, userToUpdate);
            // If PhotoUrl is set, update the main photo in Photos
            if (!string.IsNullOrEmpty(user.PhotoUrl) && userToUpdate.Photos != null)
            {
                var mainPhoto = userToUpdate.Photos.FirstOrDefault(p => p.IsMain);
                if (mainPhoto != null)
                {
                    mainPhoto.Url = user.PhotoUrl;
                }
                else if (userToUpdate.Photos.Count > 0)
                {
                    // If no main photo, set the first as main and update its URL
                    var firstPhoto = userToUpdate.Photos.First();
                    firstPhoto.Url = user.PhotoUrl;
                    firstPhoto.IsMain = true;
                }
                else
                {
                    // If no photos, add a new main photo
                    userToUpdate.Photos = new List<Photo>
                    {
                        new Photo { Url = user.PhotoUrl, IsMain = true }
                    };
                }
            }
            await _context.SaveChangesAsync();
            return _mapper.Map<MemeberDTO>(userToUpdate);
        }

        public async Task<bool> AddPhotoToGallery(int userId, PhotoDTO photo)
        {
            var user = await _context
                .Users.Include(u => u.Photos)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;
            var newPhoto = new Photo { Url = photo.Url, IsMain = false };
            user.Photos ??= new List<Photo>();
            user.Photos.Add(newPhoto);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePhotoFromGallery(int userId, int photoId)
        {
            var user = await _context
                .Users.Include(u => u.Photos)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.Photos == null)
                return false;
            var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);
            if (photo == null)
                return false;
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
