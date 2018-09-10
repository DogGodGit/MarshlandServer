using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Microsoft.Xna.Framework;

namespace MainServer.Pathing
{
    class ASMap
    {
        List<ASTriangle> m_triangles=new List<ASTriangle>();
        ASPartition[][] m_Partitions=null ;
        int m_maxX = 0;
        int m_minX = 0;
        int m_maxY = 0;
        int m_minY = 0;
        float m_partitionSize = 1.0f;

        ASPartition GetPartitionForPosition(Vector3 position)
        {

            if (m_Partitions == null)
            {
                return null;
            }
            int xGridPos = (int)Math.Floor((position.X - m_minX) / m_partitionSize);
            int yGridPos = (int)Math.Floor((position.Y - m_minY) / m_partitionSize);

            if ((xGridPos > 0) && (xGridPos < m_Partitions.Length))
            {
                if ((yGridPos > 0) && (yGridPos < m_Partitions[xGridPos].Length))
                {
                    return m_Partitions[xGridPos][yGridPos];
                }
            }
            return null;
        }

        void LoadAIMap(string fileName)
        {

            //read in triangles
            //keep a check on the max/min points

            //once all the triangles are read
            //work out how many partitions required

            //go through each triangles
            //work out the max and min points (and by such max and min partitions)
            //check it's ownership in each grid
            //add to grid element

        }

    }
}
