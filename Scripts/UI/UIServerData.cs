using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace SimpleServerListingSDK.UI
{
    public class UIServerData : MonoBehaviour
    {
        public UIServerList list;
        public ServerData serverData;
        public Text textTitle;
        public Text textDescription;
        public Text textMap;
        [FormerlySerializedAs("formatPlayer")]
        public string formatPlayersCount = "{0}/{1}";
        [FormerlySerializedAs("textPlayer")]
        public Text textPlayersCount;
        public Text textPlayersCountFromAllServers;

        private void Update()
        {
            if (textTitle) textTitle.text = serverData.title;
            if (textDescription) textDescription.text = serverData.description;
            if (textMap) textMap.text = serverData.map;
            if (textPlayersCount) textPlayersCount.text = string.Format(formatPlayersCount, serverData.currentPlayer, serverData.maxPlayer);
            if (textPlayersCountFromAllServers) textPlayersCountFromAllServers.text = list.PlayersCountFromAllServers.ToString("N0");
        }
    }
}
