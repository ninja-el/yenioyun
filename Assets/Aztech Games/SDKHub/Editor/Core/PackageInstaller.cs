using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace SDKHub.Editor
{
    public static class PackageInstaller
    {
        const string QueueKey = "SDKHub.InstallQueue";

        static readonly HashSet<string> Installed = new HashSet<string>();
        static List<KeyValuePair<string, string>> queue;
        static Request current;
        static string busyLabel;
        static string busyName;
        static Action<Request> onComplete;

        public static event Action Changed;

        public static bool HasLoaded { get; private set; }
        public static string LastError { get; private set; }
        public static bool IsBusy => current != null;
        public static int QueueCount => Queue.Count;
        public static bool IsWorking => IsBusy || Queue.Count > 0;
        public static string BusyLabel => IsBusy ? busyLabel : "Preparing next package...";

        static List<KeyValuePair<string, string>> Queue
        {
            get
            {
                if (queue == null) queue = ReadQueue();
                return queue;
            }
        }

        public static bool IsInstalled(string packageName)
        {
            return !string.IsNullOrEmpty(packageName) && Installed.Contains(packageName);
        }

        public static bool IsPending(string packageName)
        {
            if (string.IsNullOrEmpty(packageName)) return false;
            if (IsBusy && busyName == packageName) return true;
            foreach (var item in Queue)
            {
                if (item.Key == packageName) return true;
            }
            return false;
        }

        public static void ClearError()
        {
            LastError = null;
        }

        public static void Refresh()
        {
            Refresh(null);
        }

        public static void Refresh(Action then)
        {
            if (IsBusy) return;
            Run(Client.List(true, true), "Checking packages", null, request =>
            {
                if (request.Status == StatusCode.Success)
                {
                    Installed.Clear();
                    foreach (var package in ((ListRequest)request).Result) Installed.Add(package.name);
                    HasLoaded = true;
                }
                then?.Invoke();
            });
        }

        public static void InstallBatch(IEnumerable<KeyValuePair<string, string>> items)
        {
            LastError = null;
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Key) || string.IsNullOrEmpty(item.Value)) continue;
                if (Installed.Contains(item.Key) || Queue.Exists(q => q.Key == item.Key)) continue;
                Queue.Add(item);
            }
            SaveQueue();
            ProcessQueue();
        }

        public static void ResumeQueue()
        {
            ProcessQueue();
        }

        public static void Remove(string packageName)
        {
            if (IsWorking || string.IsNullOrEmpty(packageName)) return;
            LastError = null;
            Run(Client.Remove(packageName), "Removing " + packageName, packageName, request =>
            {
                if (request.Status == StatusCode.Success) Refresh();
            });
        }

        public static void ImportPackageFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.ImportPackage(path, true);
        }

        static void ProcessQueue()
        {
            if (IsBusy) return;

            while (Queue.Count > 0 && Installed.Contains(Queue[0].Key)) Queue.RemoveAt(0);
            if (Queue.Count == 0)
            {
                SaveQueue();
                RaiseChanged();
                return;
            }

            var next = Queue[0];
            Queue.RemoveAt(0);
            SaveQueue();

            Run(Client.Add(next.Value), "Installing " + next.Key, next.Key, request =>
            {
                if (request.Status == StatusCode.Success)
                {
                    Refresh(ProcessQueue);
                }
                else
                {
                    Queue.Clear();
                    SaveQueue();
                }
            });
        }

        static void Run(Request request, string label, string packageName, Action<Request> callback)
        {
            current = request;
            busyLabel = label;
            busyName = packageName;
            onComplete = callback;
            EditorApplication.update += Poll;
            RaiseChanged();
        }

        static void Poll()
        {
            if (current == null || !current.IsCompleted) return;

            EditorApplication.update -= Poll;
            var request = current;
            var callback = onComplete;
            var label = busyLabel;
            current = null;
            busyName = null;
            onComplete = null;

            if (request.Status == StatusCode.Failure)
            {
                LastError = $"{label} failed: {request.Error.message}";
                Debug.LogError("[SDK Hub] " + LastError);
            }

            callback?.Invoke(request);
            RaiseChanged();
        }

        static void RaiseChanged()
        {
            SdkHubState.Bump();
            Changed?.Invoke();
        }

        static List<KeyValuePair<string, string>> ReadQueue()
        {
            var result = new List<KeyValuePair<string, string>>();
            var raw = SessionState.GetString(QueueKey, "");
            foreach (var line in raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var split = line.IndexOf('|');
                if (split <= 0) continue;
                result.Add(new KeyValuePair<string, string>(line.Substring(0, split), line.Substring(split + 1)));
            }
            return result;
        }

        static void SaveQueue()
        {
            var builder = new StringBuilder();
            foreach (var item in Queue) builder.Append(item.Key).Append('|').Append(item.Value).Append('\n');
            SessionState.SetString(QueueKey, builder.ToString());
        }
    }
}
