using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleServerListingSDK.UI
{
    public class UIServerList : MonoBehaviour
    {
        public Transform container;
        public UIServerData uiPrefab;
        public GameObject noServerState;
        public float updateInterval = 1f;
        public bool sortByTitle = true;
        public bool sortDescendy;
        public List<string> filterTitles = new List<string>();
        public List<string> filterMaps = new List<string>();
        private float intervalCountDown;
        public int PlayersCountFromAllServers { get; private set; }

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
            if (container != null)
            {
                for (var i = container.childCount - 1; i >= 0; --i)
                {
                    if (container.GetChild(i) != null && container.GetChild(i).gameObject != null)
                    {
                        Destroy(container.GetChild(i).gameObject);
                    }
                }
            }
            if (sortByTitle)
            {
                if (sortDescendy)
                    list = list.OrderByDescending(o => o.title).ToList();
                else
                    list = list.OrderBy(o => o.title).ToList();
            }
            PlayersCountFromAllServers = 0;
            foreach (var data in list)
            {
                if (uiPrefab != null && container != null &&
                    (filterTitles.Count == 0 || filterTitles.Where(o => o.ToLower().Trim().Contains(data.title.ToLower().Trim())).Count() > 0) &&
                    (filterMaps.Count == 0 || filterMaps.Where(o => o.ToLower().Trim().Contains(data.map.ToLower().Trim())).Count() > 0))
                {
                    var newUI = Instantiate(uiPrefab, container);
                    newUI.list = this;
                    newUI.serverData = data;
                }
                PlayersCountFromAllServers += data.currentPlayer;
            }
            if (noServerState != null)
                noServerState.SetActive(container.childCount == 0);
        }
    }
}
