using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaGeometry;

namespace MainServer
{
    class PlayerSpawnPoint
    {
        #region variables
        /// <summary>
        /// the position of the spawn Point
        /// </summary>
        Vector3 m_position;
        /// <summary>
        /// The random distance from the spawn Point That players can appear 
        /// </summary>
        float m_respawnRadius;
        /// <summary>
        /// if the point can be used as a respawn Point
        /// </summary>
        bool m_respawnPoint=true;
        /// <summary>
        /// if the point can be used as a teleport Point
        /// </summary>
        bool m_teleportPoint;
        /// <summary>
        /// The Zone the spawn point is in
        /// </summary>
        int m_zoneID;
        /// <summary>
        /// The unique ID used to identify the spawn Point
        /// </summary>
        int m_spawnPointID;
        /// <summary>
        /// The angle The Player faces when they are moved to the spawn point
        /// </summary>
        float m_angle = 0;

        bool m_free_travel=false;
        #endregion
        #region properties
        public Vector3 Position
        {
            set { m_position = value; }
            get { return m_position; }
        }
        public float RespawnRadius
        {
            set { m_respawnRadius = value; }
        }
        public Vector3 RandomRespawnPosition
        {
            get
            {
                double angle = 2f * Math.PI * Program.m_rand.NextDouble();
                double distance = Program.m_rand.NextDouble() * m_respawnRadius;

                return new Vector3(m_position.X + distance * Math.Cos(angle), m_position.Y, m_position.Z + distance * Math.Sin(angle));
            }
        }

        /// <summary>
        /// true if the point can be used as a respawn Point
        /// </summary>
        public bool RespawnPoint
        {
            get { return m_respawnPoint; }
        }
        /// <summary>
        /// true if the point can be used as a teleport Point
        /// </summary>
        public bool TeleportPoint
        {
            set { m_teleportPoint = value; }
            get { return m_teleportPoint; }
        }
        public int ZoneID
        {
            set { m_zoneID = value; }
            get { return m_zoneID; }
        }
        public int SpawnPointID
        {
            set { m_spawnPointID = value; }
            get { return m_spawnPointID; }
        }
        /// <summary>
        /// The angle The Player faces when they are moved to the spawn point
        /// </summary>
        public float Angle
        {
            get { return m_angle; }
        }
        public bool FreeTravel
        {
            get { return m_free_travel; }
        }
        #endregion

        #region constructors
        public PlayerSpawnPoint(Vector3 position, float respawnRadius,  int zoneID, int spawnPointID, bool teleportPoint,float angle, bool respawnPoint,bool freeTravel)
        {
            Position = position;
            RespawnRadius = respawnRadius;
            ZoneID = zoneID;
            SpawnPointID = spawnPointID;
            TeleportPoint = teleportPoint;
            m_angle = angle;
            m_respawnPoint = respawnPoint;
            m_free_travel = freeTravel;
        }
        #endregion
    }
}
