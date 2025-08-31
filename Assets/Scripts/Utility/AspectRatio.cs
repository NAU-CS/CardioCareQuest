using Gameplay.UI;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Sets the aspect ratio of the camera to 16:9.
/// </summary>
public class AspectRatio : MonoBehaviour
{
    
    [SerializeField] private PanelSettings _panelSettings;
    
    private void Start()
    {
        Adjust();
    }

    private void Adjust()
    {
        var targetAspect = 16.0f / 9.0f;
        var windowAspect = (float)Screen.width / (float)Screen.height;
        var scaleHeight = windowAspect / targetAspect;

        Camera camera = GetComponent<Camera>();

        if(_panelSettings != null)
            _panelSettings.match = scaleHeight < 1.0f ? 0 : 1;
        
        if (scaleHeight < 1.0f)
        {
            // Adjust the camera's height
            var rect = camera.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            camera.rect = rect;
        }
        else
        {
            // Adjust the camera's width
            var scaleWidth = 1.0f / scaleHeight;
            
            var rect = camera.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            camera.rect = rect;
        }
    }
}
