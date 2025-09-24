using UnityEngine;
using UnityEngine.UI;

public class UIScroller : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public ScrollRect scrollRect;   // Drag your Scroll View here
    public Button buttonScrollUp;
    public Button buttonScrollDown;

    [Header("Scroll Settings")]
    public float scrollStep = 0.1f; // how much to move per click (0 = top, 1 = bottom)
    public float scrollSpeed = 5f;  // smooth scroll speed

    private float targetScrollPos;

    void Start()
    {
        // Start at top
        targetScrollPos = 1f;
        scrollRect.verticalNormalizedPosition = targetScrollPos;

        if (buttonScrollUp != null)
            buttonScrollUp.onClick.AddListener(ScrollUp);

        if (buttonScrollDown != null)
            buttonScrollDown.onClick.AddListener(ScrollDown);
    }

    void Update()
    {
        // Smoothly move scroll
        scrollRect.verticalNormalizedPosition = Mathf.Lerp(
            scrollRect.verticalNormalizedPosition,
            targetScrollPos,
            Time.deltaTime * scrollSpeed
        );

        // Mouse wheel support
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.01f)
        {
            targetScrollPos += wheel;
            targetScrollPos = Mathf.Clamp01(targetScrollPos);
        }
    }

    public void ScrollUp()
    {
        targetScrollPos += scrollStep;
        targetScrollPos = Mathf.Clamp01(targetScrollPos);
    }

    public void ScrollDown()
    {
        targetScrollPos -= scrollStep;
        targetScrollPos = Mathf.Clamp01(targetScrollPos);
    }
}
