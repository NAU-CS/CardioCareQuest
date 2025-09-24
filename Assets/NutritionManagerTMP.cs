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

public class NutritionManagerTMP : MonoBehaviour
{
    [Header("UI Elements (Assign in Inspector)")]
    public TMP_InputField Input_MealDescription;
    public TMP_InputField Input_Protein;

    public TMP_Dropdown Dropdown_Vegetable;
    public TMP_Dropdown Dropdown_Fruit;
    public TMP_Dropdown Dropdown_Grains;
    public TMP_Dropdown Dropdown_Salt;
    public TMP_Dropdown Dropdown_Sugar;
    public TMP_Dropdown Dropdown_Fat;
    public TMP_Dropdown Dropdown_Cholesterol;
    public TMP_Dropdown Dropdown_Meal;

    public Button Button_FetchHealthKit;
    public Button Button_Save;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;

    private bool dataFetched = false;  // true if HealthKit provided protein data
    private bool firebaseReady = false;

    // Colors for Save button states
    private Color colorEnabled = Color.green;
    private Color colorDisabled = Color.gray;

    void Awake()
    {
        // Disable Save button at startup
        SetSaveButtonState(false);

        // Watch manual entry changes
        if (Input_MealDescription != null)
            Input_MealDescription.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        if (Input_Protein != null)
            Input_Protein.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        // Hook up button listeners
        if (Button_FetchHealthKit != null)
            Button_FetchHealthKit.onClick.AddListener(FetchFromHealthKit);

        if (Button_Save != null)
            Button_Save.onClick.AddListener(SaveNutritionData);
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

        bool hasDescription = !string.IsNullOrEmpty(Input_MealDescription.text.Trim());
        bool hasProtein = !string.IsNullOrEmpty(Input_Protein.text.Trim());

        // Enable Save if (HealthKit fetched) OR (user typed both fields)
        SetSaveButtonState(dataFetched || (hasDescription && hasProtein));
    }

    private void FetchFromHealthKit()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // Call into native HealthKit bridge
        HealthKitManager.FetchLatestProtein((proteinValue) =>
        {
            if (!string.IsNullOrEmpty(proteinValue))
            {
                Input_Protein.text = proteinValue;
                dataFetched = true; // Mark that data has been fetched
                Debug.Log("✅ HealthKit protein data fetched");
                UpdateSaveButtonState();
            }
            else
            {
                Debug.LogWarning("⚠️ No HealthKit protein data found. Enter manually.");
            }
        });
#else
        Debug.LogWarning("⚠️ Simulated HealthKit fetch in Editor. Enter manually on device.");
#endif
    }

    private void SaveNutritionData()
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

        var nutritionData = new Dictionary<string, object>
        {
            { "mealDescription", Input_MealDescription.text },
            { "protein", Input_Protein.text },
            { "vegetables", Dropdown_Vegetable.options[Dropdown_Vegetable.value].text },
            { "fruits", Dropdown_Fruit.options[Dropdown_Fruit.value].text },
            { "grains", Dropdown_Grains.options[Dropdown_Grains.value].text },
            { "salt", Dropdown_Salt.options[Dropdown_Salt.value].text },
            { "sugar", Dropdown_Sugar.options[Dropdown_Sugar.value].text },
            { "fat", Dropdown_Fat.options[Dropdown_Fat.value].text },
            { "cholesterol", Dropdown_Cholesterol.options[Dropdown_Cholesterol.value].text },
            { "mealType", Dropdown_Meal.options[Dropdown_Meal.value].text },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };

        dbRef.Child("users").Child(uid).Child("nutrition").Push().SetValueAsync(nutritionData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    {
                        Debug.Log("✅ Nutrition data saved for user " + uid);
                        // Trigger next scene in SignaturePad flow
                        SignaturePad.TriggerNextScene();
                    }
                else
                    Debug.LogError("❌ Failed to save data: " + task.Exception);
            });
           
    }
}
