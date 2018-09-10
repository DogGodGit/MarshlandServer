using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer.Pathing
{
    class ASNode
    {
        /// <summary>
        /// TheTriangle on the map that this Node Represents
        /// </summary>
        ASTriangle m_tri = null;
        /// <summary>
        /// The Triangle that led onto the current tri
        /// </summary>
        ASTriangle m_previousTri = null;

        /// <summary>
        ///  the estimated Cost to the finnish
        /// </summary>
        float m_heuristic;
        /// <summary>
        /// The Lowest cost to reach this node
        /// </summary>
        float m_cost;

        internal float TotalCost
        {
            get { return m_cost + m_heuristic; }
        }
    }
}
