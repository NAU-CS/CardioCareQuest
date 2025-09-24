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

public class MedicationManagerTMP : MonoBehaviour
{
    [Header("UI Elements (Assign in Inspector)")]
    public TMP_InputField Input_MedName;
    public TMP_InputField Input_Time;
    public TMP_Dropdown Dropdown_Schedule;
    public TMP_Dropdown Dropdown_MedType;
    public TMP_Dropdown Dropdown_MissedDoseReason;
    public Toggle Toggle_Compliance;
    public Toggle Toggle_Reminder;
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
        if (Input_MedName != null)
            Input_MedName.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        if (Input_Time != null)
            Input_Time.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        // Hook up button listeners
        if (Button_FetchHealthKit != null)
            Button_FetchHealthKit.onClick.AddListener(FetchFromHealthKit);

        if (Button_Save != null)
            Button_Save.onClick.AddListener(SaveMedicationData);
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

        bool hasMedName = !string.IsNullOrEmpty(Input_MedName.text.Trim());
        bool hasTime = !string.IsNullOrEmpty(Input_Time.text.Trim());

        // Enable Save if (HealthKit fetched) OR (user typed both fields)
        SetSaveButtonState(dataFetched || (hasMedName && hasTime));
    }

    private void FetchFromHealthKit()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // Call into native HealthKit bridge
        HealthKitManager.FetchLatestMedication((medName, medTime) =>
        {
            if (!string.IsNullOrEmpty(medName) && !string.IsNullOrEmpty(medTime))
            {
                Input_MedName.text = medName;
                Input_Time.text = medTime;
                dataFetched = true; // Mark that data has been fetched
                Debug.Log("✅ HealthKit medication data fetched");
                UpdateSaveButtonState();
            }
            else
            {
                Debug.LogWarning("⚠️ No HealthKit medication data found. Enter manually.");
            }
        });
#else
        Debug.LogWarning("⚠️ Simulated HealthKit fetch in Editor. Enter manually on device.");
#endif
    }

    private void SaveMedicationData()
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

        var medData = new Dictionary<string, object>
        {
            { "medicationName", Input_MedName.text },
            { "medicationType", Dropdown_MedType.options[Dropdown_MedType.value].text },
            { "schedule", Dropdown_Schedule.options[Dropdown_Schedule.value].text },
            { "time", Input_Time.text },
            { "compliance", Toggle_Compliance.isOn },
            { "missedDoseReason", Dropdown_MissedDoseReason.options[Dropdown_MissedDoseReason.value].text },
            { "reminder", Toggle_Reminder.isOn },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };

        dbRef.Child("users").Child(uid).Child("medications").Push().SetValueAsync(medData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    {
                        Debug.Log("✅ Medication saved for user " + uid);
                        // Trigger next scene in SignaturePad flow
                        SignaturePad.TriggerNextScene();
                    }
                else
                    Debug.LogError("❌ Failed to save data: " + task.Exception);
            });
            
    }
}
