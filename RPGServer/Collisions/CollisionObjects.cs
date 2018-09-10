using System;
using XnaGeometry;

namespace MainServer.Collisions
{
    enum COLLISION_OBJECT_TYPES
    {
        COLLISION_OBJECT_SPHERE,
        COLLISION_OBJECT_AACYLINDER,
        COLLISION_OBJECT_AABB,
        COLLISION_OBJECT_OBB,
        COLLISION_OBJECT_QUAD
    };
    class CSphere//memuse=16
    {

        public CSphere(Vector3 centre, double radius)
        {
            m_centre = centre;
            m_radius = radius;
        }
        public Vector3 m_centre;
        public double m_radius;
    }
    class CCollisionObject
    {
        public
            CCollisionObject()
        {
            m_bounding_Sphere = null;
        }
        ~CCollisionObject()
        {
            //SAFE_DELETE(m_bounding_Sphere);
        }
        internal virtual bool checkBoundingSphere(Vector3 newPos, float radius)
        {
            bool ret = false;
            double minseparation = radius + m_bounding_Sphere.m_radius;
            if ((m_bounding_Sphere.m_centre - newPos).LengthSquared() < minseparation * minseparation)
            {
                ret = true;
            }
            return ret;
        }
        internal virtual bool checkBoundingSphere(Vector3 newPos, double radius, bool ignoreY)
        {
            bool ret = false;
            double minseparation = radius + m_bounding_Sphere.m_radius;
            double distanceSquared = 0;
            if (ignoreY)
            {
                distanceSquared = Utilities.Difference2DSquared(m_bounding_Sphere.m_centre, newPos);
            }
            else
            {
                distanceSquared = (m_bounding_Sphere.m_centre - newPos).LengthSquared();
            }
            if (distanceSquared < minseparation * minseparation)
            {

                ret = true;
            }
            return ret;
        }
        internal virtual bool CheckPositionIntersection(Vector3 position)
        {
            return false;
        }
        internal virtual Vector3 checkCollision(Vector3 oldPos, Vector3 newPos, float radius, float height, int index)
        {
            return oldPos;
        }
        public COLLISION_OBJECT_TYPES m_objectType;

        public CSphere m_bounding_Sphere;
        public CCircle m_bounding_Circle;
        public double m_halfWidth,m_halfDepth;

    }
    class CCollision_Sphere : CCollisionObject
    {

        public CCollision_Sphere(Vector3 centre, double radius)
        {
            m_bounding_Sphere = new CSphere(centre, radius);
            m_bounding_Circle = new CCircle(new Vector2(centre.X, centre.Z), radius);
            m_halfWidth =m_halfDepth=radius;
            m_objectType = COLLISION_OBJECT_TYPES.COLLISION_OBJECT_SPHERE;
        }
        ~CCollision_Sphere()
        {


            m_bounding_Sphere = null;
        }

        internal override Vector3 checkCollision(Vector3 oldPos, Vector3 newPos, float radius, float height, int index)
        {
            Vector3 revisedPos = newPos;
            double minseparation = radius + m_bounding_Sphere.m_radius;
            double xsep = m_bounding_Sphere.m_centre.X - newPos.X;
            double zsep = m_bounding_Sphere.m_centre.Z - newPos.Z;
            if (newPos.Y < m_bounding_Sphere.m_centre.Y + m_bounding_Sphere.m_radius && newPos.Y + height > m_bounding_Sphere.m_centre.Y - m_bounding_Sphere.m_radius)
            {
                if (xsep * xsep + zsep * zsep < minseparation * minseparation)
                {
                    revisedPos = oldPos;

                }
            }
            return revisedPos;
        }
        internal override bool CheckPositionIntersection(Vector3 position)
        {
            bool intersects = false;
            double xdif = position.X - m_bounding_Sphere.m_centre.X;
            double zdif = position.Z - m_bounding_Sphere.m_centre.Z;
            if (((xdif * xdif) + (zdif * zdif)) < (m_bounding_Sphere.m_radius * m_bounding_Sphere.m_radius))
            {
                intersects = true;
            }
            return intersects;

        }
    }
    class CCollision_AACylinder : CCollisionObject
    {

