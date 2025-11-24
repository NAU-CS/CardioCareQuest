using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using Gameplay.UI;

public class QuizManagerTMP : MonoBehaviour
{
    [Header("UI Elements (Assign in Inspector)")]
    public Button Button_Save;
    public Button Button_Next;

    public TMP_Dropdown Dropdown_Q1;
    public TMP_Dropdown Dropdown_Q2;
    public TMP_Dropdown Dropdown_Q3;
    public TMP_Dropdown Dropdown_Q4;
    public TMP_Dropdown Dropdown_Q5;
    public TMP_Dropdown Dropdown_Q6;
    public TMP_Dropdown Dropdown_Q7;
    public TMP_Dropdown Dropdown_Q8;
    public TMP_Dropdown Dropdown_Q9;
    public TMP_Dropdown Dropdown_Q10;

    public TMP_Text Text_Status;

    private TMP_Dropdown[] allDropdowns;
    private DatabaseReference dbRef;
    private FirebaseAuth auth;

    private readonly Color btnEnabled = new Color(0.18f, 0.70f, 0.24f, 1f);
    private readonly Color btnDisabled = new Color(0.55f, 0.55f, 0.55f, 1f);
    private readonly Color correctColor = new Color(0.20f, 0.70f, 0.20f, 1f);
    private readonly Color wrongColor = new Color(0.85f, 0.20f, 0.20f, 1f);

    private readonly string[] correctAnswers = new string[]
    {
        "(C) The rest of their life",
        "(A) Yes",
        "(C) Normal",
        "(A) High",
        "(A) Everyday",
        "(B) Go down",
        "(B) Go down",
        "(A) Yes",
        "(B) No",
        "(A) Yes"
    };

    void Awake()
    {
        allDropdowns = new TMP_Dropdown[]
        {
            Dropdown_Q1, Dropdown_Q2, Dropdown_Q3, Dropdown_Q4, Dropdown_Q5,
            Dropdown_Q6, Dropdown_Q7, Dropdown_Q8, Dropdown_Q9, Dropdown_Q10
        };

        SetSaveButtonState(false);

        // Disable NEXT button at start
        if (Button_Next != null)
        {
            Button_Next.interactable = false;
            var nextGraphic = Button_Next.targetGraphic as Graphic;
            if (nextGraphic != null) nextGraphic.color = btnDisabled;
        }

        foreach (var dd in allDropdowns)
        {
            if (dd != null)
                dd.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });
        }

        if (Button_Save != null)
            Button_Save.onClick.AddListener(OnClickSave);

        if (Button_Next != null)
            Button_Next.onClick.AddListener(OnClickNext);
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("✅ Firebase ready");
            }
            else
            {
                Debug.LogError("❌ Firebase not available: " + task.Result);
            }
        });
    }

    private void SetSaveButtonState(bool enabled)
    {
        if (Button_Save == null) return;

        Button_Save.interactable = enabled;

        var g = Button_Save.targetGraphic as Graphic;
        if (g != null) g.color = enabled ? btnEnabled : btnDisabled;
    }

    private void UpdateSaveButtonState()
    {
        SetSaveButtonState(AreAllAnswered());
    }

    private bool AreAllAnswered()
    {
        foreach (var dd in allDropdowns)
        {
            string choice = dd.options[dd.value].text.Trim();
            if (string.IsNullOrEmpty(choice)) return false;
            if (choice.StartsWith("select", StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    private void OnClickSave()
    {
        var (userAnswers, correctness, allCorrect) = GradeAndColorUI();

        if (auth == null || auth.CurrentUser == null)
        {
            if (Text_Status != null)
                Text_Status.text = "Please log in before saving.";
            return;
        }

        string uid = auth.CurrentUser.UserId;

        var data = new Dictionary<string, object>
        {
            { "answers", userAnswers },
            { "correctness", correctness },
            { "score", CountTrue(correctness) + "/10" },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };

        dbRef.Child("users").Child(uid).Child("quizResults").Push()
            .SetValueAsync(data).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("✅ Quiz results saved for user " + uid);

                    // Disable save button
                    Button_Save.interactable = false;

                    // ENABLE NEXT BUTTON AFTER SAVE SUCCESS
                    if (Button_Next != null)
                    {
                        Button_Next.interactable = true;
                        var nextGraphic = Button_Next.targetGraphic as Graphic;
                        if (nextGraphic != null) nextGraphic.color = btnEnabled;
                    }

                    if (Text_Status != null)
                        Text_Status.text = "Click on 'Next'";
                }
                else
                {
                    if (Text_Status != null)
                        Text_Status.text = "Failed to save quiz. Try again.";
                }
            });
    }

    private void OnClickNext()
    {
        Debug.Log("➡️ Button_Next clicked...");
        StartCoroutine(CountdownAndLoadScene(0));
    }

    private IEnumerator CountdownAndLoadScene(int delay)
    {
        yield return new WaitForSeconds(delay);
        SignaturePad.TriggerNextScene();
    }

    private (string[] answers, bool[] correctness, bool allCorrect) GradeAndColorUI()
    {
        string[] userAnswers = new string[allDropdowns.Length];
        bool[] correctness = new bool[allDropdowns.Length];
        bool allCorrect = true;

        for (int i = 0; i < allDropdowns.Length; i++)
        {
            var dd = allDropdowns[i];
            string selectedText = dd.options[dd.value].text.Trim();
            userAnswers[i] = selectedText;

            bool isCorrect = string.Equals(selectedText, correctAnswers[i], StringComparison.OrdinalIgnoreCase);
            correctness[i] = isCorrect;
            if (!isCorrect) allCorrect = false;

            var bg = dd.GetComponent<Image>();
            if (bg != null) bg.color = isCorrect ? correctColor : wrongColor;
        }

        return (userAnswers, correctness, allCorrect);
    }

    private int CountTrue(bool[] arr)
    {
        int c = 0;
        foreach (var b in arr) if (b) c++;
        return c;
    }
}
