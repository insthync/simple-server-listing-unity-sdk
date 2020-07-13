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
    public class ServerListingManager : MonoBehaviour
    {
        private static ServerListingManager instance;
        public static ServerListingManager Instance
        {
            get
            {
                if (!instance)
                    new GameObject("_SimpleServerListingSDK").AddComponent<ServerListingManager>();
                return instance;
            }
        }

        public float healthInterval = 1f;
        public float reconnectInterval = 1f;
        public string serviceAddress = "http://localhost:8000";

        public bool IsConnected { get { return !string.IsNullOrEmpty(ServerId); } }
        public string ServerId { get; private set; }
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private bool connecting;
        private ServerData connectingData;

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

        private async void OnDestroy()
        {
            await ShutDown();
            cancellationTokenSource.Dispose();
        }

        private async void OnApplicationQuit()
        {
            await ShutDown();
            cancellationTokenSource.Dispose();
        }

        public async Task<List<ServerData>> List()
        {
            var result = await SendRequestAsync("/", "{}", UnityWebRequest.kHttpVerbGET);
            if (result.isPass)
            {
                ServerListResult serverDataResult = JsonUtility.FromJson<ServerListResult>(result.body);
                return serverDataResult.gameServers;
            }
            return new List<ServerData>();
        }

        public bool Connect(ServerData connectServerData)
        {
            if (connecting)
                return false;
            connecting = true;
            connectingData = connectServerData;
            ConnectAsync();
            return true;
        }

        public async Task<bool> Health()
        {
            if (!IsConnected)
                return false;
            var result = await SendRequestAsync("/health", $"{{\"id\":\"{ServerId}\"}}");
            if (result.isPass)
                return true;
            if (result.statusCode == (long)HttpStatusCode.NotFound)
            {
                // It may timedout from the server, so try to reconnect
                ServerId = string.Empty;
                connecting = true;
                ConnectAsync();
            }
            return false;
        }

        public async Task<bool> UpdateInfo(ServerData updateServerData)
        {
            if (!IsConnected)
                return false;
            updateServerData.id = ServerId;
            return await SendRequestAsync("/update", JsonUtility.ToJson(updateServerData), UnityWebRequest.kHttpVerbPUT).ContinueWith(task => task.Result.isPass);
        }

        public async Task<bool> ShutDown()
        {
            connecting = false;
            if (!IsConnected)
                return false;
            ServerId = string.Empty;
            await SendRequestAsync("/shutdown", $"{{\"id\":\"{ServerId}\"}}");
            return true;
        }

        private async void ConnectAsync()
        {
            while (connecting)
            {
                RequestResult result;
                try
                {
                    result = await SendRequestAsync("/connect", JsonUtility.ToJson(connectingData));
                }
                catch (ObjectDisposedException)
                {
                    // Game object destroyed
                    break;
                }
                if (result.isPass)
                {
                    // Connected then break from reconnect loop
                    ServerDataResult serverDataResult = JsonUtility.FromJson<ServerDataResult>(result.body);
                    ServerId = serverDataResult.gameServer.id;
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(reconnectInterval));
            }
        }

        private async void HealthCheckAsync()
        {
            while (true)
            {
                try
                {
                    await Health();
                }
                catch (ObjectDisposedException)
                {
                    // Game object destroyed
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(healthInterval));
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
            if (req.responseCode != (long)HttpStatusCode.OK)
                Debug.LogError("Error occurs when call [" + api + "] : " + req.error);
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
