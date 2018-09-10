using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaGeometry;

namespace MainServer.Collisions
{



class CQuadTreeNode
{

public	CQuadTreeNode(Vector2 centre,float halfWidth)
    {

        m_centre = centre;
        m_halfWidth = halfWidth;
    
        m_boundingCircle = new CCircle(centre, (float)(Math.Sqrt(2) * halfWidth));
        m_min = m_centre - new Vector2(halfWidth, halfWidth);
        m_max = m_centre + new Vector2(halfWidth, halfWidth);
        for (int i = 0; i < 4; i++)
        {
            pChild[i] = null;
        }
        pObjList.Clear();

    }


	public Vector2 m_centre;
	public float m_halfWidth;
	public CCircle m_boundingCircle;
    public Vector2 m_min;
    public Vector2 m_max;
	public CQuadTreeNode[] pChild=new CQuadTreeNode[4];
    public List<CCollisionObject> pObjList = new List<CCollisionObject>();
	public void insertObject(CCollisionObject pObject)
    {
        int index = 0;
        bool straddle = false;
        double dx = pObject.m_bounding_Circle.m_centre.X - m_centre.X;

        if (Math.Abs(dx) < pObject.m_bounding_Circle.m_radius & Math.Abs(dx)<pObject.m_halfWidth)
        {
            straddle = true;
        }
        else
        {
            if (dx > 0)
            {
                index |= 1;
            }
            double dz = pObject.m_bounding_Circle.m_centre.Y - m_centre.Y;

            if (Math.Abs(dz) < pObject.m_bounding_Circle.m_radius & Math.Abs(dz) < pObject.m_halfDepth)
            {
                straddle = true;
            }
            else
            {
                if (dz > 0)
                {
                    index |= 2;
                }
            }
        }
        if (!straddle && m_halfWidth > 1.0f)
        {
            if (pChild[index] == null)
            {
                float offx, offz, step;
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
                    offz = -step;

                }
                else
                {
                    offz = step;
                }
                Vector2 newCentre = m_centre + new Vector2(offx, offz);

                pChild[index] = new CQuadTreeNode(newCentre, step);
            }
            pChild[index].insertObject(pObject);
        }
        else
        {


            pObjList.Add(pObject);
        }


    }

};

}
