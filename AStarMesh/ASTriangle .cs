using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaGeometry;

namespace AStarMesh
{
   
    class Line3D
    {

        static float MIN_DISTANCE_FROM_EDGE = 0.5f;
        static float COMFORT_DISTANCE_FROM_EDGE = 1.0f;
        Vector3 m_startPoint;
        Vector3 m_endPoint;
        Vector3 m_midpoint;
        Vector3 m_vector;

        /// <summary>
        /// The min value of T allowed from the edge
        /// </summary>
        double m_minT = 0.5f;
        /// <summary>
        /// The value of t deemed a comfortable distance from the edge
        /// </summary>
        double m_comfortT = 0.5f;
        internal double MinT
        {
            get { return m_minT; }
        }
        internal double ComfortT
        {
            get { return m_comfortT; }
        }
        internal Vector3 MidPoint
        {
            get { return m_midpoint; }
        }
        internal Line3D(Vector3 start, Vector3 end)
        {
            m_startPoint = start;
            m_endPoint = end;
            m_vector = m_endPoint - m_startPoint;
            m_midpoint = (end + start) / 2;

            double length = m_vector.Length();
            m_minT = MIN_DISTANCE_FROM_EDGE / length;
            if ((m_minT < 0) || (m_minT > 0.5))
            {
                m_minT = 0.5f;
            }

            m_comfortT = COMFORT_DISTANCE_FROM_EDGE / length;
            if ((m_comfortT < 0) || (m_comfortT > 0.5))
            {
                m_comfortT = 0.5f;
            }

        }
        /// <summary>
        /// on line, given point C find closest point to line D
        /// </summary>
        /// <param name="point0"></param>
        /// <returns></returns>
        internal Vector3 ClosestPointOnSegmentFromPoint(Vector3 point0)
        {

            // on line AB , given point C find closest point to line D
            Vector3 vecAC = point0 - m_startPoint;

            double t0 = Vector3.Dot(vecAC, m_vector) / Vector3.Dot(m_vector, m_vector);

            if (t0 < m_comfortT)
            {
                t0 = m_comfortT;
            }
            else if (t0 > (1 - m_comfortT))
            {
                t0 = 1-m_comfortT;
            }

            Vector3 closestPoint = m_startPoint + t0 * m_vector;

            return closestPoint;

        }
        internal Vector3 Get2DInterSectionWithLine(Vector3 point0, Vector3 point1, ref double t0, ref double t1)
        {
            t0 = -1;
            t1 = -1;
            /*taken from : http://en.wikipedia.org/wiki/Line-line_intersection 19/5/11
             * //L1 is this L2 is points handed in
             * (Px,Py) = ((X1Y2-Y1X2)(X3-X4)-(X1-X2)(X3Y4-Y3X4))    |   (X1Y2-Y1X2)(Y3-Y4) - (Y1-Y2)(X3Y4-Y3X4)
             *             ------------------------------------     |    ------------------------------------
             *              (X1-X2)(Y3-Y4)-(Y1-Y2)(X3-X4)           |       (X1-X2)(Y3-Y4)-(Y1-Y2)(X3-X4)
             * 
             */

            // (X1-X2)(Y3-Y4)-(Y1-Y2)(X3-X4)
            double denominator = (m_startPoint.X - m_endPoint.X) * (point0.Z - point1.Z) -
                (m_startPoint.Z - m_endPoint.Z) * (point0.X - point1.X);

            // the lines are parallel so will not intersect
            if (denominator == 0)
            {
                t0 = -100;
                t1 = -100;
                return new Vector3(99999, 99999, 99999);
            }
            //(X1Y2-Y1X2)
            double X1Y2_Y1X2 = (m_startPoint.X * m_endPoint.Z - m_startPoint.Z * m_endPoint.X);
            //(X3Y4-Y3X4)
            double X3Y4_Y3X4 = point0.X * point1.Z - point0.Z * point1.X;
             /*         |((X1Y2-Y1X2)(X3-X4)-(X1-X2)(X3Y4-Y3X4))    |   
             *x     =   | ------------------------------------      |
             *          |   (X1-X2)(Y3-Y4)-(Y1-Y2)(X3-X4)           |*/
            double x = (X1Y2_Y1X2 * (point0.X - point1.X) - (m_startPoint.X - m_endPoint.X) * X3Y4_Y3X4) / denominator;
            /*          |   (X1Y2-Y1X2)(Y3-Y4) - (Y1-Y2)(X3Y4-Y3X4)
             * y    =   |   ------------------------------------
             *          |       (X1-X2)(Y3-Y4)-(Y1-Y2)(X3-X4)*/
            double y = (X1Y2_Y1X2 * (point0.Z - point1.Z) - (m_startPoint.Z - m_endPoint.Z) * X3Y4_Y3X4) / denominator;

            //work out t Values
            //l1 (this)
            if (m_vector.X != 0)
            {
                t0 = (x - m_startPoint.X) / m_vector.X;
            }
            else if (m_vector.Z != 0)
            {
                t0 = (y - m_startPoint.Z) / m_vector.Z;
            }
            //l2 passed in point
            Vector3 l2Vector = point1 - point0;
            if (l2Vector.X != 0)
            {
                t1 = (x - point0.X) / l2Vector.X;
            }
            else if (l2Vector.Z != 0)
            {
                t1 = (y - point0.Z) / l2Vector.Z;
            }
            float minT = 0;
            if (t0 < (0+minT))
            {
               t1 = -1;
                x = m_comfortT * m_vector.X + m_startPoint.X;
                y = m_comfortT * m_vector.Z + m_startPoint.Z;
            }
            else if (t0 > (1 - minT))
            {
                t1 = -1;
                x = (1 - m_comfortT) * m_vector.X + m_startPoint.X;
                y = (1 - m_comfortT) * m_vector.Z + m_startPoint.Z;
            }
            return new Vector3(x, 0, y);
        }
    };
    class Plane3D
    {
        Vector3 m_normal;
        Vector3 m_a;
        Vector3 m_b;
        Vector3 m_c;
        //p.n
        double m_d;
        double TripleScalarProduct(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Dot(a,Vector3.Cross(b,c));
        }
        internal Vector3 GetClosestPointTo(Vector3 point)
        {
            double t = Vector3.Dot(m_normal, point) - m_d;
            return point - t * m_normal;
        }