        public CAACylinder m_cylinder;
        public CCollision_AACylinder(Vector3 centre, float radius, float height)
        {
            m_objectType = COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AACYLINDER;
            float bsradius = (float)Math.Sqrt(height / 2 * height / 2 + radius * radius);
           
            m_bounding_Sphere = new CSphere(centre, bsradius);
            m_bounding_Circle = new CCircle(new Vector2(centre.X, centre.Z), radius);
            m_halfWidth=m_halfDepth=radius;
            m_cylinder = new CAACylinder(centre, radius + 0.5f, height + 1.0f);
        }
        ~CCollision_AACylinder()
        {
            m_cylinder = null;
        }

        internal bool checkCircleInterSection(Vector3 circleCenter, float circleRadius)
        {
            float sumOfRadiaa = (m_cylinder.m_radius) + circleRadius;
            float distance = Math.Abs(Utilities.Difference2D(circleCenter, m_cylinder.m_centre));

            return distance <= sumOfRadiaa;
        }

        internal override Vector3 checkCollision(Vector3 oldPos, Vector3 newPos, float radius, float height, int index)
        {
            Vector3 revisedPos = newPos;
            float minseparation = radius + m_cylinder.m_radius - 0.5f;
            double xsep = m_bounding_Sphere.m_centre.X - newPos.X;
            double zsep = m_bounding_Sphere.m_centre.Z - newPos.Z;
            //if (newPos.Y < m_bounding_Sphere.m_centre.Y + m_cylinder.m_height / 2 && newPos.Y + height > m_bounding_Sphere.m_centre.Y - m_cylinder.m_height / 2)
                if (xsep * xsep + zsep * zsep < minseparation * minseparation)
                {
                    Vector2 pos = new Vector2(m_bounding_Sphere.m_centre.X, m_bounding_Sphere.m_centre.Z) - Vector2.Normalize(new Vector2(m_bounding_Sphere.m_centre.X - newPos.X, m_bounding_Sphere.m_centre.Z - newPos.Z)) * (minseparation);

                    revisedPos = new Vector3(pos.X, oldPos.Y, pos.Y);
                    /**if(DEBUG_COL){
                        DLog(@"Actual collision Cylinder %d",index);
                    }*/
                }
            return revisedPos;
        }
        internal override bool CheckPositionIntersection(Vector3 position)
        {
            bool intersects = false;
            double xdif = position.X - m_bounding_Sphere.m_centre.X;
            double zdif = position.Z - m_bounding_Sphere.m_centre.Z;
            if (((xdif * xdif) + (zdif * zdif)) < (m_cylinder.m_radius * m_cylinder.m_radius))
            {
                intersects = true;
            }
            return intersects;

        }
        public Vector3 checkIntersection(Vector3 oldPos, Vector3 newPos,bool ignoreY, float radius)
        {
            Vector3 revisedPos = newPos;
            float minseparation = m_cylinder.m_radius + radius - 0.5f;
            double xsep = m_bounding_Sphere.m_centre.X - newPos.X;
            double zsep = m_bounding_Sphere.m_centre.Z - newPos.Z;
            if (ignoreY || (newPos.Y < m_bounding_Sphere.m_centre.Y + m_cylinder.m_height / 2 && newPos.Y > m_bounding_Sphere.m_centre.Y - m_cylinder.m_height / 2))
               // if (xsep * xsep + zsep * zsep < minseparation * minseparation)
                {
                    Vector3 localP1 = oldPos - m_cylinder.m_centre;
                    Vector3 localP2 = newPos - m_cylinder.m_centre;
                    Vector3 P2MinusP1 = localP2 - localP1;
                //magnitude squared
                    double a = (P2MinusP1.X * P2MinusP1.X) + (P2MinusP1.Z * P2MinusP1.Z);
                    if (a == 0)
                    {
                        return oldPos;
                    }
                    double b = 2 * ((P2MinusP1.X * localP1.X) + (P2MinusP1.Z * localP1.Z));
                    double c = (localP1.X * localP1.X) + (localP1.Z * localP1.Z) - (minseparation * minseparation);
                    double delta = b * b - (4 * a * c);
                    if (delta > 0)
                    {
                        float sqrtDelta = (float)Math.Sqrt(delta);
                        double u1 = (-b + sqrtDelta) / (2 * a);
                        double u2 = (-b - sqrtDelta) / (2 * a);

                        if (Math.Abs(u1) < Math.Abs(u2) && u1>=0 && u1<1)
                        {
                            revisedPos = oldPos + P2MinusP1 * u1;
                        }
                        else if(u2>=0 && u2<1)
                        {
                            revisedPos = oldPos + P2MinusP1 * u2;
                        }
                    }
                    /*if (DEBUG_COL)
                    {
                        DLog(@"Actual collision Cylinder %d", index);
                    }*/
                }
            return revisedPos;

        }
    }
    class CCollision_AABB : CCollisionObject
    {

