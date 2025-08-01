using ChattingApplicationProject.Services;

namespace ChattingApplicationProject.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        public string? UserName { get; set; }

        public byte[]? PasswordHash { get; set; }
        public byte[]? PasswordSalt { get; set; }

        public string? Role { get; set; }

        public DateTime DateOfBirth { get; set; }
        public string? KnownAs { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastActive { get; set; } = DateTime.Now;
        public string? Gender { get; set; }
        public string? Introduction { get; set; }
        public string? LookingFor { get; set; }
        public string? Interests { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public ICollection<Photo>? Photos { get; set; }

        // Likes relationships
        public ICollection<UserLike>? LikedByUsers { get; set; } // Users who liked this user
        public ICollection<UserLike>? LikedUsers { get; set; } // Users this user has liked

        // Messages relationships
        public ICollection<Message>? MessagesSent { get; set; }
        public ICollection<Message>? MessagesReceived { get; set; }

        public int GetAge()
        {
            return new GetAgeService().CalculateAge(DateOfBirth);
        }
    }
}
