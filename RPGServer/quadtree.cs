/*
 *  octree.mm
 *  BspMaze
 *
 *  Created by James McLaren on 17/11/2009.
 *  Copyright 2009 One Thumb Mobile. All rights reserved.
 *
 */
using System.Collections.ObjectModel;
using System;
using XnaGeometry;
namespace MainServer
{

    class CCircle
    {

        public CCircle(Vector2 centre, double radius)
        {
            m_centre = centre;
            m_radius = radius;
        }
        public Vector2 m_centre;
        public double m_radius;
    };

    struct CQuadTreeObject
    {
        public CQuadTreeObject(int objectIndex, CCircle boundingCircle)
        {
            m_objectIndex = objectIndex;
            m_boundingCircle = boundingCircle;
        }
        public CCircle m_boundingCircle;
        int m_objectIndex;
    };


    class CQuadTreeNode
    {



        Vector2 m_centre;
        float m_halfWidth;
        CQuadTreeNode[] pChild = new CQuadTreeNode[4];
        Collection<CQuadTreeObject> pObjList;



        public CQuadTreeNode(Vector2 centre, float halfWidth)
        {

            m_centre = centre;
            m_halfWidth = halfWidth;
            for (int i = 0; i < 4; i++)
            {
                pChild[i] = null;
            }
            pObjList = new Collection<CQuadTreeObject>();

        }

        public void insertObject(CQuadTreeObject pObject)
        {
            
            int index = 0;
            bool straddle = false;
            double dx = pObject.m_boundingCircle.m_centre.X - m_centre.X;
            if (Math.Abs(dx) <  pObject.m_boundingCircle.m_radius)
            {
                straddle = true;
            }
            else
            {
                if (dx > 0)
                {
                    index |= 1;
                }
                double dy = pObject.m_boundingCircle.m_centre.Y - m_centre.Y;
                if (Math.Abs(dy) <  pObject.m_boundingCircle.m_radius)
                {
                    straddle = true;
                }
                else
                {
                    if (dy > 0)
                    {
                        index |= 2;
                    }
                }
            }
            if (!straddle )
            {
                if (m_halfWidth == 1)
                {
                    pObjList.Add(pObject);
                }

                else if (pChild[index] == null)
                    {
                        float offx, offy, step;
                        step = m_halfWidth / 2;
                        if ((index & 1) == 0)
                        {
                            offx = -step;

                        }
                        else
                        {
                            offx = step;
                        }

                        if ((index & 2) == 0)
                        {
                            offy = -step;

                        }
                        else
                        {
                            offy = step;
                        }
                        Vector2 newCentre = m_centre + new Vector2(offx, offy);

                        pChild[index] = new CQuadTreeNode(newCentre, step);
                        pChild[index].insertObject(pObject);
                    }


            }
            else
            {
                System.Diagnostics.Debug.Print("object " + pObject.m_boundingCircle.m_centre+","+pObject.m_boundingCircle.m_radius + " node centre=" + m_centre + " halfwidth=" + m_halfWidth);
                pObjList.Add(pObject);
            }
        }

    };
}