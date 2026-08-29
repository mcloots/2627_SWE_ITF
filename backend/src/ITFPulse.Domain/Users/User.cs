using ITFPulse.Domain.Notifications;
using ITFPulse.Domain.Posts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Users
{
    internal class User
    {
        public Guid Id { get; private set; }
        public UserName UserName { get; private set; }
        public Email Email { get; private set; }
        public Profile? Profile { get; private set; }

        private readonly List<Post> _posts = [];
        public IReadOnlyCollection<Post> Posts => _posts.AsReadOnly();

        private readonly List<Notification> _notifications = [];
        public IReadOnlyCollection<Notification> Notifications => _notifications.AsReadOnly();

        public User(Guid id, UserName userName, Email email)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "User ID cannot be empty.",
                    nameof(id));
            }

            Id = id;
            UserName = userName
                ?? throw new ArgumentNullException(nameof(userName));

            Email = email
                ?? throw new ArgumentNullException(nameof(email));
        }

        public void CreateProfile(DisplayName displayName)
        {
            if (Profile is not null)
            {
                throw new InvalidOperationException(
                    "The user already has a profile.");
            }

            Profile = new Profile(displayName);
        }

        public void ChangeUserName(UserName userName)
        {
            UserName = userName
                ?? throw new ArgumentNullException(nameof(userName));
        }

        public void ChangeEmail(Email email)
        {
            Email = email
                ?? throw new ArgumentNullException(nameof(email));
        }

        public void AddPost(Post post)
        {
            ArgumentNullException.ThrowIfNull(post);
            _posts.Add(post);
        }

        public void AddNotification(Notification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            _notifications.Add(notification);
        }
    }
}
