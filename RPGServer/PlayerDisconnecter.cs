using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace MainServer
{
    class PlayerDisconnecter
    {
        private readonly CommandProcessor commandProcessor;
        private readonly Dictionary<Player, DateTime> currentlyBackgroundedPlayers;
        private const int BACKGROUND_TIME_DEFAULT = 30;
        private readonly int backgroundTimeLimit;
        private readonly List<Player> forRemoval;
        private DateTime lastUpdateTime;
        private const int updateFrequencyInMilliseconds = 500;

        /// <summary>
        /// Simple class which tracks the background / resume status of individual clients, and disconnects them if they are backgrounded 
        /// beyond a configurable threshold.
        /// </summary>
        /// <param name="commandProcessor"></param>
        public PlayerDisconnecter(CommandProcessor commandProcessor)
        {

            if (ConfigurationManager.AppSettings["ClientBackgroundTimeout"] != null)
                backgroundTimeLimit = int.Parse(ConfigurationManager.AppSettings["ClientBackgroundTimeout"]);
            else
            {
                backgroundTimeLimit = BACKGROUND_TIME_DEFAULT;
            }

            this.commandProcessor = commandProcessor;
            currentlyBackgroundedPlayers = new Dictionary<Player, DateTime>();
            forRemoval = new List<Player>();
        }

        public void PlayerBackground(Player player)
        {
            if (currentlyBackgroundedPlayers.ContainsKey(player))
                Program.Display("Player already backgrounded");
            else
            {
                currentlyBackgroundedPlayers.Add(player, DateTime.Now);
            }
        }

        public void PlayerResume(Player player)
        {
            if (currentlyBackgroundedPlayers.ContainsKey(player))
                currentlyBackgroundedPlayers.Remove(player);
            else
            {
                Program.Display("Player already resumed");
            }
        }

		public void PlayerRemove(Player player)
		{
			if (currentlyBackgroundedPlayers.ContainsKey(player))
			{
				currentlyBackgroundedPlayers.Remove(player);
			}
		}

        public bool PlayerIsBackgrounded(Player player)
        {
            if (player == null)
            {
                return false;
            }

            if (currentlyBackgroundedPlayers.ContainsKey(player))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

		public void Update(DateTime nowTime)
        {
            if (currentlyBackgroundedPlayers.Count == 0)
                return;

            TimeSpan timeSinceLastUpdate = nowTime - lastUpdateTime;
            if(timeSinceLastUpdate.TotalMilliseconds < updateFrequencyInMilliseconds)
                return;
            

            foreach (KeyValuePair<Player, DateTime> currentlyBackgroundedPlayer in currentlyBackgroundedPlayers)
            {
                TimeSpan timeSinceBackgrounding = nowTime - currentlyBackgroundedPlayer.Value;

                if (timeSinceBackgrounding.TotalSeconds > backgroundTimeLimit)
                {
                    forRemoval.Add(currentlyBackgroundedPlayer.Key);
                }

            }

            RemoveTimedoutBackgroundedPlayers();

            lastUpdateTime = nowTime;
        }

        private void RemoveTimedoutBackgroundedPlayers()
        {
            foreach (Player player in forRemoval)
            {
                commandProcessor.disconnect(player, true, String.Empty);
                Program.Display("Player removed during client background");
                if (currentlyBackgroundedPlayers.ContainsKey(player))
                    currentlyBackgroundedPlayers.Remove(player);
            }

            forRemoval.Clear();
        }
    }
}
