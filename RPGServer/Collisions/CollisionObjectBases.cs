using System;
using XnaGeometry;

namespace MainServer.Collisions
{
    class CAACylinder
    {

        public CAACylinder(Vector3 centre, float radius, float height)
        {
            m_centre = centre;
            m_radius = radius;
            m_height = height;

        }
        public Vector3 m_centre;
        public float m_radius;
        public float m_height;
    }
    class CAABB
    {

        public CAABB(Vector3 min, Vector3 max)
        {
            m_min = min;
            m_max = max;
        }
        public Vector3 m_min;
        public Vector3 m_max;

        internal bool checkCircleInterSection(Vector3 circleCenter, float circleRadius, Vector3 AABBcenter, bool OBB)
        {
            bool collision = false;

            Vector3 recCenter = OBB ? (m_max + m_min) / 2.0f : AABBcenter;

            float recHalfWidth = (float)((m_max.X - m_min.X) / 2.0f);
            float recHalfHeight = (float)((m_max.Z - m_min.Z) / 2.0f);
            float circleDistanceX = (float)Math.Abs(circleCenter.X - recCenter.X);
            float circleDistanceZ = (float)Math.Abs(circleCenter.Z - recCenter.Z);

            if ((circleDistanceX < (recHalfWidth + circleRadius)) && (circleDistanceZ < (recHalfHeight + circleRadius)))
            {
                collision = true;
                return collision;
            }
            else
            {
                collision = false;
                return collision;
            }
        }

        public Vector3 checkIntersection(Vector3 oldPos, Vector3 newPos, bool ignoreY, float radius)
        {
            float EPSILON = 0.001f;
            Vector3 p = oldPos;
            Vector3 d = newPos - oldPos;
            double tmin = 0.0f;          // set to -FLT_MAX to get first hit on line
            double tmax = d.Length();// C3DMath::Magnitude(d); // set to max distance ray can travel (for segment)
            if (tmax == 0)
            {
                return oldPos;
            }
            d = d / tmax;
            // For all three slabs
            if (Math.Abs(d.X) < EPSILON)
            {
                // Ray is parallel to slab. No hit if origin not within slab
                if (p.X < (m_min.X - radius) || p.X > (m_max.X + radius)) return newPos;
            }
            else
            {
                // Compute intersection t value of ray with near and far plane of slab
                double ood = 1.0f / d.X;
                double t1 = ((m_min.X - radius) - p.X) * ood;
                double t2 = ((m_max.X + radius) - p.X) * ood;
                // Make t1 be intersection with near plane, t2 with far plane
                if (t1 > t2)
                {
                    double f = t2;
                    t2 = t1;
                    t1 = f;
                }
                // Compute the intersection of slab intersections intervals
                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;
                // Exit with no collision as soon as slab intersection becomes empty
                if (tmin > tmax) return newPos;
            }


            // For all three slabs
            if(!ignoreY)
            {
            if (Math.Abs(d.Y) < EPSILON)
            {
                // Ray is parallel to slab. No hit if origin not within slab
                if (p.Y < (m_min.Y - radius) || p.Y > (m_max.Y + radius)) return newPos;
            }
            else
            {
                // Compute intersection t value of ray with near and far plane of slab
                double ood = 1.0f / d.Y;
                double t1 = ((m_min.Y - radius) - p.Y) * ood;
                double t2 = ((m_max.Y + radius) - p.Y) * ood;
                // Make t1 be intersection with near plane, t2 with far plane
                if (t1 > t2)
                {
                    double f = t2;
                    t2 = t1;
                    t1 = f;
                }
                // Compute the intersection of slab intersections intervals
                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;
                // Exit with no collision as soon as slab intersection becomes empty
                if (tmin > tmax) return newPos;
            }

            }
          

            // For all three slabs

            if (Math.Abs(d.Z) < EPSILON)
            {
                // Ray is parallel to slab. No hit if origin not within slab
                if (p.Z < (m_min.Z - radius) || p.Z > (m_max.Z + radius)) return newPos;
            }
            else
            {
                // Compute intersection t value of ray with near and far plane of slab
                double ood = 1.0f / d.Z;
                double t1 = ((m_min.Z - radius) - p.Z) * ood;
                double t2 = ((m_max.Z + radius) - p.Z) * ood;
                // Make t1 be intersection with near plane, t2 with far plane
                if (t1 > t2)
                {
                    double f = t2;
                    t2 = t1;
                    t1 = f;
                }
                // Compute the intersection of slab intersections intervals
                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;
                // Exit with no collision as soon as slab intersection becomes empty
                if (tmin > tmax) return newPos;
            }

            return p + d * tmin;

        }
        /// <summary>
        /// Does this point lie in or on the edges of this collision
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
          internal bool CheckPositionIntersection(Vector3 position)
          {
              bool intersects = false;

              Vector3 p = position;

              //box check
              if (position.X >= m_min.X && position.X <= m_max.X && position.Z >= m_min.Z && position.Z <= m_max.Z)
              {
                  intersects = true;
              }
             
              return intersects;
          }
    }
    class COBB
    {

