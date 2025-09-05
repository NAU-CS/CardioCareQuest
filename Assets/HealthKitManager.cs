using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class HealthKitManager : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RequestHealthKitAuthorization();

    [DllImport("__Internal")]
    private static extern void FetchLatestBloodPressure(Action<double, double> callback);
#else
    // Stubs for testing in editor
    private static void RequestHealthKitAuthorization() { Debug.Log("RequestHealthKitAuthorization called (Editor)"); }
    private static void FetchLatestBloodPressure(Action<double, double> callback) { callback(120, 80); }
#endif

    public void AuthorizeHealthKit()
    {
        RequestHealthKitAuthorization();
        Debug.Log("Requested HealthKit Authorization");
    }

    public void GetLatestBloodPressure()
    {
        FetchLatestBloodPressure((systolic, diastolic) => {
            Debug.Log($"Latest BP: {systolic}/{diastolic}");
        });
    }
}
