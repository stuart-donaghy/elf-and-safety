using ElfAndSafety.Models;

namespace ElfAndSafety.Services
{
    public class UserService : IUserService
    {
        private readonly List<User> _users = new();
        private int _nextId = 1;

        public UserService()
        {
            // Add some sample data
            SeedData();
        }

        public Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return Task.FromResult(_users.AsEnumerable());
        }

        public Task<IEnumerable<User>> GetUsersAsync(bool? showDeleted = null)
        {
            var query = _users.AsQueryable();
            
            if (showDeleted.HasValue)
            {
                query = query.Where(u => u.Deleted == showDeleted.Value);
            }

            return Task.FromResult(query.AsEnumerable());
        }

        public Task<User?> GetUserByIdAsync(int id)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }

        public Task<User> CreateUserAsync(User user)
        {
            user.Id = _nextId++;
            user.DateCreated = DateTime.UtcNow;
            user.DateLastModified = DateTime.UtcNow;
            user.Deleted = false;

            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task<User> UpdateUserAsync(User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser == null)
            {
                throw new ArgumentException("User not found");
            }

            existingUser.FirstName = user.FirstName;
            existingUser.Surname = user.Surname;
            existingUser.EmailAddress = user.EmailAddress;
            existingUser.Username = user.Username;
            existingUser.DateLastModified = DateTime.UtcNow;

            return Task.FromResult(existingUser);
        }

        public Task<bool> DeleteUserAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Task.FromResult(false);
            }

            user.Deleted = true;
            user.DateLastModified = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> RestoreUserAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Task.FromResult(false);
            }

            user.Deleted = false;
            user.DateLastModified = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        private void SeedData()
        {
            var sampleUsers = new List<User>
            {
                new User
                {
                    FirstName = "John",
                    Surname = "Doe",
                    EmailAddress = "john.doe@example.com",
                    Username = "johndoe",
                    Deleted = false,
                    DateCreated = DateTime.UtcNow.AddDays(-30),
                    DateLastModified = DateTime.UtcNow.AddDays(-5)
                },
                new User
                {
                    FirstName = "Jane",
                    Surname = "Smith",
                    EmailAddress = "jane.smith@example.com",
                    Username = "janesmith",
                    Deleted = false,
                    DateCreated = DateTime.UtcNow.AddDays(-25),
                    DateLastModified = DateTime.UtcNow.AddDays(-10)
                },
                new User
                {
                    FirstName = "Bob",
                    Surname = "Johnson",
                    EmailAddress = "bob.johnson@example.com",
                    Username = "bobjohnson",
                    Deleted = true,
                    DateCreated = DateTime.UtcNow.AddDays(-20),
                    DateLastModified = DateTime.UtcNow.AddDays(-2)
                }
            };

            foreach (var user in sampleUsers)
            {
                user.Id = _nextId++;
                _users.Add(user);
            }
        }
    }
}
