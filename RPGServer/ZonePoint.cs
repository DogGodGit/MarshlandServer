using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaGeometry;

namespace MainServer
{
    class ZonePoint
    {

         #region variables
        /// <summary>
        /// the position of the spawn Point
        /// </summary>
        Vector3 m_position;
        /// <summary>
        /// The random distance from the spawn Point That players can appear 
        /// </summary>
        float m_radius;
        /// <summary>
        /// The Zone the point is in
        /// </summary>
        int m_zoneID;
        /// <summary>
        /// The unique ID used to identify the zone Point
        /// </summary>
        int m_zonePointID;
        /// <summary>
        /// The zone that this point links to
        /// </summary>
        int m_adjoiningZoneID;
        /// <summary>
        /// The unique ID used to identify the spawn Point
        /// that this point links to
        /// </summary>
        int m_adjoiningSpawnPointID;
        int m_min_level;
        int m_quest_completed;
        #endregion
        #region properties
        /// <summary>
        /// the position of the spawn Point
        /// </summary
        public Vector3 Position
        {
            set { m_position = value; }
            get { return m_position; }
        }
        /// <summary>
        /// The random distance from the spawn Point That players can appear 
        /// </summary>
        public float Radius
        {
            set { m_radius = value; }
            get { return m_radius; }
        }
        /// <summary>
        /// The Zone the point is in
        /// </summary>
        public int ZoneID
        {
            set { m_zoneID = value; }
        }
        /// <summary>
        /// The unique ID used to identify the zone Point
        /// </summary>
        public int ZonePointID
        {
            set { m_zonePointID = value; }
            get { return m_zonePointID; }
        }
        /// <summary>
        /// The zone that this point links to
        /// </summary>
        public int AdjoiningZoneID
        {
            set { m_adjoiningZoneID = value; }
            get { return m_adjoiningZoneID; }
        }
        /// <summary>
        /// The unique ID used to identify the spawn Point
        /// that this point links to
        /// </summary>
        public int AdjoiningSpawnPointID
        {
            set { m_adjoiningSpawnPointID = value; }
            get { return m_adjoiningSpawnPointID; }
        }


        public int MinLevel
        {
            set { m_min_level = value; }
            get { return m_min_level; }
        }

        public int QuestCompleted
        {
            get { return m_quest_completed; }
        }
        #endregion

        #region constructors
        public ZonePoint(Vector3 position, float radius, int zoneID,int zonePointID, int adjoiningZoneID, int adjoiningSpawnPointID,int min_level,int quest_completed)
        {
            Position = position;
            Radius = radius;
            ZoneID = zoneID;
            ZonePointID = zonePointID;
            AdjoiningZoneID = adjoiningZoneID;
            AdjoiningSpawnPointID = adjoiningSpawnPointID;
            MinLevel = min_level;
            m_quest_completed = quest_completed;
        }

        #endregion
    }
}
