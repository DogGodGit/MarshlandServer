using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Microsoft.Xna.Framework;

namespace MainServer.Pathing
{

    class Line3D
    {

    };
    class Plane3D
    {

    };

    class ASTriangle
    {
        
        ASTriangle [] m_linkedCells= new ASTriangle[3];

        Vector3[] m_vertices = new Vector3[3];

        Line3D[] m_side = new Line3D[3];
        Plane3D m_plane = null;

        ASTriangle GetLink(int linkNumber)
        {
            if ((linkNumber >= 0) && (linkNumber < m_linkedCells.Length))
            {
                return m_linkedCells[linkNumber];
            }
            return null;
        }

        void SetLink(int linkNumber, ASTriangle newLink)
        {
            if ((linkNumber >= 0) && (linkNumber < m_linkedCells.Length))
            {
                m_linkedCells[linkNumber] = newLink;
            } 
        }

        internal ASTriangle(Vector3 point0, Vector3 point1, Vector3 point2)
        {
            for (int i = 0; i < m_linkedCells.Length; i++)
            {
                m_linkedCells[i] = null;
            }
            //set points
            m_vertices[0] = point0;
            m_vertices[1] = point1;
            m_vertices[2] = point2;
            //work out lines

            //work out plane
        }
    }
}