        internal CAABB m_aabb;
        internal CCollision_AABB(Vector3 min, Vector3 max)
        {
            m_objectType = COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AABB;
            Vector3 centre = (min + max) / 2;
            double radius = (max - centre).Length();
            double radius2D = Utilities.Difference2DSquared(max, centre);
            m_bounding_Sphere = new CSphere(centre, radius + 0.5f);
            m_bounding_Circle = new CCircle(new Vector2(centre.X, centre.Z), radius2D);
            m_halfWidth=(max-min).X/2;
            m_halfDepth=(max-min).Z/2;
            m_aabb = new CAABB(min, max);

        }
        ~CCollision_AABB()
        {
            m_aabb = null;
        }

        internal bool checkCircleInterSection(Vector3 circleCenter, float circleRadius)
        {
            return m_aabb.checkCircleInterSection(circleCenter, circleRadius, m_bounding_Sphere.m_centre, false);
        }

        override internal Vector3 checkCollision(Vector3 oldPos, Vector3 newPos, float radius, float height, int index)
        {
            Vector3 revisedPos = newPos;
            //if (newPos.Y < m_aabb.m_max.Y && newPos.Y + height > m_aabb.m_min.Y)
            {
                double sqDist = 0;
                int quad = 0;
                if (newPos.X < m_aabb.m_min.X) //west
                {
                    sqDist += (m_aabb.m_min.X - newPos.X) * (m_aabb.m_min.X - newPos.X);
                    quad += 1;
                }
                if (newPos.X > m_aabb.m_max.X) //east
                {
                    sqDist += (newPos.X - m_aabb.m_max.X) * (newPos.X - m_aabb.m_max.X);
                    quad += 2;
                }
                if (newPos.Z < m_aabb.m_min.Z) //south
                {
                    sqDist += (m_aabb.m_min.Z - newPos.Z) * (m_aabb.m_min.Z - newPos.Z);
                    quad += 4;
                }
                if (newPos.Z > m_aabb.m_max.Z) //north
                {
                    sqDist += (newPos.Z - m_aabb.m_max.Z) * (newPos.Z - m_aabb.m_max.Z);
                    quad += 8;
                }
                if (sqDist < radius * radius)
                {


                    //	revisedPos=oldPos;


                    switch (quad)
                    {
                        case 1: //hit left of tile

                            //move character outside of tile
                            revisedPos.X = m_aabb.m_min.X - radius;
                            break;
                        case 2: //hit right of tile
                            revisedPos.X = m_aabb.m_max.X + radius;
                            break;
                        case 4: //hit top of tile
                            //move character outside of tile
                            revisedPos.Z = m_aabb.m_min.Z - radius;
                            break;
                        case 8: //hit bottom of tile
                            // bounce the ball away from at a speed proportional to the projected velocity and a bounciness factor
                            revisedPos.Z = m_aabb.m_max.Z + radius;
                            break;
                        case 5: //hit topleft of tile
                            {
                                Vector2 pos = new Vector2(m_aabb.m_min.X, m_aabb.m_min.Z) - Vector2.Normalize(new Vector2(m_aabb.m_min.X - newPos.X, m_aabb.m_min.Z - newPos.Z)) * radius;
                                revisedPos.X = pos.X;
                                revisedPos.Z = pos.Y;
                                //angle of impact
                                break;
                            }
                        case 6: //hit topright of tile
                            {
                                Vector2 pos = new Vector2(m_aabb.m_max.X, m_aabb.m_min.Z) - Vector2.Normalize(new Vector2(m_aabb.m_max.X - newPos.X, m_aabb.m_min.Z - newPos.Z)) * radius;
                                revisedPos.X = pos.X;
                                revisedPos.Z = pos.Y;
                                //angle of impact
                                break;
                            }
                        case 9: //hit bottomleft of tile
                            {
                                Vector2 pos = new Vector2(m_aabb.m_min.X, m_aabb.m_max.Z) - Vector2.Normalize(new Vector2(m_aabb.m_min.X - newPos.X, m_aabb.m_max.Z - newPos.Z)) * radius;
                                revisedPos.X = pos.X;
                                revisedPos.Z = pos.Y;
                                break;
                            }
                        case 10: //hit bottomright of tile
                            {
                                Vector2 pos = new Vector2(m_aabb.m_max.X, m_aabb.m_max.Z) - Vector2.Normalize(new Vector2(m_aabb.m_max.X - newPos.X, m_aabb.m_max.Z - newPos.Z)) * radius;
                                revisedPos.X = pos.X;
                                revisedPos.Z = pos.Y;
                                break;
                            }
                    }
                    /*if(DEBUG_COL)
                    {
                        DLog(@"Actual Collision AABB %d in quad %d at %f,%f,%f reseting to %f,%f,%f",index,quad,newPos.x,newPos.y,newPos.z,revisedPos.x,revisedPos.y,revisedPos.z);
                    }*/
                }

            }
            return revisedPos;
        }

