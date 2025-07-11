using ChattingApplicationProject.Models;

namespace ChattingApplicationProject.Interfaces
{
    public interface ITokenService
    {
        public string CreateToken(AppUser user);
    }
}
