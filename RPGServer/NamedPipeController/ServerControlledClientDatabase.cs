namespace MainServer.NamedPipeController
{
    interface IServerControlledClientDatabase
    {
        bool ShouldSpawnServerControlledClient();
    }

    class ServerControlledClientDatabase : IServerControlledClientDatabase
    {
        private readonly SqlQuery m_sqlQuery;

        public ServerControlledClientDatabase(SqlQuery sqlQuery)
        {
            m_sqlQuery = sqlQuery;
        }

        public bool ShouldSpawnServerControlledClient()
        {
            bool shouldSpawn = false;
            m_sqlQuery.ExecuteCommand("Select is_server_controlled from worlds where world_id=" + Program.m_worldID);
            if (m_sqlQuery.Read())
            {
                shouldSpawn = m_sqlQuery.GetBoolean("is_server_controlled");
            }
            m_sqlQuery.CleanUpAfterExecute();
            return shouldSpawn;
        }
    }
}
