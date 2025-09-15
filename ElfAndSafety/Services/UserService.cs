using ElfAndSafety.Models;
using ElfAndSafety.Persistence;
using ElfAndSafety.Persistence.Cqrs;

namespace ElfAndSafety.Services
{
    // UserService acts as the read-model (in-memory cache) and issues commands to the repository (write-model).
    public class UserService : IUserService
    {
        private readonly List<User> _users = new();
        private readonly IUserRepository _repository;
        private readonly IEventBus _eventBus;

        public UserService(IUserRepository repository, IEventBus eventBus)
        {
            _repository = repository;
            _eventBus = eventBus;

            // Load initial read model from repository
            var all = _repository.GetUsersAsync(null).GetAwaiter().GetResult();
            _users.AddRange(all);

            // Subscribe to events to keep cache in sync
            _eventBus.Subscribe<UserChangedEvent>(HandleUserChangedEvent);
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
        // Commands - go to repository (write model). The read model updates when the repository publishes events.
        public async Task<User> CreateUserAsync(User user)
        {
            user.DateCreated = DateTime.UtcNow;
            user.DateLastModified = DateTime.UtcNow;
            user.Deleted = false;

            // Validate email format
            if (!IsValidEmail(user.EmailAddress))
            {
                throw new ArgumentException("Email address is not valid.");
            }

            // Duplicate checks (case-insensitive)
            var existing = (await _repository.GetUsersAsync(null)).ToList();
            if (existing.Any(u => string.Equals(u.EmailAddress, user.EmailAddress, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateUserException("A user with the same email address already exists.");
            }
            if (existing.Any(u => string.Equals(u.Username, user.Username, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateUserException("A user with the same username already exists.");
            }
            if (existing.Any(u => string.Equals(u.FirstName, user.FirstName, StringComparison.OrdinalIgnoreCase) && string.Equals(u.Surname, user.Surname, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateUserException("A user with the same first name and surname already exists.");
            }

            var created = await _repository.CreateUserAsync(user);
            return created;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.DateLastModified = DateTime.UtcNow;
            // Validate email format
            if (!IsValidEmail(user.EmailAddress))
            {
                throw new ArgumentException("Email address is not valid.");
            }

            // Duplicate checks (exclude current user id)
            var existing = (await _repository.GetUsersAsync(null)).ToList();
            if (existing.Any(u => u.Id != user.Id && string.Equals(u.EmailAddress, user.EmailAddress, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateUserException("A user with the same email address already exists.");
            }
            if (existing.Any(u => u.Id != user.Id && string.Equals(u.Username, user.Username, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateUserException("A user with the same username already exists.");
            }
            if (existing.Any(u => u.Id != user.Id && string.Equals(u.FirstName, user.FirstName, StringComparison.OrdinalIgnoreCase) && string.Equals(u.Surname, user.Surname, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateUserException("A user with the same first name and surname already exists.");
            }

            var updated = await _repository.UpdateUserAsync(user);
            return updated;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Practical RFC 5322 regex (simplified but covers most valid cases)
            // Note: fully strict RFC 5322 regexes are extremely large; this is a pragmatic validation.
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                // MailAddress allows some non-RFC addresses but is generally robust.
                // Additional regex to enforce common stricter rules:
                var pattern = @"^(?=.{1,254}$)(?=.{1,64}@)[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@" +
                              @"[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$";
                return System.Text.RegularExpressions.Regex.IsMatch(email, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var result = await _repository.DeleteUserAsync(id);
            return result;
        }

        public async Task<bool> RestoreUserAsync(int id)
        {
            var result = await _repository.RestoreUserAsync(id);
            return result;
        }

        private void HandleUserChangedEvent(UserChangedEvent ev)
        {
            if (ev == null) return;

            switch (ev.Type)
            {
                case UserEventType.Created:
                    if (ev.User != null)
                    {
                        // add if not exists
                        if (!_users.Any(u => u.Id == ev.User.Id))
                        {
                            _users.Add(ev.User);
                        }
                    }
                    break;
                case UserEventType.Updated:
                    if (ev.User != null)
                    {
                        var ex = _users.FirstOrDefault(u => u.Id == ev.User.Id);
                        if (ex != null)
                        {
                            ex.FirstName = ev.User.FirstName;
                            ex.Surname = ev.User.Surname;
                            ex.EmailAddress = ev.User.EmailAddress;
                            ex.Username = ev.User.Username;
                            ex.Deleted = ev.User.Deleted;
                            ex.DateLastModified = ev.User.DateLastModified;
                        }
                        else
                        {
                            _users.Add(ev.User);
                        }
                    }
                    break;
                case UserEventType.Deleted:
                    if (ev.UserId.HasValue)
                    {
                        var du = _users.FirstOrDefault(u => u.Id == ev.UserId.Value);
                        if (du != null)
                        {
                            du.Deleted = true;
                            du.DateLastModified = DateTime.UtcNow;
                        }
                    }
                    break;
                case UserEventType.Restored:
                    if (ev.UserId.HasValue)
                    {
                        var ru = _users.FirstOrDefault(u => u.Id == ev.UserId.Value);
                        if (ru != null)
                        {
                            ru.Deleted = false;
                            ru.DateLastModified = DateTime.UtcNow;
                        }
                    }
                    break;
            }
        }

        private void SeedData()
        {
            // No-op: seeding is handled by DB initializer. Retained for compatibility.
        }
    }
}
