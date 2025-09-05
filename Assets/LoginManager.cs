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

            Debug.Log("✅ Logged in: " + task.Result.User.UserId);
            SceneManager.LoadScene("Registration");
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

            Debug.Log("✅ Registered: " + task.Result.User.UserId);
            SceneManager.LoadScene("Registration");
        });
    }
}
