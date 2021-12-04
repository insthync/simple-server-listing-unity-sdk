using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleServerListingSDK.UI
{
    public class UIServerList : MonoBehaviour
    {
        public Transform container;
        public UIServerData uiPrefab;
        public GameObject noServerState;
        public float updateInterval = 1f;
        private float intervalCountDown;

        private void Update()
        {
            if (intervalCountDown > 0)
                intervalCountDown -= Time.deltaTime;
            if (intervalCountDown <= 0)
            {
                intervalCountDown = updateInterval;
                GetList();
            }
        }

        public async void GetList()
        {
            var list = await ServerListingManager.Instance.List();
            for (var i = container.childCount - 1; i >= 0; --i)
            {
                if (container.GetChild(i) != null && container.GetChild(i).gameObject != null)
                {
                    Destroy(container.GetChild(i).gameObject);
                }
            }
            foreach (var data in list)
            {
                if (uiPrefab != null && container != null)
                {
                    var newUI = Instantiate(uiPrefab, container);
                    newUI.serverData = data;
                }
            }
            if (noServerState != null)
                noServerState.SetActive(list.Count == 0);
        }
    }
}
