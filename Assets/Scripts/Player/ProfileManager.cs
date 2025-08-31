using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;

namespace Player
{
    public class ProfileManager : SingletonPersistent<ProfileManager>
    {
        public Profile CurrentProfile { get; private set; }
        public List<Profile> AvailableProfiles = new List<Profile>();

        public event Action OnProfileChanged;

        protected override void Setup()
        {

            if (!PlayerPrefs.HasKey("IsUserStudy"))
            {
                ClientPrefs.SetUserStudy(true);
            }
            
            GetAllProfiles();
        }

        private void OnEnable()
        {
            OnProfileChanged += SaveProfiles;
        }

        private void OnDisable()
        {
            OnProfileChanged -= SaveProfiles;
        }

        public void AddProfile(Profile profile)
        {
            Debug.Log("Profile added: " + profile.FirstName);
            AvailableProfiles.Add(profile);

            SaveProfiles();
           

            OnProfileChanged?.Invoke();
        }

        public void DeleteProfile(Profile profile)
        {
            AvailableProfiles.Remove(profile);
            
            Debug.Log("Deleted profile: " + profile.FirstName);
            ClientPrefs.DeleteString(profile.Id);
            
            OnProfileChanged?.Invoke();
        }

        public void UpdateProfile(Profile profile)
        {
            // TODO: look into this
            var index = AvailableProfiles.FindIndex(p => p.Id == profile.Id);
            AvailableProfiles[index] = profile;

            OnProfileChanged?.Invoke();
        }

        private void GetAllProfiles()
        {
            // comma seperated profile id's
            var loadedProfiles = ClientPrefs.GetAvailableProfiles();

            foreach (var profileId in loadedProfiles.Split(","))
            {
                if (profileId == "")
                    continue;

                var str = ClientPrefs.GetString(profileId);
                if (str == "")
                    continue;
                var profile = Profile.FromString(profileId, str);
                AvailableProfiles.Add(profile);
            }
        }

        public void SetCurrentProfile(string id)
        {
            // fetch the profile from the given id and set it as current profile
            var profile = Profile.FromString(id, ClientPrefs.GetString(id));
            CurrentProfile = profile;
        }

        private void SaveProfiles()
        {
            if (AvailableProfiles.Count == 0)
                return;

            var profilesToSave = AvailableProfiles.Select(profile => profile.Id)
                .Aggregate((current, next) => current + "," + next);

            ClientPrefs.SetAvailableProfiles(profilesToSave);
            foreach (var profile in AvailableProfiles)
            {
                ClientPrefs.SetString(profile.Id, profile.ToString());
            }
        }
    }
}