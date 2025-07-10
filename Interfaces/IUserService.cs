using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattingApplicationProject.Models;

namespace ChattingApplicationProject.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<AppUser>> GetUsers();
        Task<AppUser> GetUserById(int id);
        Task<bool> UserExists(string username);
        Task<AppUser> AddUser(AppUser user);
        Task<AppUser> GetUserByUsername(string username);
    }
}