        public COBB(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float height)
        {
            m_p0 = p0;
            m_p1 = p1;
            m_p2 = p2;
            m_p3 = p3;
            m_p3 = p2 + p1 - p0;
            Vector3 p0p1 = p1 - p0;
            Vector3 p0p2 = p2 - p0;
            Vector3 cp = (p2 + p1) / 2;
            double w = p0p1.Length();//C3DMath::Magnitude(p0p1);
            double h = p0p2.Length();//C3DMath::Magnitude(p0p2);

            m_height = height;
            Vector3 min = new Vector3(-w / 2, -height / 2, -h / 2);
            Vector3 max = new Vector3(w / 2, height / 2, h / 2);
            Vector3 n1 = Vector3.Normalize(p0p1);//C3DMath::Normalize(p0p1);
            m_trans = Matrix.Identity;//.setIdentity();

            /*m_trans.f[0]=n1.x;
            m_trans.f[2]=n1.z;
            m_trans.f[8]=-n1.z;
            m_trans.f[10]=n1.x;
            m_trans.f[12]=cp.x;
            m_trans.f[13]=cp.y+height/2;
            m_trans.f[14]=cp.z;
            */

            m_trans.M11 = n1.X;
            m_trans.M13 = n1.Z;
            m_trans.M31 = -n1.Z;
            m_trans.M33 = n1.X;
            m_trans.M41 = cp.X;
            m_trans.M42 = cp.Y + height / 2;
            m_trans.M43 = cp.Z;




            m_invTrans = Matrix.Invert(m_trans);

            /*Vector3 t1 = Vector3.Transform(m_p0, m_invTrans);
            Vector3 t2 = Vector3.Transform(m_p1, m_invTrans);
            Vector3 t3 = Vector3.Transform(m_p2, m_invTrans);
            Vector3 t4 = Vector3.Transform(m_p3, m_invTrans);
            Vector3 t5 = Vector3.Transform(new Vector3(66.6627579f, 1.8323034f, -10.1776447f), m_invTrans);
            Vector3 t6 = Vector3.Transform(new Vector3(66.5989761f, 1.25349581f, -6.52375412f), m_invTrans);*/
            /*Vector3 t1 = m_p0 * m_invTrans;
            Vector3 t2 = m_p1 * m_invTrans;
            Vector3 t3 = m_p2 * m_invTrans;
            Vector3 t4 = m_p3 * m_invTrans;
            Vector3 t5 = Vector3(66.6627579, 1.8323034, -10.1776447) * m_invTrans;
            Vector3 t6 = Vector3(66.5989761, 1.25349581, -6.52375412) * m_invTrans;*/

            m_AABB = new CAABB(min, max);
        }
        ~COBB()
        {
            m_AABB = null;
        }
        public CAABB m_AABB;
        public float m_orientationAngle = 0;
        public Vector3 m_p0;
        public Vector3 m_p1;
        public Vector3 m_p2;
        public Vector3 m_p3;
        public Matrix m_trans;
        public Matrix m_invTrans;
        //CMatrix4 m_trans;
        //CMatrix4 m_invTrans;
        public float m_height;
    }

    class CQUAD
    {

        public CQUAD(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            m_a = a;
            m_b = b;
            m_c = c;
            m_d = d;
        }
        public Vector3 m_a;
        public Vector3 m_b;
        public Vector3 m_c;
        public Vector3 m_d;
    }
}