        internal Vector3 checkIntersection(Vector3 oldPos, Vector3 newPos, bool ignoreY, float radius)
        {
            return m_aabb.checkIntersection(oldPos, newPos, ignoreY, radius);
        }

        /// <summary>
        /// Does this point lie in or on the edges of this collision
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal override bool CheckPositionIntersection(Vector3 position)
        {
            bool intersects = false;

            intersects = m_aabb.CheckPositionIntersection(position);

            return intersects;
        }
    }
    class CCollision_OBB : CCollisionObject
    {

        public COBB m_obb;
        public CCollision_OBB(Vector3 p0, Vector3 p1, Vector3 p2, float height,string name)
        {
            
            Vector3 a, b, c,d;
                Vector3 p1p0=p1-p0;
            double d10=p1p0.Length();
            Vector3 p2p0=p2-p0;
            double d20=p2p0.Length();
            Vector3 p2p1=p2-p1;
            double d21=p2p1.Length();
            double dot1 = Vector3.Dot(p1p0, p2p0);
            double dot2 = Vector3.Dot(p2p1, p1p0);
            double dot3 = Vector3.Dot(p2p1, p2p0);
            double t1=Math.Abs(Vector3.Dot(p1p0,p2p0)/(d10*d20));
            double t2=Math.Abs(Vector3.Dot(p2p1,p1p0)/(d21*d10));
            double t3 = Math.Abs(Vector3.Dot(p2p1, p2p0) / (d21 * d20));
            double tollerance = 0.002f;
            if (t1 < tollerance && t1 < t2 && t1 < t3)
            {
                a = p0;
                b = p1;
                c = p2;

            }
            else
            {

                if (t2 < tollerance && t2 < t1 && t2 < t1)
                {
                    a = p1;
                    b = p2;
                    c = p0;

                }
                else
                {
                    if (t3 < tollerance && t3 < t1 && t3 < t2)
                    {
                        a = p2;
                        b = p0;
                        c = p1;


                    }
                    else
                    {


                        Program.Display(@"badly defined OBB  "+name +" p0=" + p0.ToString() + ", p1=" + p1.ToString() + ",p2=" + p2.ToString() + ",height=" + height);


                        Program.Display("t1=" + t1 + ",t2=" + t2 + ",t3=" + t3);
                        a = p2;
                        b = p0;
                        c = p1;
                    }
                }
            }

            /* if (Math.Abs(Vector3.Dot((p1 - p0), (p2 - p0))) < 0.01f)
             {
                 a = p0;
                 b = p1;
                 c = p2;
             }
             else if (Math.Abs(Vector3.Dot(p2 - p1, p0 - p1)) < 0.01f)
             {
                 a = p1;
                 b = p2;
                 c = p0;
             }
             else
             {
                 a = p2;
                 b = p0;
                 c = p1;
             }*/
            m_objectType = COLLISION_OBJECT_TYPES.COLLISION_OBJECT_OBB;
            Vector3 centre = (c + b) / 2 + new Vector3(0, height / 2, 0);


            double bsradius = (a - centre).Length();
            d = a + (centre - a) * 2;
            Vector2 min   =new Vector2(Math.Min(a.X,Math.Min(b.X,Math.Min(c.X,d.X))),Math.Min(a.Z,Math.Min(b.Z,Math.Min(c.Z,d.Z))));
            Vector2 max = new Vector2(Math.Max(a.X, Math.Max(b.X, Math.Max(c.X, d.X))), Math.Max(a.Z, Math.Max(b.Z, Math.Max(c.Z, d.Z))));
            m_halfWidth = (max - min).X/2;
            m_halfDepth = (max - min).Y/2;
            double radius2D = Utilities.Difference2DSquared(a, centre);
            m_bounding_Sphere = new CSphere(centre, bsradius);
            m_bounding_Circle = new CCircle(new Vector2(centre.X, centre.Z), radius2D);
            m_obb = new COBB(a, b, c, d, height);
        }
        ~CCollision_OBB()
        {
            m_obb = null;
        }
        
