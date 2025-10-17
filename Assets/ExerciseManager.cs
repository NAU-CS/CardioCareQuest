using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using Gameplay.UI;

public class ExerciseManager : MonoBehaviour
{
    [Header("UI Elements (Assign in Inspector)")]
    public TMP_InputField Input_Type;
    public TMP_InputField Input_Date;
    public TMP_InputField Input_Time;
    public TMP_InputField Input_Calories;
    public TMP_Dropdown Dropdown_Duration;
    public TMP_Dropdown Dropdown_HeartRate;
    public Button Button_FetchHealthKit;
    public Button Button_Save;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;

    private bool dataFetched = false;  // true if HealthKit provided data
    private bool firebaseReady = false;

    // Colors for Save button states
    private Color colorEnabled = Color.green;
    private Color colorDisabled = Color.gray;

    void Awake()
    {
        // Disable Save button at startup
        SetSaveButtonState(false);

        // Watch manual entry changes
        if (Input_Type != null)
            Input_Type.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        if (Input_Date != null)
            Input_Date.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        if (Input_Time != null)
            Input_Time.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        // Hook up button listeners
        if (Button_FetchHealthKit != null)
            Button_FetchHealthKit.onClick.AddListener(FetchFromHealthKit);

        if (Button_Save != null)
            Button_Save.onClick.AddListener(SaveExerciseData);
    }

    void Start()
    {
        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                auth = FirebaseAuth.DefaultInstance;
                firebaseReady = true;

                Debug.Log("✅ Firebase ready");
                UpdateSaveButtonState();
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

        ColorBlock colors = Button_Save.colors;
        Color activeColor = enabled ? colorEnabled : colorDisabled;
        colors.normalColor = activeColor;
        colors.highlightedColor = activeColor;
        colors.pressedColor = activeColor * 0.8f;
        colors.selectedColor = activeColor;
        colors.disabledColor = colorDisabled;
        Button_Save.colors = colors;
    }

    private void UpdateSaveButtonState()
    {
        if (!firebaseReady || Button_Save == null || auth == null)
        {
            SetSaveButtonState(false);
            return;
        }

        FirebaseUser user = auth.CurrentUser;
        if (user == null)
        {
            SetSaveButtonState(false);
            return;
        }

        bool hasType = !string.IsNullOrEmpty(Input_Type.text.Trim());
        bool hasDate = !string.IsNullOrEmpty(Input_Date.text.Trim());
        bool hasTime = !string.IsNullOrEmpty(Input_Time.text.Trim());

        // Enable Save if (HealthKit fetched) OR (user typed required fields)
        SetSaveButtonState(dataFetched || (hasType && hasDate && hasTime));
    }

    private void FetchFromHealthKit()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // Call native HealthKit bridge for heart rate
        HealthKitManager.FetchLatestHeartRate((heartRate) =>
        {
            if (heartRate > 0)
            {
                Input_HeartRate.text = heartRate.ToString();
                dataFetched = true;
                Debug.Log("✅ Heart rate fetched from HealthKit: " + heartRate);
                UpdateSaveButtonState();
            }
            else
            {
                Debug.LogWarning("⚠️ No heart rate found in HealthKit. Enter manually.");
            }
        });
#else
        Debug.LogWarning("⚠️ HealthKit not available in Editor. Enter manually.");
#endif
    }

    private void SaveExerciseData()
    {
        if (!firebaseReady || dbRef == null || auth == null)
        {
            Debug.LogWarning("❌ Firebase not ready.");
            return;
        }

        FirebaseUser user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No user logged in.");
            return;
        }

        string uid = user.UserId;

        var exerciseData = new Dictionary<string, object>
        {
            { "type", Input_Type.text },
            { "date", Input_Date.text },
            { "time", Input_Time.text },
            { "duration", Dropdown_Duration.options[Dropdown_Duration.value].text },
            { "heartRate", Dropdown_HeartRate.options[Dropdown_HeartRate.value].text },
            { "calories", Input_Calories.text },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };

        dbRef.Child("users").Child(uid).Child("exercises").Push().SetValueAsync(exerciseData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    {
                        Debug.Log("✅ Exercise saved for user " + uid);
                        // Trigger next scene in SignaturePad flow
                        SignaturePad.TriggerNextScene();
                    }
                else
                    Debug.LogError("❌ Failed to save data: " + task.Exception);
            });
           
    }
}
