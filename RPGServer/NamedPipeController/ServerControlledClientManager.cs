using System.Threading;

namespace MainServer.NamedPipeController
{
    interface IServerControlledClientManager
    {
        void RecheckSettings();
        void DespawnServerControlledClient();
    }

    class ServerControlledClientManager : IServerControlledClientManager
    {
        private readonly IServerControlledClientDatabase m_serverControlledClientDatabase;
        //Gets messages sent through named pipe from the server controller
        public bool m_spawnServerControlledClient = false;
        public ServerControlledClient m_serverControlledClient;
        public Thread m_serverControlledClientThread;

        public ServerControlledClientManager(IServerControlledClientDatabase serverControlledClientDatabase)
        {
            m_serverControlledClientDatabase = serverControlledClientDatabase;
            SetUp();
        }

        private void SetUp()
        {
            m_spawnServerControlledClient = m_serverControlledClientDatabase.ShouldSpawnServerControlledClient();
            if (m_spawnServerControlledClient)
            {
                SpawnServerControlledClient();
            }
        }

        public void RecheckSettings()
        {
            var spawnSettingChanged = m_serverControlledClientDatabase.ShouldSpawnServerControlledClient();
            if (spawnSettingChanged != m_spawnServerControlledClient)
            {
                m_spawnServerControlledClient = spawnSettingChanged;
                if (spawnSettingChanged)
                {
                    SpawnServerControlledClient();
                }
                else
                {
                    DespawnServerControlledClient();
                }               
            }
        }

        private void SpawnServerControlledClient()
        {
            if (m_serverControlledClientThread != null) return; // don't spawn one if there's already one

            m_serverControlledClientThread = new Thread(ServerControlledClientUpdate);
			m_serverControlledClientThread.Name = "ServerControlledClientThread";
            m_serverControlledClientThread.Start();
        }

        public void DespawnServerControlledClient()
        {
            if (m_serverControlledClientThread != null)
            {
                m_serverControlledClientThread.Abort();
            }

            if (m_serverControlledClient != null)
            {
                m_serverControlledClient.KeepRunning = false;
                m_serverControlledClient = null;
            }
        }

        private void ServerControlledClientUpdate()
        {
            m_serverControlledClient = new ServerControlledClient();
            while (true)
            {
                Thread.Sleep(10000);
                m_serverControlledClient.Update();
            }
        }
    }
}
