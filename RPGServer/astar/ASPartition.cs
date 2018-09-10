using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XnaGeometry;

namespace MainServer
{
    class ASPartition
    {
        List<ASTriangle> m_triangles = new List<ASTriangle>();

        internal void AddTriangleToList(ASTriangle newTriangle)
        {
            m_triangles.Add(newTriangle);
        }
        internal ASTriangle GetClosestTriangleToPosition(Vector3 position)
        {

            Vector3 point0 = position;
            Vector3 point1 = position;
            point0.Y = 100000;
            point1.Y = -100000;

            ASTriangle closestTri = null;
            double closestDistance = 10000;

            for (int i = 0; i < m_triangles.Count; i++)
            {
                ASTriangle currentTri = m_triangles[i];
                Vector3 currentTriPoint = currentTri.Plane.GetClosestPointToLine(point0, point1);
                // if the y position has been changed then it is above the plane
                if ((currentTriPoint.Y != point0.Y))
                {
                    
                    return currentTri;
                }
                //otherwise look for the closest triangle
                currentTriPoint.Y = position.Y;
                double distance = (currentTriPoint - position).Length();
                if (distance < closestDistance || closestTri==null)
                {
                    closestDistance = distance;
                    closestTri = currentTri;
                }
            }

            return closestTri;
        }
        internal ASTriangle GetClosestTriangleToPosition(Vector3 position, ref double minDistance)
        {

            Vector3 point0 = position;
            Vector3 point1 = position;
            point0.Y = 100000;
            point1.Y = -100000;

            ASTriangle closestTri = null;
            double closestDistance = 10000;

            for (int i = 0; i < m_triangles.Count; i++)
            {
                ASTriangle currentTri = m_triangles[i];
                Vector3 currentTriPoint = currentTri.Plane.GetClosestPointToLine(point0, point1);
                // if the y position has been changed then it is above the plane
                if ((currentTriPoint.Y != point0.Y))
                {
                    minDistance = 0;
                    return currentTri;
                }
                //otherwise look for the closest triangle
                currentTriPoint.Y = position.Y;
                double distance = (currentTriPoint - position).Length();
                if (distance < closestDistance || closestTri == null)
                {
                    closestDistance = distance;
                    minDistance = closestDistance;
                    closestTri = currentTri;
                }
            }

            return closestTri;
        }
    }
}
