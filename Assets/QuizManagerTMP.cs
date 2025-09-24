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
    public TMP_Text Text_Status; // optional status text

    private TMP_Dropdown[] allDropdowns;
    private DatabaseReference dbRef;
    private FirebaseAuth auth;

    // Colors
    private readonly Color btnEnabled = new Color(0.18f, 0.70f, 0.24f, 1f); 
    private readonly Color btnDisabled = new Color(0.55f, 0.55f, 0.55f, 1f); 
    private readonly Color correctColor = new Color(0.20f, 0.70f, 0.20f, 1f); 
    private readonly Color wrongColor   = new Color(0.85f, 0.20f, 0.20f, 1f); 

    // Expected answers
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

        foreach (var dd in allDropdowns)
        {
            if (dd != null) dd.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });
        }

        if (Button_Save != null) Button_Save.onClick.AddListener(OnClickSave);
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

    // Enable/disable Save button
    private void SetSaveButtonState(bool enabled)
    {
        if (Button_Save == null) return;

        Button_Save.interactable = enabled;
        var g = Button_Save.targetGraphic as Graphic;
        if (g != null) g.color = enabled ? btnEnabled : btnDisabled;

        var img = Button_Save.GetComponent<Image>();
        if (img != null) img.color = enabled ? btnEnabled : btnDisabled;
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
            Debug.LogError("❌ No logged-in user. Make sure Firebase login is working.");
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
            //{ "finalResult", allCorrect ? "Excellent!!!" : "Good, try again" },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };


            dbRef.Child("users").Child(uid).Child("quizResults").Push()
                .SetValueAsync(data).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        Debug.Log("✅ Quiz results saved for user " + uid);

                        // 🔒 Disable the save button
                        if (Button_Save != null)
                            Button_Save.interactable = false;
                        
                        if (Text_Status != null)
                        {
                            // Start 60-second countdown before loading next scene
                            Text_Status.text = "Next scene in 60s…";
                            // Assuming this script is a MonoBehaviour
                            StartCoroutine(CountdownAndLoadScene(60));
                        }
                    }
                    else
                    {
                        Debug.LogError("❌ Firebase save failed: " + task.Exception);
                        if (Text_Status != null)
                            Text_Status.text = "Failed to save quiz. Try again.";
                    }
                });

            
    }
     // Coroutine for countdown
                 private IEnumerator CountdownAndLoadScene(int delay)
            {
                int remaining = delay;
                while (remaining > 0)
                {
                    if (Text_Status != null)
                        Text_Status.text = $"Next scene in {remaining}s…";

                    yield return new WaitForSeconds(1f);
                    remaining--;
                }
                

                // ✅ Trigger next scene once countdown is done
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

            Debug.Log($"Q{i+1}: Selected='{selectedText}' | Expected='{correctAnswers[i]}' | Correct={isCorrect}");
        }

        return (userAnswers, correctness, allCorrect);
    }

    private int CountTrue(bool[] arr)
    {
        int c = 0; foreach (var b in arr) if (b) c++; return c;
    }
}