        internal bool checkCircleInterSection(Vector3 circleCenter, float circleRadius)
        {
            Vector3 circleCenterT = Vector3.Transform(circleCenter, m_obb.m_invTrans);

            return m_obb.m_AABB.checkCircleInterSection(circleCenterT, circleRadius, m_bounding_Sphere.m_centre, true);
        }

        internal Vector3 checkIntersection(Vector3 oldPos, Vector3 newPos, bool ignoreY, float radius)
        {
            Vector3 p = Vector3.Transform(oldPos ,m_obb.m_invTrans);
            Vector3 q = Vector3.Transform(newPos , m_obb.m_invTrans);

            Vector3 nq = m_obb.m_AABB.checkIntersection(p, q,ignoreY, radius);
            Vector3 revisedPos = Vector3.Transform(nq , m_obb.m_trans);

            return revisedPos;
        }
        /// <summary>
        /// Does this point lie in or on the edges of this collision
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal override bool CheckPositionIntersection(Vector3 position)
        {
            bool intersects = false;
            Vector3 p = Vector3.Transform(position, m_obb.m_invTrans);

            intersects = m_obb.m_AABB.CheckPositionIntersection(p);
            
            return intersects;
        }
    }
    class CCollision_Quad : CCollisionObject
    {

        internal CQUAD m_quad;
        internal CCollision_Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            m_objectType = COLLISION_OBJECT_TYPES.COLLISION_OBJECT_QUAD;
            Vector3 centre = (a + b + c + d) / 4;
            double ra = (a - centre).Length();
            double rb = (b - centre).Length();
            double rc = (c - centre).Length();
            double rd = (d - centre).Length();
            double bsradius = ra;
            if (rb > bsradius)
            {
                bsradius = rb;
            }
            if (rc > bsradius)
            {
                bsradius = rc;
            }
            if (rd > bsradius)
            {
                bsradius = rd;
            }
            m_bounding_Sphere = new CSphere(centre, bsradius);
            m_quad = new CQUAD(a, b, c, d);
        }
        ~CCollision_Quad()
        {
            m_quad = null;
        }
        internal override Vector3 checkCollision(Vector3 oldPos, Vector3 newPos, float radius, float height, int index)
        {

            Vector3 revisedPos = newPos;
            Vector3 pq = oldPos - newPos;
            Vector3 pa = m_quad.m_a - oldPos;
            Vector3 pb = m_quad.m_b - oldPos;
            Vector3 pc = m_quad.m_c - oldPos;
            Vector3 m = Vector3.Cross(pc, pq);
            double v = Vector3.Dot(pa, m);
            if (v >= 0.0f)
            {
                double u = -Vector3.Dot(pb, m);
                if (u < 0.0f)
                    return newPos;
                double w = Vector3.Dot(Vector3.Cross(pq, pb), pa);
                if (w < 0.0f)
                    return newPos;
                /*if(DEBUG_COL)
                {
                    DLog(@"Actual Collision Quad %d",index);
                }*/
                return oldPos;
                //	float denom=1.0f/(u+v+w);

            }
            else
            {
                Vector3 pd = m_quad.m_d - oldPos;
                double u = -Vector3.Dot(pd, m);
                if (u < 0.0f)
                    return newPos;
                double w = Vector3.Dot(Vector3.Cross(pq, pa), pd);
                if (w < 0.0f)
                    return newPos;
                /*if(DEBUG_COL)
                {
                    DLog(@"Actual Collision Quad %d",index);
                }*/
                return oldPos;
            }

        }
    }


}