        internal Vector3 GetClosestPointToLine(Vector3 point0, Vector3 point1)
        {
            /*
             * for line la +(lb-la)t
             * with plane p0+(p1-p0)u + (p2-p0)v
             * form http://en.wikipedia.org/wiki/Line-plane_intersection
             |t|        |(Xa-Xb)    (X1-X0)     (X2-X0)|-1  |(Xa-X0)|
             |u|    =   |(Ya-Yb)    (Y1-Y0)     (Y2-Y0)|    |(Ya-Y0)|
             |v|        |(Za-Zb)    (Z1-Z0)     (Z2-Z0)|    |(Za-Z0)|
             
             */
            //la-lb
            Vector3 la_lb = point0 - point1;
            bool parallel = (Vector3.Dot(la_lb,m_normal)==0);
            //if parallel then don't check
            if (parallel)
            {
                return point0;
            }
            //p1-p0
            Vector3 p1_p0 = m_b - m_a;
            //p2-p0
            Vector3 p2_p0 = m_c - m_a;
            //la-p0
            Vector3 la_p0 = point0 - m_a;

            Matrix theMatrix = new Matrix(la_lb.X, la_lb.Y, la_lb.Z, 0, p1_p0.X, p1_p0.Y, p1_p0.Z, 0, p2_p0.X, p2_p0.Y, p2_p0.Z, 0, 0, 0, 0, 1);

            Matrix inverse = Matrix.Invert(theMatrix);
            //Matrix transpose = Matrix.Transpose(inverse);

            Vector3 result = Vector3.Transform(la_p0, inverse);

            //subs into line la +(lb-la)t
            double t = result.X;
            Vector3 resultPoint = point0 + (-la_lb) * t;
            //is it on the segment
            double u = result.Y;
            double v = result.Z;
            double u_v_sqr = u + v;// (u * u) + (v * v);
            if((u<0)||(v<0)||
                (u>1)||(v>1)||
                (u_v_sqr<0)||(u_v_sqr>1))
            {
                //clamp uv to the segment
                if (u_v_sqr > 1)
                {
                    u = u / u_v_sqr;
                    v = v / u_v_sqr;
                }
                if (u < 0)
                {
                    u = 0;
                }
                else if (u > 1)
                {
                    u = 1;
                }
                if (v < 0)
                {
                    v = 0;
                }
                else if (v > 1)
                {
                    v = 1;
                }

                resultPoint = m_a + u * p1_p0 + v * p2_p0;
                resultPoint.Y = point0.Y;
                return resultPoint;
            }
            
            return resultPoint;

        }
        
