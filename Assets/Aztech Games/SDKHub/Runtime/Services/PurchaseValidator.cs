using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SDKHub
{
    // Sends purchase receipts to your own server before the purchase is confirmed.
    public static class PurchaseValidator
    {
        [Serializable]
        class ValidationRequest
        {
            public string productId;
            public string platform;
            public string receipt;
        }

        [Serializable]
        class ValidationResponse
        {
            public bool valid;
        }

        public static bool IsEnabled
        {
            get
            {
                var config = SdkHubConfig.Instance;
                return config != null
                    && config.Get("validation", "enabled") == "1"
                    && !string.IsNullOrEmpty(config.Get("validation", "url"));
            }
        }

        public static Task<bool> ValidateAsync(string productId, string receipt)
        {
            var source = new TaskCompletionSource<bool>();
            if (!IsEnabled)
            {
                source.SetResult(true);
                return source.Task;
            }

            var config = SdkHubConfig.Instance;
            var body = JsonUtility.ToJson(new ValidationRequest
            {
                productId = productId,
                platform = SdkPlatformInfo.Current.ToString(),
                receipt = receipt
            });

            var request = new UnityWebRequest(config.Get("validation", "url"), "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 15
            };
            request.SetRequestHeader("Content-Type", "application/json");
            var authorization = config.Get("validation", "authHeader");
            if (!string.IsNullOrEmpty(authorization)) request.SetRequestHeader("Authorization", authorization);

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                var valid = false;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        valid = JsonUtility.FromJson<ValidationResponse>(request.downloadHandler.text).valid;
                    }
                    catch (ArgumentException)
                    {
                        valid = false;
                    }
                }
                else
                {
                    Debug.LogWarning("[SDK Hub] Purchase validation request failed: " + request.error);
                }
                request.Dispose();
                source.SetResult(valid);
            };
            return source.Task;
        }
    }
}
