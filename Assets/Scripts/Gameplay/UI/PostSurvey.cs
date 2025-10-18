using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
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
        private bool _hasShownPopupOnce;
        private int _lastPopupPage = -1;

        public AccountOwner AccountOwner { get; } = new();
        private string _signupRecordKey = Guid.NewGuid().ToString();


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
	        if (_currentPage.Value != _lastPopupPage)
		        _hasShownPopupOnce = false;

	        var ok = ValidatePostSurveyInfo();
	        _lastPopupPage = _currentPage.Value;

	        if (!ok) return;

	        if (_currentPage.Value < _pages.Length - 1)
	        {
		        _currentPage.Value++;
	        }
	        else if (_currentPage.Value == _pages.Length - 1)
	        {
		        SaveSurveyToFirebase();
		        SceneManager.LoadScene("LoginScene", LoadSceneMode.Single);
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
		
	    private void SaveSurveyToFirebase()
		{
		    string uid = LoginManager.CurrentUserId; // current signed-in user
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

		    // Collect all dropdown responses into a single dictionary
		    var allResponses = new Dictionary<string, object>();
		    for (int i = 0; i < _pages.Length; i++)
		    {
		        var pageResponses = _pages[i].Query<DropdownField>().ToList();
		        for (int j = 0; j < pageResponses.Count; j++)
		        {
		            string key = $"Q{i + 1}_{j + 1}";
		            allResponses[key] = pageResponses[j].value;
		        }
		    }

		    // Add timestamp
    		allResponses["timestamp"] = DateTime.UtcNow.ToString("o"); // ISO 8601 UTC
		    var dbRef = Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference;

		    // ✅ Save everything under the single signup key for the user
		    dbRef.Child("users").Child(uid).Child("general")
		         .Child(_signupRecordKey)
		         .Child("postSurveyResponses")
		         .UpdateChildrenAsync(allResponses)
		         .ContinueWithOnMainThread(task =>
		         {
		             if (task.IsCompleted)
		             {
		                 Debug.Log("✅ Survey responses saved under single signup record.");
		                 
		                 // Show success popup and optionally load next scene
		                 Popup.Instance.Show("🎉 Congratulations on completing the survey!");
		                 SceneManager.LoadScene("LoginScene", LoadSceneMode.Single);
		             }
		             else
		             {
		                 Debug.LogError("❌ Failed to save survey: " + task.Exception);
		                 Popup.Instance.Show("Failed to save survey. Please try again.");
		             }
		         });
		}


    }
}