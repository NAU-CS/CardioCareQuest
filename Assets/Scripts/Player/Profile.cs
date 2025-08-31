using System;
using System.Collections.Generic;
using System.Linq;

namespace Player
{
    public class Profile
    {
        public string Id = Guid.NewGuid().ToString();
        public string FirstName;
        public string LastName;
        public string Dob;
        public string Gender;
        public List<string> Race = new();
        public List<string> SpeechImpairments = new();

        public override string ToString()
        {
            return FirstName + "," + LastName + "," + Dob + "," + Gender + "," + Race + ","
                   + string.Join(",", SpeechImpairments);
        }

        public static Profile FromString(string id, string profileString)
        {
            if (profileString.Length == 0)
                return null;

            var parts = profileString.Split(',');
            var profile = new Profile
            {
                Id = id,
                FirstName = parts[0],
                LastName = parts[1],
                Dob = parts[2],
                Gender = parts[3],
                Race = { parts[4] }, // TODO: check this
                SpeechImpairments = { }
            };
            return profile;
        }
    }
}