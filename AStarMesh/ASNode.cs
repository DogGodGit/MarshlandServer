using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaGeometry;

namespace AStarMesh
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
        ASNode m_previousNode = null;

        /// <summary>
        /// The Line To the  previous node
        /// </summary>
        Line3D m_linkedLine=null;
        /// <summary>
        /// The Point the path enters the triangle
        /// </summary>
        Vector3 m_entryPoint;

        /// <summary>
        ///  the estimated Cost to the finnish
        /// </summary>
        double m_heuristic;
        /// <summary>
        /// The Lowest cost to reach this node
        /// </summary>
        double m_cost;

        internal double TotalCost
        {
            get { return m_cost + m_heuristic; }
        }
        internal ASTriangle Triangle
        {
            get { return m_tri; }
        }
        internal ASNode Previous
        {
            get { return m_previousNode; }
        }
        internal double Heuristic
        {
            get { return m_heuristic; }
        }
        internal double Cost
        {
            get { return m_cost; }
        }
        /// <summary>
        /// The Point the path enters the triangle
        /// </summary>
        internal Vector3 EntryPoint
        {
            get { return m_entryPoint; }
            set { m_entryPoint = value; }
        }
        internal bool LinePassesThroughLinkedLine(Vector3 point0, Vector3 point1, bool useMinT )
        {
            if (m_linkedLine != null)
            {
                double t0 = -1;
                double t1 = -1;
                Vector3 intersection = m_linkedLine.Get2DInterSectionWithLine(point0, point1, ref t0, ref t1);

                double minT = 0;
                if(useMinT==true)
                {
                    minT = m_linkedLine.MinT;
                }
                if ((t0>=(0+minT))&&(t0<=(1-minT)) &&
                    (t1>=0)&&(t1<=1))
                {
                    return true;
                }
            }

            return false;
        }
        internal bool LinePassesThroughLinkedLine(Vector3 point0, Vector3 point1, ref Vector3 intersection)
        {
            if (m_linkedLine != null)
            {
                double t0 = -1;
                double t1 = -1;
                intersection = m_linkedLine.Get2DInterSectionWithLine(point0, point1, ref t0, ref t1);
                if ((t0 >= 0) && (t0 <= 1) &&
                    (t1 >= 0) && (t1 <= 1))
                {
                    return true;
                }
            }

            return false;
        }

        internal ASNode(ASTriangle tri, ASNode previousTri, double cost, double heuristic, Vector3 entryPoint)
        {
            m_tri = tri;
            m_previousNode = previousTri;
            m_cost = cost;
            m_heuristic = heuristic;
            m_entryPoint = entryPoint;
            if (m_tri != null && m_previousNode != null)
            {
                int linkNum = m_tri.GetIndexForTriangle(m_previousNode.Triangle);
                m_linkedLine = m_tri.GetSide(linkNum);
            }
        }
    }
}
