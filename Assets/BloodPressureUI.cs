using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class BloodPressureUI : MonoBehaviour
{
    [Header("UI Elements (Assign in Inspector)")]
    public TMP_Dropdown Dropdown_Position;
    public TMP_InputField Input_Diastolic;
    public TMP_InputField Input_Systolic;
    public Button Button_FetchHealthKit;
    public Button Button_SaveFirebase;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private bool dataFetched = false; // True if HealthKit data was fetched

    // Colors for Save button states
    private Color colorEnabled = Color.green;
    private Color colorDisabled = Color.gray;

    void Awake()
    {
        // Save button disabled at scene load
        SetSaveButtonState(false);

        // Listen to manual input changes
        if (Input_Systolic != null)
            Input_Systolic.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        if (Input_Diastolic != null)
            Input_Diastolic.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        // Button listeners
        if (Button_FetchHealthKit != null)
            Button_FetchHealthKit.onClick.AddListener(FetchFromHealthKit);

        if (Button_SaveFirebase != null)
            Button_SaveFirebase.onClick.AddListener(SaveData);
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
                Debug.Log("✅ Firebase ready");

                // Check if fields already have data (e.g., pre-filled) and update Save button
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
        if (Button_SaveFirebase == null) return;

        Button_SaveFirebase.interactable = enabled;

        ColorBlock colors = Button_SaveFirebase.colors;
        Color activeColor = enabled ? colorEnabled : colorDisabled;
        colors.normalColor = activeColor;
        colors.highlightedColor = activeColor;
        colors.pressedColor = activeColor * 0.8f;
        colors.selectedColor = activeColor;
        colors.disabledColor = colorDisabled;
        Button_SaveFirebase.colors = colors;
    }

    private void UpdateSaveButtonState()
    {
        if (Button_SaveFirebase == null || dbRef == null || auth == null)
        {
            SetSaveButtonState(false);
            return;
        }

        bool systolicValid = !string.IsNullOrEmpty(Input_Systolic.text.Trim()) && int.TryParse(Input_Systolic.text, out _);
        bool diastolicValid = !string.IsNullOrEmpty(Input_Diastolic.text.Trim()) && int.TryParse(Input_Diastolic.text, out _);

        // Enable Save button only if HealthKit fetched OR manual input is valid
        SetSaveButtonState(dataFetched || (systolicValid && diastolicValid));
    }

    private void FetchFromHealthKit()
    {
#if UNITY_IOS && !UNITY_EDITOR
        HealthKitManager.FetchLatestBloodPressure((systolic, diastolic) =>
        {
            if (systolic > 0 && diastolic > 0)
            {
                Input_Systolic.text = systolic.ToString();
                Input_Diastolic.text = diastolic.ToString();
                dataFetched = true; // Mark that data has been fetched
                Debug.Log("✅ HealthKit data fetched");
                UpdateSaveButtonState();
            }
            else
            {
                Debug.LogWarning("No HealthKit data found. Enter manually.");
            }
        });
#else
        Debug.LogWarning("HealthKit not available in Editor/iOS. Enter manually.");
#endif
    }

    private void SaveData()
    {
        if (dbRef == null || auth == null)
        {
            Debug.LogWarning("Firebase not ready.");
            return;
        }

        if (!int.TryParse(Input_Systolic.text, out int systolic) ||
            !int.TryParse(Input_Diastolic.text, out int diastolic))
        {
            Debug.LogWarning("❌ Enter valid numbers for blood pressure.");
            return;
        }

        FirebaseUser user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No user logged in.");
            return;
        }

        string position = Dropdown_Position.options[Dropdown_Position.value].text;
        string uid = user.UserId;

        var bpData = new Dictionary<string, object>
        {
            { "systolic", systolic },
            { "diastolic", diastolic },
            { "position", position },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };

        dbRef.Child("users").Child(uid).Child("bloodPressure").Push().SetValueAsync(bpData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("✅ Saved BP for user " + uid);
                else
                    Debug.LogError("❌ Error saving to Firebase: " + task.Exception);
            });
    }
}
