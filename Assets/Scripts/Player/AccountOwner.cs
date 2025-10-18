using System.Collections.Generic;

namespace Player
{
    public interface IAccountType
    {
    }

    public class AccountOwner
    {
        public string Id { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string ZipCode { get; set; }
        public string CityState { get; set; }

        public string Gender { get; set; }
        public List<string> Race { get; set; }

        public string Description { get; set; }
        public string KnowAboutSpokeIt { get; set; }
        public string Feedback { get; set; }

        public IAccountType Type { get; set; }
    }
}