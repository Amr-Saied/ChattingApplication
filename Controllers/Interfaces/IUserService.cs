using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattingApplicationProject.Models;

namespace ChattingApplicationProject.Controllers.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<AppUser>> GetUsers();
        Task<AppUser> GetUserById(int id);
    }
}
