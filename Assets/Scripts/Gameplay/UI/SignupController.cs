using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Player;
using UnityEngine;
using UnityEngine.UIElements;
using Utility;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

namespace Gameplay.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SignupController : Singleton<SignupController>
    {
        #region Private UI Elements

        private VisualElement _container;
        private VisualElement _basicInfoForm, _genderRaceInfoForm, _appUsageForm, _numberOfPlayerForm,
            _preSurveyQuestionsForm, _termsAndConditionsForm;

        private Button _backButton, _nextButton, _acceptButton;

        private VisualElement[] _pages;
        private readonly CallbackValue<int> _currentPage = new();

        private TextField _firstNameText, _lastNameText, _zipCodeText, _stateText, _cityText;
        private DropdownField _genderDropdown;
        private TextField _genderText;
        private MultiSelectDropdown _raceDropdown;
        private TextField _raceText;

        private DropdownField _appUsageDropdown;
        private TextField _appName;

        private DropdownField _numberOfPlayersDropdown;
        private TextField _numberOfPlayersText;

        private VisualElement _form1, _form2, _form3, _form4, _form5, _form6, _form7, _form8;
        
        private ProgressBar _progressBar;

        #endregion

        private bool _firstPaint = true;
        private bool _hasShownPopupOnce;
        private int _lastPopupPage = -1;
        public AccountOwner AccountOwner { get; } = new();

        private const string PreferNotToSay = "Prefer Not to Say";
        private const string OtherOption = "Other (specify)";

        private static readonly string[] Genders =
            { "Female", "Male", "Transgender Man", "Transgender Woman", "Fluid or Non-Binary", PreferNotToSay, OtherOption };

        private static readonly string[] Races =
            { "White", "Black or African American", "American Indian or Alaska Native", "Asian",
              "Native Hawaiian or Other Pacific Islander", "Hispanic or Latino", "Multi-Racial", PreferNotToSay, OtherOption };

        private static readonly string[] NumberOfPlayers = { "Single player", "Multiplayer" };
        private static readonly string[] AppUsage = { "Yes", "No" };
        private static readonly string[] PreSurvey =
            { "Strongly disagree", "Somewhat disagree", "Neither agree nor disagree", "Somewhat agree", "Strongly agree" };

        // ✅ New field to store unique signup key
        private string _signupRecordKey;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _container = root.Q<VisualElement>("container");
            if (_firstPaint)
            {
                _container.style.display = DisplayStyle.None;
                _firstPaint = false;
            }

            // Pages
            _basicInfoForm = root.Q<VisualElement>("basic-info");
            _genderRaceInfoForm = root.Q<VisualElement>("gender-race-info");
            _appUsageForm = root.Q<VisualElement>("app-usage-info");
            _numberOfPlayerForm = root.Q<VisualElement>("number-of-players-info");
            _preSurveyQuestionsForm = root.Q<VisualElement>("pre-survey-questions");
            _termsAndConditionsForm = root.Q<VisualElement>("terms-and-cond-form");

            _form1 = _preSurveyQuestionsForm.Q<VisualElement>("form-1");
            _form2 = _preSurveyQuestionsForm.Q<VisualElement>("form-2");
            _form3 = _preSurveyQuestionsForm.Q<VisualElement>("form-3");
            _form4 = _preSurveyQuestionsForm.Q<VisualElement>("form-4");
            _form5 = _preSurveyQuestionsForm.Q<VisualElement>("form-5");
            _form6 = _preSurveyQuestionsForm.Q<VisualElement>("form-6");
            _form7 = _preSurveyQuestionsForm.Q<VisualElement>("form-7");
            _form8 = _preSurveyQuestionsForm.Q<VisualElement>("form-8");
            
            _progressBar = _container.Q<ProgressBar>("progress-bar");

            _pages = new[]
            {
                _basicInfoForm,_genderRaceInfoForm,_numberOfPlayerForm,_appUsageForm,
                _form1,_form2,_form3,_form4,_form5,_form6,_form7,_form8,_termsAndConditionsForm
            };

            // Basic Info
            _firstNameText = _basicInfoForm.Q<TextField>("first-name");
            _lastNameText = _basicInfoForm.Q<TextField>("last-name");
            _zipCodeText = _basicInfoForm.Q<TextField>("zip-code");
            _stateText = _basicInfoForm.Q<TextField>("state");
            _cityText = _basicInfoForm.Q<TextField>("city");

            // Gender/Race
            _genderDropdown = _genderRaceInfoForm.Q<DropdownField>("gender");
            _genderText = _genderRaceInfoForm.Q<TextField>("gender-others");
            _raceText = _genderRaceInfoForm.Q<TextField>("race-others");
            _raceDropdown = new MultiSelectDropdown(Races, "Race *", _raceText);
            _genderRaceInfoForm.Insert(2, _raceDropdown);

            // App Info
            _numberOfPlayersDropdown = _numberOfPlayerForm.Q<DropdownField>("single-multiplayer");
            _numberOfPlayersText = _numberOfPlayerForm.Q<TextField>("number-of-players");
            _appUsageDropdown = _appUsageForm.Q<DropdownField>("appUsage");
            _appName = _appUsageForm.Q<TextField>("app-name");

            // Buttons
            _backButton = root.Q<Button>("back");
            _nextButton = root.Q<Button>("next");
            _acceptButton = root.Q<Button>("accept");

            // Callbacks
            _backButton.clicked += OnBackButtonClicked;
            _nextButton.clicked += OnNextButtonClicked;
            _acceptButton.clicked += OnAcceptButtonClicked;
            _currentPage.Changed += OnCurrentPageChanged;

            _genderDropdown.RegisterValueChangedCallback(OnDropdownChanged);
            _numberOfPlayersDropdown.RegisterValueChangedCallback(OnDropdownChanged);
            _appUsageDropdown.RegisterValueChangedCallback(OnDropdownChanged);
        }

        private void Start()
        {
            foreach (var e in Genders) _genderDropdown.choices.Add(e);
            foreach (var e in NumberOfPlayers) _numberOfPlayersDropdown.choices.Add(e);
            foreach (var e in AppUsage) _appUsageDropdown.choices.Add(e);

            foreach (var p in _pages) p.style.display = DisplayStyle.None;
            _currentPage.ForceSet(0);
            Show();

            // ✅ Generate unique record key once per signup
            GenerateSignupRecordKey();
        }

        private void GenerateSignupRecordKey()
        {
            string uid = LoginManager.CurrentUserId;
            if (string.IsNullOrEmpty(uid))
            {
                Popup.Instance.Show("No logged-in user. Please log in first.");
                return;
            }

            var dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            _signupRecordKey = dbRef.Child("users").Child(uid).Child("general").Push().Key;
            Debug.Log("✅ Signup record key generated: " + _signupRecordKey);
        }

        public void Show(bool value = true) => _container.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;

        private void OnBackButtonClicked() => _currentPage.Value = Math.Max(_currentPage.Value - 1, 0);

        private void OnNextButtonClicked()
        {
            if (!ValidatePage()) return;

            SaveCurrentPageData(); // save incremental page

            if (_currentPage.Value == 3 && _appUsageDropdown.value == "No")
            {
                _currentPage.Value = _pages.Length - 1;
                return;
            }

            if (_currentPage.Value < _pages.Length - 1) _currentPage.Value++;
            else CompleteSignup(); // last page
        }

        private void OnCurrentPageChanged(int value)
        {
            if (value < 0 || value >= _pages.Length) return;
            
            _progressBar.value = _currentPage.Value / (float)_pages.Length * 100f;

            for (var i = 0; i < _pages.Length; i++)
                _pages[i].style.display = i == value ? DisplayStyle.Flex : DisplayStyle.None;

            if (value > 3 && value != _pages.Length - 1)
            {
                _pages[value].Query<Label>().ForEach(label => label.text = label.text.Replace("{appName}", _appName.value));
                _pages[value].Query<DropdownField>().ForEach(dropDown => { foreach (var e in PreSurvey) dropDown.choices.Add(e); });
            }

            _backButton.style.display = value == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _nextButton.style.display = value == _pages.Length - 1 ? DisplayStyle.None : DisplayStyle.Flex;
            _nextButton.text = "Next";
        }

        private void OnAcceptButtonClicked()
        {
            // Populate AccountOwner with full name and email before showing signature pad
            AccountOwner.FullName = $"{_firstNameText.value} {_lastNameText.value}";
            AccountOwner.Email = LoginManager.CurrentUserEmail;



            // Show the signature pad
            SignaturePad.Instance.Show();
        }


        private void OnDropdownChanged(ChangeEvent<string> evt)
        {
            var show = evt.newValue is OtherOption or "Yes" or "Multiplayer";
            var field = (evt.target as DropdownField)?.name switch
            {
                "gender" => _genderText,
                "single-multiplayer" => _numberOfPlayersText,
                "appUsage" => _appName,
                _ => null
            };
            if (field != null) field.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SaveCurrentPageData()
        {
            var pageData = new Dictionary<string, object>();
            int pageIndex = _currentPage.Value;

            switch (pageIndex)
            {
                case 0: // Basic Info
                    pageData["FirstName"] = _firstNameText.value;
                    pageData["LastName"] = _lastNameText.value;
                    pageData["ZipCode"] = _zipCodeText.value;
                    pageData["State"] = _stateText.value;
                    pageData["City"] = _cityText.value;
                    break;
                case 1: // Gender/Race
                    pageData["Gender"] = _genderDropdown.value == OtherOption ? _genderText.value : _genderDropdown.value;
                    pageData["Race"] = _raceDropdown.SelectedOptions.Contains(OtherOption)
                        ? _raceDropdown.SelectedOptions.Concat(new[] { _raceText.value }).ToList()
                        : _raceDropdown.SelectedOptions;
                    break;
                case 2: // Number of players
                    pageData["NumberOfPlayers"] = _numberOfPlayersDropdown.value;
                    pageData["NumberOfPlayersText"] = _numberOfPlayersText.value;
                    break;
                case 3: // App usage
                    pageData["AppUsage"] = _appUsageDropdown.value;
                    pageData["AppName"] = _appName.value;
                    break;
                case int n when (n >= 4 && n <= 11): // Pre-survey
                    var preSurveyDict = new Dictionary<string, object>();
                    var dropDowns = _pages[pageIndex].Query<DropdownField>().ToList();
                    for (int i = 0; i < dropDowns.Count; i++)
                    {
                        string questionText = _pages[pageIndex].Q<Label>("question-" + (i + 1))?.text ?? $"Question {i + 1}";
                        preSurveyDict[questionText] = dropDowns[i].value;
                    }
                    pageData["PreSurveyPage" + (pageIndex - 3)] = preSurveyDict;
                    break;
                case 12: // Terms & conditions
                    pageData["AcceptedTerms"] = SignaturePad.Instance.SigningComplete;
                    break;
            }

            pageData["Timestamp"] = DateTime.UtcNow.ToString("o"); // ISO 8601 timestamp

            SavePageDataToFirebase(pageData);
        }

        private void CompleteSignup()
        {
            var fullData = new Dictionary<string, object>
            {
                { "FirstName", _firstNameText.value },
                { "LastName", _lastNameText.value },
                { "ZipCode", _zipCodeText.value },
                { "State", _stateText.value },
                { "City", _cityText.value },
                { "Gender", _genderDropdown.value == OtherOption ? _genderText.value : _genderDropdown.value },
                { "Race", _raceDropdown.SelectedOptions.Contains(OtherOption)
                            ? _raceDropdown.SelectedOptions.Concat(new[] { _raceText.value }).ToList()
                            : _raceDropdown.SelectedOptions },
                { "NumberOfPlayers", _numberOfPlayersDropdown.value },
                { "NumberOfPlayersText", _numberOfPlayersText.value },
                { "AppUsage", _appUsageDropdown.value },
                { "AppName", _appName.value },
                { "AcceptedTerms", SignaturePad.Instance.SigningComplete },
                { "Timestamp", DateTime.UtcNow.ToString("o") }
            };

            for (int i = 4; i <= 11; i++)
            {
                var preSurveyDict = new Dictionary<string, object>();
                var dropDowns = _pages[i].Query<DropdownField>().ToList();
                for (int j = 0; j < dropDowns.Count; j++)
                {
                    string questionText = _pages[i].Q<Label>("question-" + (j + 1))?.text ?? $"Question {j + 1}";
                    preSurveyDict[questionText] = dropDowns[j].value;
                }
                fullData["PreSurveyPage" + (i - 3)] = preSurveyDict;
            }

            SavePageDataToFirebase(fullData);
            Popup.Instance.Show("Signup completed successfully!");
        }

        private void SavePageDataToFirebase(Dictionary<string, object> data)
        {
            string uid = LoginManager.CurrentUserId;
            if (string.IsNullOrEmpty(uid))
            {
                Popup.Instance.Show("No logged-in user. Please log in first.");
                return;
            }

            if (string.IsNullOrEmpty(_signupRecordKey))
            {
                Debug.LogError("Signup record key not initialized!");
                return;
            }

            var dbRef = FirebaseDatabase.DefaultInstance.RootReference;

            // ✅ Save everything under the single signup key
            dbRef.Child("users").Child(uid).Child("general")
                .Child(_signupRecordKey)
                .UpdateChildrenAsync(data)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                        Debug.Log("✅ Page data saved under single signup record.");
                    else
                        Debug.LogError("❌ Failed to save page data: " + task.Exception);
                });
        }

        #region Validation Methods
        private bool ValidatePage()
        {
            if (_currentPage.Value != _lastPopupPage)
                _hasShownPopupOnce = false;

            var result = _currentPage.Value switch
            {
                0 => ValidateBasicInfo(),
                1 => ValidateGenderRaceInfo(),
                2 => ValidateNumberOfPlayersInfo(),
                3 => ValidateAppUsageInfo(),
                >= 4 and <= 11 => ValidatePreSurveyInfo(),
                12 => ValidateTermsAndCondForm(),
                _ => false
            };

            // Update last popup page after validation
            _lastPopupPage = _currentPage.Value;

            return result;
        }

        private bool ValidateBasicInfo()
        {
            if (string.IsNullOrEmpty(_firstNameText.value) ||
                string.IsNullOrEmpty(_lastNameText.value) ||
                string.IsNullOrEmpty(_zipCodeText.value) ||
                string.IsNullOrEmpty(_stateText.value) ||
                string.IsNullOrEmpty(_cityText.value))
            {
                Popup.Instance.Show("Please fill all the required fields."); return false;
            }
            
            if(!Regex.IsMatch(_zipCodeText.value, @"^\d+(-\d+)?$"))
            {
                Popup.Instance.Show("Please enter a valid zip code."); return false;
            }
            return true;
        }

        private bool ValidateGenderRaceInfo()
        {
            if (string.IsNullOrEmpty(_genderDropdown.value) || _raceDropdown.SelectedOptions.Count == 0)
            {
                Popup.Instance.Show("Please fill all the required fields."); return false;
            }
            if (_genderDropdown.value == OtherOption && string.IsNullOrEmpty(_genderText.value))
            {
                Popup.Instance.Show("Please enter a gender."); return false;
            }
            if (_raceDropdown.SelectedOptions.Contains(OtherOption) && string.IsNullOrEmpty(_raceText.value))
            {
                Popup.Instance.Show("Please enter a race."); return false;
            }
            AccountOwner.Gender = _genderDropdown.value == OtherOption ? _genderText.value : _genderDropdown.value;
            AccountOwner.Race = _raceDropdown.SelectedOptions;
            if(_raceDropdown.SelectedOptions.Contains(OtherOption))
                AccountOwner.Race.Add(_raceText.value);
            return true;
        }

        private bool ValidateNumberOfPlayersInfo()
        {
            if (string.IsNullOrEmpty(_numberOfPlayersDropdown.value))
            {
                Popup.Instance.Show("Please fill all the required fields."); return false;
            }
            if (_numberOfPlayersDropdown.value == "Multiplayer" && string.IsNullOrEmpty(_numberOfPlayersText.value))
            {
                Popup.Instance.Show("Please enter the number of player who will participate."); return false;
            }
            return true;
        }

        private bool ValidateAppUsageInfo()
        {
            if (string.IsNullOrEmpty(_appUsageDropdown.value))
            {
                Popup.Instance.Show("Please fill all the required fields."); return false;
            }
            if (_appUsageDropdown.value == "Yes" && string.IsNullOrEmpty(_appName.value))
            {
                Popup.Instance.Show("Please enter the app name you use."); return false;
            }
            return true;
        }

        private bool ValidatePreSurveyInfo()
        {
            var dropDowns = _pages[_currentPage.Value].Query<DropdownField>().ToList();

            var hasEmpty = dropDowns.Any(d => string.IsNullOrEmpty(d.value));

            if (hasEmpty)
            {
                if (!_hasShownPopupOnce)
                {
                    Popup.Instance.Show("Please fill all the required fields.");
                    _hasShownPopupOnce = true;
                    return false;
                }
                
                return true;
            }
            
            _hasShownPopupOnce = false;
            return true;
        }

        private bool ValidateTermsAndCondForm()
        {
            if (!SignaturePad.Instance.SigningComplete)
            {
                Popup.Instance.Show("Please read the terms and conditions and accept it.", "Terms & Conditions"); return false;
            }
            return true;
        }
        #endregion
    }
}
