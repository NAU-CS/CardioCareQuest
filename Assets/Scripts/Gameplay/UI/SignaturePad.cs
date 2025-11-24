using System;
using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Utility;

namespace Gameplay.UI
{
    [System.Serializable]
    public class DocumentPayload
    {
        public string name;
        public string email;

        // Handwritten signature + date
        public string signature;
        public string date;

        // Audio consent (reusing handwritten for now)
        public string audioSignature;
        public string audioDate;

        // Video consent (reusing handwritten for now)
        public string videoSignature;
        public string videoDate;
        
    }    

    [RequireComponent(typeof(UIDocument))]
    public class SignaturePad : Singleton<SignaturePad>
    {
        public const string BaseUrl = "https://consentform-api-35495734440.us-central1.run.app";

        private bool _isMouseDown;

        private VisualElement _container;
        private VisualElement _inputField;
        private Image _image;
        private Button _saveButton;
        private Button _resetButton;
        private Button _closeButton;


        private Texture2D _texture;
        private readonly List<Vector2> _points = new();

        private float InputFieldWidth => _container?.layout.width ?? 0;
        private float InputFieldHeight => _container?.layout.height ?? 0;

        public bool SigningComplete { get; private set; }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _container = root.Q<VisualElement>("container");
            _inputField = _container.Q<VisualElement>("input-field");

            _saveButton = _container.Q<Button>("save-button");
            _resetButton = _container.Q<Button>("reset-button");
            _closeButton = _container.Q<Button>("close-button");

