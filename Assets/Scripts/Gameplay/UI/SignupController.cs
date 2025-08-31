using System;
using System.Text.RegularExpressions;
using Player;
using UnityEngine;
using UnityEngine.UIElements;
using Utility;

namespace Gameplay.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SignupController : Singleton<SignupController>
    {
        #region Private UI Elements

        private VisualElement _container;

        private VisualElement _basicInfoForm;
        private VisualElement _genderRaceInfoForm;
        private VisualElement _appUsageForm;
        private VisualElement _preSurveyQuestionsForm;
        private VisualElement _termsAndConditionsForm;

        private Button _backButton;
        private Button _nextButton;
        private Button _acceptButton;

        private VisualElement[] _pages;
        private readonly CallbackValue<int> _currentPage = new();

        private TextField _fullNameText;
        private TextField _emailText;
        private TextField _zipCodeText;
        private TextField _cityStateText;

        private DropdownField _genderDropdown;
        private TextField _genderText;
        private MultiSelectDropdown _raceDropdown;
        private TextField _raceText;

        private DropdownField _appUsageDropdown;
        private TextField _appName;
        
        private VisualElement _form1;
        private VisualElement _form2;
        private VisualElement _form3;
        private VisualElement _form4;
        private VisualElement _form5;
        private VisualElement _form6;
        private VisualElement _form7;
        private VisualElement _form8;
        
        #endregion
        
        private bool _firstPaint = true;

        public AccountOwner AccountOwner { get; } = new();

        private const string PreferNotToSay = "Prefer Not to Say";
        private const string OtherOption = "Other (specify)";

        #region Dropdown Values

        private static readonly string[] Genders =
        {
            "Female",
            "Male",
            "Transgender Man",
            "Transgender Woman",
            "Fluid or Non-Binary",
            PreferNotToSay,
            OtherOption
        };

        private static readonly string[] Races =
        {
            "White",
            "Black or African American",
            "American Indian or Alaska Native",
            "Asian",
            "Native Hawaiian or Other Pacific Islander",
            "Hispanic or Latino",
            "Multi-Racial",
            PreferNotToSay,
            OtherOption
        };
        
        private static readonly string[] AppUsage =
        {
            "Yes",
            "No"
        };

        private static readonly string[] PreSurvey =
        {
            "Strongly disagree",
            "Somewhat disagree",
            "Neither agree nor disagree",
            "Somewhat agree",
            "Strongly agree"
        };

        #endregion

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _container = root.Q<VisualElement>("container");
            if (_firstPaint)
            {
                _container.style.display = DisplayStyle.None;
                _firstPaint = false;
            }

            #region Form Pages

            _basicInfoForm = root.Q<VisualElement>("basic-info");
            _genderRaceInfoForm = root.Q<VisualElement>("gender-race-info");
            _appUsageForm = root.Q<VisualElement>("app-usage-info");
            _preSurveyQuestionsForm = root.Q<VisualElement>("pre-survey-questions");
            _termsAndConditionsForm = root.Q<VisualElement>("terms-and-cond-form");
            
            // pre-survey questions form
            _form1 = _preSurveyQuestionsForm.Q<VisualElement>("form-1");
            _form2 = _preSurveyQuestionsForm.Q<VisualElement>("form-2");
            _form3 = _preSurveyQuestionsForm.Q<VisualElement>("form-3");
            _form4 = _preSurveyQuestionsForm.Q<VisualElement>("form-4");
            _form5 = _preSurveyQuestionsForm.Q<VisualElement>("form-5");
            _form6 = _preSurveyQuestionsForm.Q<VisualElement>("form-6");
            _form7 = _preSurveyQuestionsForm.Q<VisualElement>("form-7");
            _form8 = _preSurveyQuestionsForm.Q<VisualElement>("form-8");

            _pages = new[]
            {
                _basicInfoForm,
                _genderRaceInfoForm,
                _appUsageForm,
                _form1,
                _form2,
                _form3,
                _form4,
                _form5,
                _form6,
                _form7,
                _form8,
                _termsAndConditionsForm
            };

            #endregion

            #region Basic Info Form

            _fullNameText = _basicInfoForm.Q<TextField>("fullname");
            _emailText = _basicInfoForm.Q<TextField>("email");
            _zipCodeText = _basicInfoForm.Q<TextField>("zip-code");
            _cityStateText = _basicInfoForm.Q<TextField>("city-state");

            #endregion

            #region Gender/Race Info Form

            _genderDropdown = _genderRaceInfoForm.Q<DropdownField>("gender");
            _genderText = _genderRaceInfoForm.Q<TextField>("gender-others");

            _raceText = _genderRaceInfoForm.Q<TextField>("race-others");
            _raceDropdown = new MultiSelectDropdown(Races, "Race *", _raceText);
            _genderRaceInfoForm.Insert(2, _raceDropdown);

            #endregion

            #region Personal Info Form

            _appUsageDropdown = _appUsageForm.Q<DropdownField>("appUsage");
            _appName = _appUsageForm.Q<TextField>("app-name");

            #endregion

            #region Buttons

            _backButton = root.Q<Button>("back");
            _nextButton = root.Q<Button>("next");
            _acceptButton = root.Q<Button>("accept");

            #endregion

            #region Register Callbacks

            _backButton.clicked += OnBackButtonClicked;
            _nextButton.clicked += OnNextButtonClicked;
            _acceptButton.clicked += OnAcceptButtonClicked;
            _currentPage.Changed += OnCurrentPageChanged;

            _genderDropdown.RegisterValueChangedCallback(OnDropdownChanged);
            _appUsageDropdown.RegisterValueChangedCallback(OnDropdownChanged);

            #endregion
        }

        private void Start()
        {
            foreach (var e in Genders) _genderDropdown.choices.Add(e);
            foreach (var e in AppUsage) _appUsageDropdown.choices.Add(e);
            
            foreach (var p in _pages) p.style.display = DisplayStyle.None;

            _currentPage.ForceSet(0);
            
            Show();
        }

        private void OnDisable()
        {
            _backButton.clicked -= OnBackButtonClicked;
            _nextButton.clicked -= OnNextButtonClicked;
            _acceptButton.clicked -= OnAcceptButtonClicked;
            _currentPage.Changed -= OnCurrentPageChanged;

            _genderDropdown.UnregisterValueChangedCallback(OnDropdownChanged);
            _appUsageDropdown.UnregisterValueChangedCallback(OnDropdownChanged);
        }

        public void Show(bool value = true)
        {
            _container.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #region Callbacks

        private void OnBackButtonClicked()
        {
            if (_currentPage.Value > 0)
            {
                _currentPage.Value--;
            }
        }

        private void OnNextButtonClicked()
        {
            var ok = ValidatePage();
            if (!ok) return;
            
            if (_currentPage.Value < _pages.Length - 1)
            {
                _currentPage.Value++;
            }
            else if (_currentPage.Value == _pages.Length - 1)
            {
                CompleteSignup();
            }
        }

        private void OnCurrentPageChanged(int value)
        {
            if (value < 0 || value >= _pages.Length) return;

            // hide previous and next pages
            if (value > 0) _pages[value - 1].style.display = DisplayStyle.None;
            if (value < _pages.Length - 1) _pages[value + 1].style.display = DisplayStyle.None;

            if (value > 2 && value != _pages.Length - 1)
            {
                _pages[value].Query<Label>().ForEach((label) =>
                {
                    var labelWithAppName = label.text.Replace("{appName}", _appName.value);
                    label.text = labelWithAppName;
                });
                
                _pages[value].Query<DropdownField>().ForEach((dropDown) =>
                {
                    foreach (var e in PreSurvey) dropDown.choices.Add(e);
                });
            }
            
            // show current page
            Debug.Log($"Showing page {value}");
            _pages[value].style.display = DisplayStyle.Flex;

            _backButton.style.display = value == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            // _nextButton.text = value == _pages.Length - 1 ? "Create profile" : "Next";
            _nextButton.style.display = value == _pages.Length - 1 ? DisplayStyle.None : DisplayStyle.Flex;
            _nextButton.text = "Next";
            
        }

        private void OnAcceptButtonClicked()
        {
            SignaturePad.Instance.Show();
        }

        private void OnDropdownChanged(ChangeEvent<string> evt)
        {
            var show = evt.newValue is OtherOption or "Yes";
            var field = (evt.target as DropdownField)?.name switch
            {
                "gender" => _genderText,
                "appUsage" => _appName,
                _ => null
            };

            if (field != null) field.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #endregion

        private void CompleteSignup()
        {
            // DataManager.Instance.SaveAccountDetailsOnFb(AccountOwner);
        }

        #region Data Validation

        private bool ValidatePage()
        {
            return _currentPage.Value switch
            {
                0 => ValidateBasicInfo(),
                1 => ValidateGenderRaceInfo(),
                2 => ValidatePersonalInfo(),
                3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 => ValidatePreSurveyInfo(),
                11 => ValidateTermsAndCondForm(),
                _ => false
            };
        }

        private bool ValidateBasicInfo()
        {
            if (string.IsNullOrEmpty(_fullNameText.value) ||
                string.IsNullOrEmpty((_emailText).value) ||
                string.IsNullOrEmpty(_zipCodeText.value) ||
                string.IsNullOrEmpty(_cityStateText.value))
            {
                Popup.Instance.Show("Please fill all the required fields.");
                return false;
            }
            
            if(!Regex.IsMatch(_emailText.value, @"^\S+@\S+\.\S+$"))
            {
                Popup.Instance.Show("Please enter a valid email.");
                return false;
            }
            
            if(!Regex.IsMatch(_zipCodeText.value, @"^\d+(-\d+)?$"))
            {
                Popup.Instance.Show("Please enter a valid zip code.");
                return false;
            }

            AccountOwner.FullName = _fullNameText.value;
            AccountOwner.Email = _emailText.value;
            AccountOwner.ZipCode = _zipCodeText.value;
            AccountOwner.CityState = _cityStateText.value;

            return true;
        }

        private bool ValidateGenderRaceInfo()
        {
            if (string.IsNullOrEmpty(_genderDropdown.value) ||
                _raceDropdown.SelectedOptions.Count == 0)
            {
                Popup.Instance.Show("Please fill all the required fields.");
                return false;
            }

            if (_genderDropdown.value == OtherOption &&
                string.IsNullOrEmpty(_genderText.value))
            {
                Popup.Instance.Show("Please enter a gender.");
                return false;
            }

            if (_raceDropdown.SelectedOptions.Contains(OtherOption) &&
                string.IsNullOrEmpty(_raceText.value))
            {
                Popup.Instance.Show("Please enter a race.");
                return false;
            }

            AccountOwner.Gender = _genderDropdown.value == OtherOption ? _genderText.value : _genderDropdown.value;
            
            AccountOwner.Race = _raceDropdown.SelectedOptions;
            if(_raceDropdown.SelectedOptions.Contains(OtherOption))
                AccountOwner.Race.Add(_raceText.value);

            return true;
        }

        private bool ValidatePersonalInfo()
        {
            if (string.IsNullOrEmpty(_appUsageDropdown.value))
            {
                Popup.Instance.Show("Please fill all the required fields.");
                return false;
            }

            if (_appUsageDropdown.value == "Yes" &&
                string.IsNullOrEmpty(_appName.value))
            {
                Popup.Instance.Show("Please enter the app name you use.");
                return false;
            }

            // AccountOwner.Description = _descriptionDropdown.value == OtherOption
            //     ? _descriptionText.value
            //     : _descriptionDropdown.value;

            return true;
        }

        private bool ValidatePreSurveyInfo()
        {
            if (_currentPage.Value == 3)
            {
                var usageReason = _pages[_currentPage.Value].Q<TextField>("usageReason");
                if (string.IsNullOrEmpty(usageReason.text))
                {
                    Popup.Instance.Show("Please fill all the required fields.");
                    return false;
                }
            }
            
            var dropDowns = _pages[_currentPage.Value].Query<DropdownField>().ToList();
            foreach (var dropDown in dropDowns)
            {
                if (string.IsNullOrEmpty(dropDown.value))
                {
                    Popup.Instance.Show("Please fill all the required fields.");
                    return false;
                }
            }
            
            return true;
        }
        
        private bool ValidateTermsAndCondForm()
        {
            if (!SignaturePad.Instance.SigningComplete)
            {
                Popup.Instance.Show("Please read the terms and conditions and accept it.", "Terms & Conditions");
                return false;
            }

            return true;
        }

        #endregion
    }
}