        internal Vector3 Normal
        {
           // set { m_normal = value; }
            get { return m_normal; }
        }


        internal Plane3D(Vector3 a, Vector3 b, Vector3 c)
        {
            m_normal = Vector3.Normalize(Vector3.Cross((b - a), (c - a)));

            m_a = a;
            m_b = b;
            m_c = c;
            m_d = Vector3.Dot(m_normal, a);
            
        }

    };

    class ASTriangle
    {
        
        ASTriangle [] m_linkedCells = new ASTriangle[3];
        internal ushort [] m_linkedCellsIndices;
        Vector3[] m_vertices = new Vector3[3];
        Vector3 m_faceNormal;
        Line3D[] m_side = new Line3D[3];
        Plane3D m_plane = null;

        internal Plane3D Plane
        {
            get { return m_plane; }
        }
        internal Vector3 GetVertices(int verticeNumber)
        {
            if ((verticeNumber >= 0) && (verticeNumber < m_vertices.Length))
            {
                return m_vertices[verticeNumber];
            }
            return new Vector3(-99999);
        }
        internal Line3D GetSide(int linkNumber)
        {
            if ((linkNumber >= 0) && (linkNumber < m_side.Length))
            {
                return m_side[linkNumber];
            }
            return null;
        }
        internal ASTriangle GetLink(int linkNumber)
        {
            if ((linkNumber >= 0) && (linkNumber < m_linkedCells.Length))
            {
                return m_linkedCells[linkNumber];
            }
            return null;
        }
        
        internal void SetLink(int linkNumber, ASTriangle newLink)
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
            m_side[0] = new Line3D(point0, point1);
            m_side[1] = new Line3D(point1, point2);
            m_side[2] = new Line3D(point2, point0);
            

            //work out plane
            m_plane = new Plane3D(point0, point1, point2);


        }
        internal Vector3 FaceNormal
        {
            set { m_faceNormal = value; }
            get { return m_faceNormal; }
        }
        /// <summary>
        /// Checks which vertexes are shared to decide what link position it shoul dave
        /// </summary>
        /// <param name="joiningTriangle">The triangle to find the link number for</param>
        /// <returns>-1 on error</returns>
        internal int GetIndexForTriangle(ASTriangle joiningTriangle)
        {

            bool matches0 = false;
            bool matches1 = false;
            bool matches2 = false;
            for (int i = 0; i < 3; i++)
            {
                Vector3 vertex = joiningTriangle.GetVertices(i);

                if (vertex == m_vertices[0])
                {
                    matches0 = true;
                }
                if (vertex == m_vertices[1])
                {
                    matches1 = true;
                }
                if (vertex == m_vertices[2])
                {
                    matches2 = true;
                }
            }
            
            //if it matches vertices 0 & 1
            if (matches0 && matches1)
            {
                //it's link 0
                return 0;
            }

            //if it matches vertices 1 & 2
            if (matches1 && matches2)
            {
                //it's link 1
                return 1;
            }

            //if it matches vertices 2 & 0
            if (matches2 && matches0)
            {
                //it's link 2
                return 2;
            }

            //if none of these worked
            //return -1 in error
            return -1;
        }
    }
}
