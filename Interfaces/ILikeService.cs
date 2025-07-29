using System.Threading.Tasks;

namespace ChattingApplicationProject.Interfaces
{
    public interface ILikeService
    {
        // Like/Unlike operations
        Task<bool> AddLike(int sourceUserId, int likedUserId);
        Task<bool> RemoveLike(int sourceUserId, int likedUserId);
        Task<bool> HasUserLiked(int sourceUserId, int likedUserId);

        // Get like count for a user
        Task<int> GetUserLikedByCount(int userId);
    }
}
