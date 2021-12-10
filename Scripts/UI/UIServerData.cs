using UnityEngine;
using UnityEngine.UI;

namespace SimpleServerListingSDK.UI
{
    public class UIServerData : MonoBehaviour
    {
        public ServerData serverData;
        public Text textTitle;
        public Text textDescription;
        public Text textMap;
        public string formatPlayer = "{0}/{1}";
        public Text textPlayer;

        private void Update()
        {
            if (textTitle) textTitle.text = serverData.title;
            if (textDescription) textDescription.text = serverData.description;
            if (textMap) textMap.text = serverData.map;
            if (textPlayer) textPlayer.text = string.Format(formatPlayer, serverData.currentPlayer, serverData.maxPlayer);
        }
    }
}
