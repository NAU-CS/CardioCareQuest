using UnityEngine;
using Utility;

namespace Player
{
    public static class ClientPrefs
    {
        private const string MINI_GAME_DISTRIBUTION_KEY = "AssignedMiniGame";
        private const string USER_ID_KEY = "UserId";
        private const string AVAILABLE_PROFILES_KEY = "AvailableProfiles";
        private const string CONSENT_SIGNED_KEY = "ConsentSigned";
        private const string IS_USER_STUDY_KEY = "IsUserStudy";
        
        public static int GetMiniGame()
        {
            return PlayerPrefs.GetInt(MINI_GAME_DISTRIBUTION_KEY, 0);
        }
        
        public static void SetMiniGame(int value)
        {
            PlayerPrefs.SetInt(MINI_GAME_DISTRIBUTION_KEY, value);
        }
        
        public static string GetUserId()
        {
            return GetString(USER_ID_KEY, "editor");
        }
        
        public static void SetUserId(string userId)
        {
            SetString(USER_ID_KEY, userId);
        }
        
        public static string GetAvailableProfiles()
        {
            // returns a comma seperated string of Guids of all the profiles
            return GetString(AVAILABLE_PROFILES_KEY, null);
        }
        public static void SetAvailableProfiles(string profiles)
        {
            SetString(AVAILABLE_PROFILES_KEY, profiles);
        }

        public static string GetString(string key, string fallback = default)
        {
            return PlayerPrefs.GetString(key, fallback);
        }
        
        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public static bool GetBool(string key, bool fallback = default)
        {
            return PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
        }
        
        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public static void DeleteString(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public static bool ConsentSigned()
        {
            return PlayerPrefs.GetInt(CONSENT_SIGNED_KEY, 0) == 1;
        }

        public static void SetConsentSigned()
        {
            PlayerPrefs.SetInt(CONSENT_SIGNED_KEY, 1);
        }
        
        public static bool IsUserStudy()
        {
// #if !UNITY_EDITOR
//             return GetBool(IS_USER_STUDY_KEY, false);
// #else
//             return false;
// #endif
            return false;
        }
        
        public static void SetUserStudy(bool value)
        {
            SetBool(IS_USER_STUDY_KEY, value);
        }
    }
}