using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;

    private FirebaseAuth auth;

    // ✅ Static property to store current logged-in user ID
    public static string CurrentUserId { get; private set; }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        loginButton.onClick.AddListener(() => Login(emailInput.text, passwordInput.text));
        registerButton.onClick.AddListener(() => Register(emailInput.text, passwordInput.text));
    }

    void Login(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("❌ Login failed: " + task.Exception);
                return;
            }

            CurrentUserId = task.Result.User.UserId; // ✅ Store logged-in user ID
            Debug.Log("✅ Logged in: " + CurrentUserId);
            SceneManager.LoadScene("Registration"); // or whatever your signup scene is called
        });
    }

    void Register(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("❌ Registration failed: " + task.Exception);
                return;
            }

            CurrentUserId = task.Result.User.UserId; // ✅ Store logged-in user ID
            Debug.Log("✅ Registered: " + CurrentUserId);
            SceneManager.LoadScene("Registration");
        });
    }
}
