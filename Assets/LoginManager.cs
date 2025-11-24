using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;

public class LoginManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;

    private FirebaseAuth auth;

    // ✅ Static properties to store the current logged-in user
    public static string CurrentUserId { get; private set; }
    public static string CurrentUserEmail { get; private set; }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // Add UI button listeners
        loginButton.onClick.AddListener(() => Login(emailInput.text, passwordInput.text));
        registerButton.onClick.AddListener(() => Register(emailInput.text, passwordInput.text));
    }

    void Login(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("❌ Login failed: " + task.Exception);
                return;
            }

            // ✅ Store user ID and email
            CurrentUserId = task.Result.User.UserId;
            CurrentUserEmail = email;

            Debug.Log($"✅ Logged in: {CurrentUserId}, email: {CurrentUserEmail}");

            // Load signup/registration scene
            SceneManager.LoadScene("Registration");
        });
    }

    void Register(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("❌ Registration failed: " + task.Exception);
                return;
            }

            // ✅ Store user ID and email
            CurrentUserId = task.Result.User.UserId;
            CurrentUserEmail = email;

            Debug.Log($"✅ Registered: {CurrentUserId}, email: {CurrentUserEmail}");

            // Load signup/registration scene
            SceneManager.LoadScene("Registration");
        });
    }
}
