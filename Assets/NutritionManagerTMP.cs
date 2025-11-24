using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Storage;
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

    [Header("Meal Photo Upload")]
    public Button Button_UploadPhoto;
    public RawImage ImagePreview; // optional preview
    private string mealPhotoUrl = null;

    [Header("Ingredients System")]
    public TMP_InputField Input_NewIngredient;
    public Button Button_AddIngredient;
    public Transform IngredientListContainer;
    public GameObject IngredientItemPrefab;
    private List<string> ingredients = new List<string>();

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private bool dataFetched = false;  
    private bool firebaseReady = false;

    private Color colorEnabled = Color.green;
    private Color colorDisabled = Color.gray;

    void Awake()
    {
        SetSaveButtonState(false);

        if (Input_MealDescription != null)
            Input_MealDescription.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        if (Input_Protein != null)
            Input_Protein.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        if (Button_FetchHealthKit != null)
            Button_FetchHealthKit.onClick.AddListener(FetchFromHealthKit);

        if (Button_Save != null)
            Button_Save.onClick.AddListener(SaveNutritionData);

        if (Button_UploadPhoto != null)
            Button_UploadPhoto.onClick.AddListener(OnUploadPhotoClicked);

        if (Button_AddIngredient != null)
            Button_AddIngredient.onClick.AddListener(AddIngredient);
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                auth = FirebaseAuth.DefaultInstance;
                firebaseReady = true;
                Debug.Log("Firebase ready");
                UpdateSaveButtonState();
            }
            else
            {
                Debug.LogError("Firebase not available: " + task.Result);
            }
        });
    }

    // ---------------- SAVE BUTTON ----------------
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
        if (!firebaseReady || auth == null || Button_Save == null)
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

        SetSaveButtonState(dataFetched || (hasDescription && hasProtein));
    }

    // ---------------- HEALTHKIT ----------------
    private void FetchFromHealthKit()
    {
#if UNITY_IOS && !UNITY_EDITOR
        HealthKitManager.FetchLatestProtein((proteinValue) =>
        {
            if (!string.IsNullOrEmpty(proteinValue))
            {
                Input_Protein.text = proteinValue;
                dataFetched = true;
                UpdateSaveButtonState();
            }
            else
            {
                Debug.LogWarning("No HealthKit protein data found.");
            }
        });
#else
        Debug.LogWarning("Simulated HealthKit fetch in Editor.");
#endif
    }

    // ---------------- PHOTO UPLOAD ----------------
    private void OnUploadPhotoClicked()
    {
        StartCoroutine(SelectPhoto());
    }

    private IEnumerator SelectPhoto()
    {
        yield return PhotoPicker.PickImage((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("Picked image: " + path);
                StartCoroutine(UploadMealPhoto(path));
            }
        });
    }

    private IEnumerator UploadMealPhoto(string localPath)
    {
        if (!firebaseReady || auth.CurrentUser == null)
        {
            Debug.LogError("Cannot upload – no signed-in user.");
            yield break;
        }

        var storage = FirebaseStorage.DefaultInstance;
        var storageRef = storage.GetReference("mealPhotos");
        string fileName = $"{auth.CurrentUser.UserId}_{Guid.NewGuid()}.jpg";
        var fileRef = storageRef.Child(fileName);

        var uploadTask = fileRef.PutFileAsync(localPath);
        yield return new WaitUntil(() => uploadTask.IsCompleted);

        if (uploadTask.Exception != null)
        {
            Debug.LogError("Upload failed: " + uploadTask.Exception);
            yield break;
        }

        fileRef.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted && !task.IsCanceled)
            {
                mealPhotoUrl = task.Result.ToString();
                Debug.Log("Photo uploaded: " + mealPhotoUrl);
                if (ImagePreview != null)
                    StartCoroutine(LoadImagePreview(mealPhotoUrl));
            }
            else
            {
                Debug.LogError("Failed to get download URL: " + task.Exception);
            }
        });
    }

    private IEnumerator LoadImagePreview(string url)
    {
        WWW www = new WWW(url);
        yield return www;
        if (ImagePreview != null)
            ImagePreview.texture = www.texture;
    }

    // ---------------- INGREDIENT LIST ----------------
    private void AddIngredient()
    {
        string ingredient = Input_NewIngredient.text.Trim();
        if (string.IsNullOrEmpty(ingredient)) return;

        ingredients.Add(ingredient);

        if (IngredientItemPrefab != null && IngredientListContainer != null)
        {
            GameObject item = Instantiate(IngredientItemPrefab, IngredientListContainer);
            TMP_Text textComp = item.GetComponentInChildren<TMP_Text>();
            if (textComp != null) textComp.text = ingredient;
        }

        Input_NewIngredient.text = "";
    }

    // ---------------- SAVE NUTRITION DATA ----------------
    private void SaveNutritionData()
    {
        if (!firebaseReady || auth.CurrentUser == null || dbRef == null)
        {
            Debug.LogError("Cannot save – Firebase not ready or no signed-in user.");
            return;
        }

        string uid = auth.CurrentUser.UserId;

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
            { "timestamp", DateTime.UtcNow.ToString("o") },
            { "mealPhotoUrl", mealPhotoUrl ?? "" },
            { "ingredients", ingredients }
        };

        dbRef.Child("users").Child(uid).Child("nutrition").Push().SetValueAsync(nutritionData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Nutrition data saved for user " + uid);
                    SignaturePad.TriggerNextScene();
                }
                else
                {
                    Debug.LogError("Failed to save nutrition data: " + task.Exception);
                }
            });
    }
}
