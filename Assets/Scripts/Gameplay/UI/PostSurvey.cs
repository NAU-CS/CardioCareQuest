using System;
using System.Linq;
using System.Text.RegularExpressions;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Utility;

namespace Gameplay.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PostSurvey : Singleton<PostSurvey>
    {
        #region Private UI Elements

        private VisualElement _container;
        private VisualElement _preSurveyQuestionsForm;

        private Button _backButton;
        private Button _nextButton;

        private VisualElement[] _pages;
        private readonly CallbackValue<int> _currentPage = new();
        
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
        
        private static readonly string[] PostSurveyAnswer =
        {
            "Strongly disagree",
            "Somewhat disagree",
            "Neither agree nor disagree",
            "Somewhat agree",
            "Strongly agree"
        };


        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _container = root.Q<VisualElement>("container");
            if (_firstPaint)
            {
                _container.style.display = DisplayStyle.None;
                _firstPaint = false;
            }

            
            _preSurveyQuestionsForm = root.Q<VisualElement>("post-survey-questions");
            
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
                _form1,
                _form2,
                _form3,
                _form4,
                _form5,
                _form6,
                _form7,
                _form8,
            };

            _backButton = root.Q<Button>("back");
            _nextButton = root.Q<Button>("next");
            
            #region Register Callbacks

            _backButton.clicked += OnBackButtonClicked;
            _nextButton.clicked += OnNextButtonClicked;
            _currentPage.Changed += OnCurrentPageChanged;

            #endregion
        }

        private void Start()
        {
            foreach (var p in _pages) p.style.display = DisplayStyle.None;

            _currentPage.ForceSet(0);
            
            Show();
        }

        private void OnDisable()
        {
            _backButton.clicked -= OnBackButtonClicked;
            _nextButton.clicked -= OnNextButtonClicked;
            _currentPage.Changed -= OnCurrentPageChanged;
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
            var ok = ValidatePostSurveyInfo();
            if (!ok) return;
            
            if (_currentPage.Value < _pages.Length - 1)
            {
                _currentPage.Value++;
            }
            else if (_currentPage.Value == _pages.Length - 1)
            {
                SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            }
        }

        private void OnCurrentPageChanged(int value)
        {
            if (value < 0 || value >= _pages.Length) return;

            // hide previous and next pages
            // Hide all pages except the current one
            for (var i = 0; i < _pages.Length; i++)
            {
                _pages[i].style.display = i == value ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _pages[value].Query<Label>().ForEach((label) =>
            {
                var labelWithAppName = label.text.Replace("{appName}", "Cardio Care Quest");
                label.text = labelWithAppName;
            });
                
            _pages[value].Query<DropdownField>().ForEach((dropDown) =>
            {
                foreach (var e in PostSurveyAnswer) dropDown.choices.Add(e);
            });
            
            // show current page
            Debug.Log($"Showing page {value}");
            _pages[value].style.display = DisplayStyle.Flex;

            _backButton.style.display = value == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _nextButton.text = value == _pages.Length - 1 ? "Submit" : "Next";
        }

        #endregion

        private void CompleteSignup()
        {
            // DataManager.Instance.SaveAccountDetailsOnFb(AccountOwner);
        }
        
        private bool ValidatePostSurveyInfo()
        {
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
    }
}