using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Users
{
    internal class Profile
    {
        public DisplayName DisplayName { get; private set; }

        public Bio? Bio { get; private set; }

        public ProfilePictureUrl? ProfilePictureUrl { get; private set; }

        public Profile(DisplayName displayName)
        {
            DisplayName = displayName
                ?? throw new ArgumentNullException(nameof(displayName));
        }

        public void ChangeDisplayName(DisplayName displayName)
        {
            DisplayName = displayName
                ?? throw new ArgumentNullException(nameof(displayName));
        }

        public void ChangeBio(Bio bio)
        {
            Bio = bio ?? throw new ArgumentNullException(nameof(bio));
        }

        public void RemoveBio()
        {
            Bio = null;
        }

        public void ChangeProfilePicture(ProfilePictureUrl profilePictureUrl)
        {
            ProfilePictureUrl = profilePictureUrl
                ?? throw new ArgumentNullException(nameof(profilePictureUrl));
        }

        public void RemoveProfilePicture()
        {
            ProfilePictureUrl = null;
        }
    }
}