            _inputField.RegisterCallback<MouseDownEvent>(OnSignaturePadMouseDown);
            _inputField.RegisterCallback<MouseMoveEvent>(OnSignaturePadMouseMove);
            _inputField.RegisterCallback<MouseUpEvent>(OnSignaturePadMouseUp);
            _inputField.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                _texture = CreateTexture();
                _image.image = _texture;
            });

            _saveButton.clicked += OnSave;
            _resetButton.clicked += OnReset;
            _closeButton.clicked += OnClose;
        }

        private void OnDisable()
        {
            _saveButton.clicked -= OnSave;
            _resetButton.clicked -= OnReset;
            _closeButton.clicked -= OnClose;

            _inputField.UnregisterCallback<MouseDownEvent>(OnSignaturePadMouseDown);
            _inputField.UnregisterCallback<MouseMoveEvent>(OnSignaturePadMouseMove);
            _inputField.UnregisterCallback<MouseUpEvent>(OnSignaturePadMouseUp);
        }

        public void Show(bool value = true)
        {
            if (value && _image == null)
            {
                _image = new Image
                {
                    style =
                    {
                        width = new StyleLength(new Length(100, LengthUnit.Percent)),
                        height = new StyleLength(new Length(100, LengthUnit.Percent)),
                    }
                };
                _inputField.Add(_image);
            }

            _container.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnSave()
        {
            var texture = CreateTexture();
            var whiteTexture = new Texture2D(_texture.width, _texture.height);
            var whitePixels = whiteTexture.GetPixels32();
            for (var i = 0; i < whitePixels.Length; i++) whitePixels[i] = Color.white;
            whiteTexture.SetPixels32(whitePixels);
            whiteTexture.Apply();

            SigningComplete = ComputeTextureHash(_texture) != ComputeTextureHash(texture);

            if (SigningComplete)
            {
                var signatureBase64 = Convert.ToBase64String(_texture.EncodeToPNG());
                StartCoroutine(GenerateSignedDocumentCoroutine(signatureBase64));

                Show(false);
                SignupController.Instance.Show(false);

                // 🎯 Random scene flow
                PrepareSceneQueue();
                LoadNextScene();
            }
        }

        private void OnReset()
        {
            _points.Clear();
            SigningComplete = false;

            _texture = CreateTexture();
            _image.image = _texture;
        }

        private void OnClose()
        {
            Show(false);
        }

        private void OnSignaturePadMouseDown(MouseDownEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse) return;

            _isMouseDown = true;
            _points.Clear();
            _points.Add(e.localMousePosition);
        }

        private void OnSignaturePadMouseMove(MouseMoveEvent e)
        {
            if (!_isMouseDown) return;

            _points.Add(e.localMousePosition);
            Draw();
        }

        private void OnSignaturePadMouseUp(MouseUpEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse) return;

            _isMouseDown = false;
        }

        private void Draw()
        {
            var pixels = _texture.GetPixels32();

            for (var i = 0; i < _points.Count - 1; i++)
            {
                var p1 = _points[i];
                var p2 = _points[i + 1];

                var pixel1 = PointToPixel(p1);
                var pixel2 = PointToPixel(p2);

                DrawLine(pixel1, pixel2, pixels, _texture.width, _texture.height, Color.black);
            }

            _texture.SetPixels32(pixels);
            _texture.Apply();
        }

        private Vector2Int PointToPixel(Vector2 uiPosition)
        {
            if (_image == null) return Vector2Int.zero;

            var scaledWidth = _image.layout.width * _image.transform.scale.x;
            var scaledHeight = _image.layout.height * _image.transform.scale.y;

            var pivotOffsetX = (_image.transform.position.x + _image.layout.width / 2f) * _image.transform.scale.x;
            var pivotOffsetY = (_image.transform.position.y + _image.layout.height / 2f) * _image.transform.scale.y;

            var adjustedLocalPos = new Vector2(
                uiPosition.x + _image.layout.width / 2f - pivotOffsetX,
                uiPosition.y + _image.layout.height / 2f - pivotOffsetY
            );

            var textureX = adjustedLocalPos.x / scaledWidth * _texture.width;
            var textureY = adjustedLocalPos.y / scaledHeight * _texture.height;

            return new Vector2Int(Mathf.RoundToInt(textureX), Mathf.RoundToInt(textureY));
        }

        private static void DrawLine(Vector2Int p1, Vector2Int p2, Color32[] pixels, int width, int height, Color color)
        {
            const int penWidth = 2;

            var x0 = p1.x;
            var y0 = height - 1 - p1.y;

            var x1 = p2.x;
            var y1 = height - 1 - p2.y;

            var dx = Mathf.Abs(x1 - x0);
            var dy = Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                for (var x = -penWidth / 2; x <= penWidth / 2; x++)
                {
                    for (var y = -penWidth / 2; y <= penWidth / 2; y++)
                    {
                        var drawX = x0 + x;
                        var drawY = y0 + y;
                        if (drawX >= 0 && drawX < width && drawY >= 0 && drawY < height)
                        {
                            pixels[drawY * width + drawX] = color;
                        }
                    }
                }

                if (x0 == x1 && y0 == y1)
                    break;

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private Texture2D CreateTexture()
        {
            return new Texture2D(Mathf.RoundToInt(InputFieldWidth), Mathf.RoundToInt(InputFieldHeight));
        }

        private void ClearSignature(Image signatureImage)
        {
            if (!_texture || _texture.width <= 0 || _texture.height <= 0)
            {
                Debug.LogError($"signatureTexture dimensions are invalid: width={_texture.width}, height={_texture.height}");
            }
            else
            {
                var pixels = new Color32[_texture.width * _texture.height];
                for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

                _texture.SetPixels32(pixels);
                _texture.Apply();
                signatureImage.image = _texture;
                _points.Clear();
            }
        }

        private IEnumerator GenerateSignedDocumentCoroutine(string signature)
        {
            var owner = SignupController.Instance.AccountOwner;

            var payload = new DocumentPayload
            {
                name = owner.FullName,
                email = owner.Email,

                // Handwritten
                signature = signature,
                date = DateTime.Now.ToString("dd-MM-yyyy"),

                // Reuse handwritten signature/date for audio
                audioSignature = signature,
                audioDate = DateTime.Now.ToString("dd-MM-yyyy"),

                // Reuse handwritten signature/date for video
                videoSignature = signature,
                videoDate = DateTime.Now.ToString("dd-MM-yyyy")
            };



            string jsonData = JsonUtility.ToJson(payload);

            var request = new UnityWebRequest(BaseUrl + "/document-generation", "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"❌ HTTP Error {request.responseCode}:\n{request.downloadHandler.text}");
            }
            else
            {
                Debug.Log("✅ PDF request successful:\n" + request.downloadHandler.text);

                var response = JsonUtility.FromJson<ServerResponse>(request.downloadHandler.text);
                if (response != null && !string.IsNullOrEmpty(response.downloadUrl))
                {
                    Debug.Log("📄 Consent form URL: " + response.downloadUrl);

                    string safeEmail = owner.Email.Replace("@", "_at_").Replace(".", "_");
                    string fileName = $"consent_{safeEmail}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                    StartCoroutine(DownloadAndSavePdf(response.downloadUrl, fileName));
                }
                else
                {
                    Debug.LogWarning("⚠️ Server response missing downloadUrl");
                }
            }
        }

        private IEnumerator DownloadAndSavePdf(string url, string fileName)
        {
            using (UnityWebRequest pdfRequest = UnityWebRequest.Get(url))
            {
                yield return pdfRequest.SendWebRequest();

                if (pdfRequest.result == UnityWebRequest.Result.ConnectionError ||
                    pdfRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("❌ Failed to download PDF: " + pdfRequest.error);
                }
                else
                {
                    byte[] pdfData = pdfRequest.downloadHandler.data;
                    string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                    System.IO.File.WriteAllBytes(filePath, pdfData);

                    Debug.Log("✅ Consent PDF saved locally at: " + filePath);

#if UNITY_ANDROID || UNITY_IOS
                    Application.OpenURL(filePath);
#elif UNITY_EDITOR || UNITY_STANDALONE
                    Application.OpenURL("file:///" + filePath);
#endif
                }
            }
        }

        [Serializable]
        private class ServerResponse
        {
            public string message;
            public string downloadUrl;
        }

        private static uint ComputeTextureHash(Texture2D texture)
        {
            if (!texture) return 0;

            var pixels = texture.GetPixels32();
            uint hash = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                hash = hash * 37 + (uint)(pixel.r + (pixel.g << 8) + (pixel.b << 16) + (pixel.a << 24) + i);
            }

            return hash;
        }

        private readonly Queue<string> _sceneQueue = new();

        private void PrepareSceneQueue()
        {
            _sceneQueue.Clear();

            List<string> allScenes = new()
            {
                "BloodPressureScene",
                "DASH",
                "Exercise",
                "Medication&Compliance",
                "Quiz"
            };

            for (int i = 0; i < 2; i++)
            {
                int index = UnityEngine.Random.Range(0, allScenes.Count);
                _sceneQueue.Enqueue(allScenes[index]);
                allScenes.RemoveAt(index);
            }

            _sceneQueue.Enqueue("PostSurvey");
        }

        public static void TriggerNextScene()
        {
            if (Instance != null)
            {
                Instance.OnSceneTaskComplete();
            }
            else
            {
                Debug.LogError("SignaturePad.Instance not found!");
            }
        }

        public void OnSceneTaskComplete()
        {
            LoadNextScene();
        }

private void LoadNextScene()
        {
            if (_sceneQueue.Count > 0)
            {
                string next = _sceneQueue.Dequeue();
                string currentScene = SceneManager.GetActiveScene().name;

                if (currentScene == "Quiz")
                {
                    StartCoroutine(LoadSceneWithDelay(next, 0f));
                }
                else
                {
                    Debug.Log("Loading next scene: " + next);
                    SceneManager.LoadScene(next, LoadSceneMode.Single);
                }
            }
            else
            {
                Debug.Log("Queue empty. Returning to LoginScene.");
                SceneManager.LoadScene("LoginScene", LoadSceneMode.Single);
            }
        }

        private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
        {
            Debug.Log($"⏳ Waiting {delay} seconds before moving to {sceneName}...");
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }











        private new void Awake()
        {
            base.Awake();

            if (transform.parent != null)
            {
                GameObject root = new GameObject("PersistentSignaturePadRoot");
                transform.SetParent(root.transform);
                DontDestroyOnLoad(root);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
