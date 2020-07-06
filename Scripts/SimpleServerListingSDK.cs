using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SimpleServerListingSDK
{
    public class SimpleServerListingSDK : MonoBehaviour
    {
        private static SimpleServerListingSDK instance;
        public static SimpleServerListingSDK Instance
        {
            get
            {
                if (!instance)
                    new GameObject("_SimpleServerListingSDK").AddComponent<SimpleServerListingSDK>();
                return instance;
            }
        }

        public float healthInterval = 1f;
        public string serviceAddress = "http://localhost:8000";

        public string ServerId { get; private set; }
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            HealthCheckAsync();
        }

        private void OnDestroy()
        {
            cancellationTokenSource.Dispose();
        }

        private void OnApplicationQuit()
        {
            cancellationTokenSource.Dispose();
        }

        public async Task<bool> Connect(ServerData connectServerData)
        {
            var result = await SendRequestAsync("/connect", JsonUtility.ToJson(connectServerData));
            if (result.isPass)
            {
                ServerDataResult serverDataResult = JsonUtility.FromJson<ServerDataResult>(result.body);
                ServerId = serverDataResult.gameServer.id;
                return true;
            }
            return false;
        }

        public async Task<bool> Health()
        {
            if (string.IsNullOrEmpty(ServerId))
                return false;
            return await SendRequestAsync("/health", $"{{\"id\":\"{ServerId}\"}}").ContinueWith(task => task.Result.isPass);
        }

        public async Task<bool> Update(ServerData updateServerData)
        {
            return await SendRequestAsync("/update", JsonUtility.ToJson(updateServerData), UnityWebRequest.kHttpVerbPUT).ContinueWith(task => task.Result.isPass);
        }

        public async Task<bool> ShutDown()
        {
            var result = await SendRequestAsync("/shutdown", "{}");
            if (result.isPass)
            {
                ServerId = string.Empty;
                return true;
            }
            return false;
        }

        private async void HealthCheckAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(healthInterval));

                try
                {
                    await Health();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private struct RequestResult
        {
            public bool isPass;
            public long statusCode;
            public string body;
        }

        private async Task<RequestResult>  SendRequestAsync(string api, string json, string method = UnityWebRequest.kHttpVerbPOST)
        {
            // To prevent that an async method leaks after destroying this gameObject.
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            var req = new UnityWebRequest(serviceAddress + api, method)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            await new WebRequestAsyncWrapper(req.SendWebRequest());
            return new RequestResult()
            {
                isPass = req.responseCode == (long)HttpStatusCode.OK,
                statusCode = req.responseCode,
                body = req.downloadHandler.text
            };
        }

        private class WebRequestAsyncWrapper
        {
            public UnityWebRequestAsyncOperation AsyncOp { get; }
            public WebRequestAsyncWrapper(UnityWebRequestAsyncOperation unityOp)
            {
                AsyncOp = unityOp;
            }

            public WebRequestAsyncAwaiter GetAwaiter()
            {
                return new WebRequestAsyncAwaiter(this);
            }
        }

        private class WebRequestAsyncAwaiter : INotifyCompletion
        {
            private UnityWebRequestAsyncOperation asyncOp;
            private Action continuation;
            public bool IsCompleted { get { return asyncOp.isDone; } }

            public WebRequestAsyncAwaiter(WebRequestAsyncWrapper wrapper)
            {
                asyncOp = wrapper.AsyncOp;
                asyncOp.completed += OnRequestCompleted;
            }

            public void GetResult()
            {
                asyncOp.completed -= OnRequestCompleted;
            }

            public void OnCompleted(Action continuation)
            {
                this.continuation = continuation;
            }

            private void OnRequestCompleted(AsyncOperation op)
            {
                continuation?.Invoke();
                continuation = null;
            }
        }
    }
}
