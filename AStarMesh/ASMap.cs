using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using XnaGeometry;

namespace AStarMesh
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

        internal int MaxX
        {
            get { return m_maxX; }
        }
        internal int MaxY
        {
            get { return m_maxY; }
        }
        internal int MinX
        {
            get { return m_minX; }
        }
        internal int MinY
        {
            get { return m_minY; }
        }
        internal List<ASTriangle> Triangles
        {
            get { return m_triangles; }
        }
        internal ASMap()
        {
            LoadTestAIMap();
        }
        
        internal ASPartition GetPartitionForPosition(Vector3 position)
        {

            if (m_Partitions == null)
            {
                return null;
            }
            int xGridPos = (int)Math.Floor((position.X - m_minX) / m_partitionSize);
            int yGridPos = (int)Math.Floor((position.Z - m_minY) / m_partitionSize);

            if ((xGridPos > 0) && (xGridPos < m_Partitions.Length))
            {
                if ((yGridPos > 0) && (yGridPos < m_Partitions[xGridPos].Length))
                {
                    return m_Partitions[xGridPos][yGridPos];
                }
            }
            return null;
        }

        internal ASTriangle GetClosestTriangleForPosition(Vector3 position)
        {
            if (m_Partitions == null)
            {
                return null;
            }

            double xfloatGridPos = (position.X - m_minX) / m_partitionSize;
            double yfloatGridPos = (position.Z - m_minY) / m_partitionSize;
            int xGridPos = (int)Math.Floor((position.X - m_minX) / m_partitionSize);
            int yGridPos = (int)Math.Floor((position.Z - m_minY) / m_partitionSize);

            ASTriangle closestTriangle = null;
            if ((xGridPos > 0) && (xGridPos < m_Partitions.Length))
            {
                if ((yGridPos > 0) && (yGridPos < m_Partitions[xGridPos].Length))
                {
                    closestTriangle= m_Partitions[xGridPos][yGridPos].GetClosestTriangleToPosition(position);
                }
            }
            if (closestTriangle == null)
            {

                int currentGridX = xGridPos;
                int currentGridY = yGridPos;
                if (currentGridY >= m_Partitions.Length)
                {
                    currentGridY = m_Partitions.Length - 1;
                }
                if (currentGridX >= m_Partitions.Length)
                {
                    currentGridX = m_Partitions.Length - 1;
                }
                for (int currentQuadrent = 1; currentQuadrent < m_Partitions.Length && closestTriangle == null; currentQuadrent++)
                {
                    closestTriangle = GetClosestTriForIntQuadrentsFromPoint(currentQuadrent, currentGridX, currentGridY, position);
                }
            }
            return closestTriangle;
        }
        ASTriangle GetClosestTriForIntQuadrentsFromPoint(int iQuadrent,int startGridX , int startGridY,Vector3 position)
        {

            ASTriangle closestTriangle = null;
            double closestDistance = 500;
            int currentGridX = startGridX;
            int currentGridY = startGridY;

            currentGridX = startGridX - iQuadrent;
            currentGridY = startGridY + iQuadrent;
            int maxGridSize = m_Partitions.Length;
            if ((currentGridY >= 0) && (currentGridY < maxGridSize))
            {
                for (int i = startGridX - iQuadrent; i < startGridX + iQuadrent; i++)
                {
                    if ((i >= 0) && (i < maxGridSize))
                    {
                        ASPartition currentPartition = m_Partitions[i][currentGridY];
                        double currentDist = closestDistance;
                        ASTriangle currentTriangle = currentPartition.GetClosestTriangleToPosition(position, ref currentDist);
                        if (currentTriangle != null && ((currentDist > closestDistance) || (closestTriangle==null)))
                        {
                            closestDistance = currentDist;
                            closestTriangle = currentTriangle;
                        }
                    }
                }

            }
            currentGridY = startGridY - iQuadrent;
            if ((currentGridY >= 0) && (currentGridY < maxGridSize))
            {
                for (int i = startGridX - iQuadrent; i < startGridX + iQuadrent; i++)
                {
                    if ((i >= 0) && (i < maxGridSize))
                    {
                        ASPartition currentPartition = m_Partitions[i][currentGridY];
                        double currentDist = closestDistance;
                        ASTriangle currentTriangle = currentPartition.GetClosestTriangleToPosition(position, ref currentDist);
                        if (currentTriangle != null && ((currentDist > closestDistance) || (closestTriangle == null)))
                        {
                            closestDistance = currentDist;
                            closestTriangle = currentTriangle;
                        }
                    }
                }
            }

            currentGridX = startGridX + iQuadrent;
            if ((currentGridX >= 0) && (currentGridX < maxGridSize))
            {
                for (int j = startGridY - iQuadrent+1; j < startGridY + iQuadrent-1; j++)
                {
                    if ((j >= 0) && (j < maxGridSize))
                    {
                        ASPartition currentPartition = m_Partitions[currentGridX][j];
                        double currentDist = closestDistance;
                        ASTriangle currentTriangle = currentPartition.GetClosestTriangleToPosition(position, ref currentDist);
                        if (currentTriangle != null && ((currentDist > closestDistance) || (closestTriangle == null)))
                        {
                            closestDistance = currentDist;
                            closestTriangle = currentTriangle;
                        }
                    }
                }
            }
            currentGridX = startGridX - iQuadrent;
            if ((currentGridX >= 0) && (currentGridX < maxGridSize))
            {
                for (int j = startGridY - iQuadrent+1; j < startGridY + iQuadrent-1; j++)
                {
                    if ((j >= 0) && (j < maxGridSize))
                    {
                        ASPartition currentPartition = m_Partitions[currentGridX][j];
                        double currentDist = closestDistance;
                        ASTriangle currentTriangle = currentPartition.GetClosestTriangleToPosition(position, ref currentDist);
                        if (currentTriangle != null && ((currentDist > closestDistance) || (closestTriangle == null)))
                        {
                            closestDistance =currentDist;
                            closestTriangle = currentTriangle;
                        }
                    }
                }
            }


            return closestTriangle;
        }
        void LoadTestAIMap()
        {
            LoadAIMap("aitestmap2.aimap");
        }

        void LoadAIMap(string fileName)
        {
            if (File.Exists(fileName) == false)
            {
                System.Windows.Forms.MessageBox.Show("Can't find collision file " + fileName);
                return;
            }

            Vector3 max = new Vector3(-100000, -100000, -100000);
            Vector3 min = new Vector3(100000, 100000, 100000);
            using (FileStream stream = new FileStream(fileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {


                    // read vertices
                    int numVertices = reader.ReadUInt16();
                    Vector3[] vertices=new Vector3[numVertices];
                    for (int i = 0; i < numVertices; i++)
                    {
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();
                        float z = reader.ReadSingle();
                        if (x > max.X)
                        {
                            max.X = x;
                        }
                        if (y > max.Y)
                        {
                            max.Y = y;
                        }
                        if (z > max.Z)
                        {
                            max.Z = z;
                        }
                        if (x < min.X)
                        {
                            min.X = x;
                        }
                        if (y < min.Y)
                        {
                            min.Y = y;
                        }
                        if (z < min.Z)
                        {
                            min.Z = z;
                        }
                        vertices[i] = new Vector3(x, y, z);
                    }
                    // read triangles
                    int numTriangles = reader.ReadUInt16();
                    for(int i=0;i<numTriangles ;i++)
                    {
                        // read vertex indices
                        ushort vertIndex0=reader.ReadUInt16();
                        ushort vertIndex1=reader.ReadUInt16();
                        ushort vertIndex2=reader.ReadUInt16();

                        ASTriangle newTriangle = new ASTriangle(vertices[vertIndex0], vertices[vertIndex1], vertices[vertIndex2]);
                        // read face colour
                        byte r = reader.ReadByte();
                        byte g = reader.ReadByte();
                        byte b = reader.ReadByte();
                        byte a = reader.ReadByte();
                        //read face normal
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();
                        float z = reader.ReadSingle();
                        newTriangle.FaceNormal = new Vector3(x, y, z);
                        //read neighbouring triangles
                        byte numLinkedTriangles = reader.ReadByte();
                        newTriangle.m_linkedCellsIndices = new ushort[numLinkedTriangles];
                        //newTriangle.m_linkedCells = new ASTriangle[numLinkedTriangles];
                        for (int j = 0; j < numLinkedTriangles; j++)
                        {
                            newTriangle.m_linkedCellsIndices[j] = reader.ReadUInt16();
                        }
                        m_triangles.Add(newTriangle);
                    }
                }
            }
            // assign neighbourhood triangles to each triangle
            for (int i = 0; i < m_triangles.Count; i++)
            {
                ASTriangle currentTriangle = m_triangles[i];
                for (int j = 0; j < currentTriangle.m_linkedCellsIndices.Length; j++)
                {
                    ASTriangle currentLinkedTri = m_triangles[currentTriangle.m_linkedCellsIndices[j]];
                    //work out what position to Put It
                    int linkNumber = currentTriangle.GetIndexForTriangle(currentLinkedTri);
                    if (linkNumber >= 0)
                    {
                        currentTriangle.SetLink(linkNumber,currentLinkedTri);
                    }
                    else
                    {
                        currentTriangle = null;
                    }
                }
                //tidy up afterwards
                m_triangles[i].m_linkedCellsIndices = null;
            }



            //once all the triangles are read
            //work out how many partitions required
            m_minX = (int)Math.Floor(min.X);
            m_minY = (int)Math.Floor(min.Z);
            m_maxX = (int)Math.Ceiling(max.X);
            m_maxY = (int)Math.Ceiling(max.Z);

            //the max number of partitions
            int numPartitions = 100;
            //the min size of a partition
            float minSize = 2.5f;

            float xRange = m_maxX - m_minX;
            float yRange = m_maxY - m_minY;

            float maxRange = xRange;

            if (xRange < yRange)
            {
                maxRange = yRange;
            }
            //how many partitions are required
            //if num partitions is more than needed for each grid to be min size
            //then make it size needed for each grid to be min size
            if (maxRange / minSize < numPartitions)
            {
                numPartitions = (int)Math.Ceiling(maxRange / minSize);
            }
            m_partitionSize = maxRange / (float)numPartitions;
            
            m_Partitions = new ASPartition[numPartitions][];
            for (int curPartitionCol = 0; curPartitionCol < numPartitions; curPartitionCol++)
            {
                m_Partitions[curPartitionCol] = new ASPartition[(int)numPartitions];
                for (int curPartitionRow = 0; curPartitionRow < numPartitions; curPartitionRow++)
                {
                    m_Partitions[curPartitionCol][curPartitionRow] = new ASPartition();
                }
            }
           


            //go through each triangles
            for (int i = 0; i < m_triangles.Count; i++)
            {
                max = new Vector3(-100000, -100000, -100000);
                min = new Vector3(100000, 100000, 100000);
                ASTriangle currentTri = m_triangles[i];
                for (int j = 0; j < 3; j++)
                {
                    Vector3 currentPoint = currentTri.GetVertices(j);
                    //work out the max and min points (and by such max and min partitions)
                    if (currentPoint.X > max.X)
                    {
                        max.X = currentPoint.X;
                    }
                    if (currentPoint.Y > max.Y)
                    {
                        max.Y = currentPoint.Y;
                    }
                    if (currentPoint.Z > max.Z)
                    {
                        max.Z = currentPoint.Z;
                    }
                    if (currentPoint.X < min.X)
                    {
                        min.X = currentPoint.X;
                    }
                    if (currentPoint.Y < min.Y)
                    {
                        min.Y = currentPoint.Y;
                    }
                    if (currentPoint.Z < min.Z)
                    {
                        min.Z = currentPoint.Z;
                    }
                }
                //check it's ownership in each grid
                int minxGridPos = (int)Math.Floor((min.X - m_minX) / m_partitionSize);
                int minyGridPos = (int)Math.Floor((min.Z - m_minY) / m_partitionSize);

                int maxxGridPos = (int)Math.Ceiling((max.X - m_minX) / m_partitionSize);
                int maxyGridPos = (int)Math.Ceiling((max.Z - m_minY) / m_partitionSize);
                //add to grid element

                for (int currGridX = minxGridPos; currGridX <= maxxGridPos; currGridX++)
                {
                    if (currGridX<0||currGridX >= m_Partitions.Length)
                    {
                        continue;
                    }
                    ASPartition[] currentPartitionCol = m_Partitions[currGridX];
                    for (int currGridY = minyGridPos; currGridY <= maxyGridPos; currGridY++)
                    {
                        if (currGridY < 0 || currGridY >= currentPartitionCol.Length)
                        {
                            continue;
                        }
                        //would then check collision with plane segment to aabb
                        currentPartitionCol[currGridY].AddTriangleToList(currentTri);
                        
                    }
                }
            }

        }

    }
}
