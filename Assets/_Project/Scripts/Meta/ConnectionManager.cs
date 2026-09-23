using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ConnectionManager : MonoBehaviour
{ 
    public bool IsConnected { get; private set; } = false;

    public IEnumerator TestInternetConnection()
    {
        IsConnected = false;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("Offline");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Head("https://www.google.com"))
        {
            request.timeout = 3;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("Offline");
            }
            else
            {
                IsConnected = true;
                Debug.Log("Internet Connected");
            }
        }
    }
}