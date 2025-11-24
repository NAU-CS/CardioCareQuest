using UnityEngine;
using System.Collections;
using SFB;              // Desktop file browser plugin
using System;

public static class PhotoPicker
{
    /// <summary>
    /// Opens a native file picker (desktop) or gallery picker (mobile)
    /// and returns a local file path to the selected image.
    /// </summary>
    public static IEnumerator PickImage(Action<string> OnResult)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // Desktop – Standalone File Browser
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select a meal photo",
            "",
            extensions,
            false
        );

        if (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            OnResult?.Invoke(paths[0]);
        else
            OnResult?.Invoke(null);

        yield break; // Desktop ends here

#elif UNITY_IOS || UNITY_ANDROID
        // Mobile – NativeGallery
        yield return null; // required by Unity

        NativeGallery.Permission perm = NativeGallery.GetImageFromGallery((path) =>
        {
            if (!string.IsNullOrEmpty(path))
                OnResult?.Invoke(path);
            else
                OnResult?.Invoke(null);

        }, "Select a meal picture");

        if (perm != NativeGallery.Permission.Granted)
            Debug.LogWarning("Permission denied for photos.");
#endif
    }
}
