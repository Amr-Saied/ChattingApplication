using System;
using System.Threading.Tasks;
using ChattingApplicationProject.Data;
using Microsoft.EntityFrameworkCore;

namespace ChattingApplicationProject.Services
{
    public class LikesService : ILikeService
    {
        private readonly DataContext _context;

        public LikesService(DataContext context)
        {
            _context = context;
        }

        public async Task<bool> AddLike(int sourceUserId, int likedUserId)
        {
            // Check if like already exists
            var existingLike = await _context.UserLikes.FirstOrDefaultAsync(x =>
                x.SourceUserId == sourceUserId && x.LikedUserId == likedUserId
            );

            if (existingLike != null)
                return false; // Like already exists

            // Check if users exist
            var sourceUser = await _context.Users.FindAsync(sourceUserId);
            var likedUser = await _context.Users.FindAsync(likedUserId);

            if (sourceUser == null || likedUser == null)
                return false;

            // Create new like
            var userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUserId,
                Created = DateTime.Now
            };

            _context.UserLikes.Add(userLike);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> RemoveLike(int sourceUserId, int likedUserId)
        {
            var like = await _context.UserLikes.FirstOrDefaultAsync(x =>
                x.SourceUserId == sourceUserId && x.LikedUserId == likedUserId
            );

            if (like == null)
                return false; // Like doesn't exist

            _context.UserLikes.Remove(like);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> HasUserLiked(int sourceUserId, int likedUserId)
        {
            return await _context.UserLikes.AnyAsync(x =>
                x.SourceUserId == sourceUserId && x.LikedUserId == likedUserId
            );
        }

        public async Task<int> GetUserLikedByCount(int userId)
        {
            return await _context.UserLikes.CountAsync(x => x.LikedUserId == userId);
        }
    }
}
