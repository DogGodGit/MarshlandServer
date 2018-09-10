using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using XnaGeometry;

namespace MainServer.Collisions
{
    class CCollisions
    {
        const string kCOLLISION_OBJECT_COUNT = "COCOUNT";
        const string kCOLLISION_OBJECT_SPHERE = "COSPHERE";
        const string kCOLLISION_OBJECT_AACYLINDER = "COAACYLINDER";
        const string kCOLLISION_OBJECT_AABB = "COAABB";
        const string kCOLLISION_OBJECT_OBB = "COOBB";
        const string kCOLLISION_OBJECT_QUAD = "COQUAD";
        int m_numColObjStaticMeshes = 0;
        CQuadTreeNode m_obsuringObjectsRootNode;
        public CQuadTreeNode m_allObjectsRootNode;

        internal CCollisions()
        {
            m_numColObjStaticMeshes = 0;
        }

        internal void loadCollisionObjects(string filename, float minx, float maxx, float minz, float maxz)
        {
            float xext = maxx - minx;
            float zext = maxz - minz;
            float maxext = Math.Max(xext, zext);
            m_allObjectsRootNode = new CQuadTreeNode(new Vector2((maxx + minx) / 2, (maxz + minz) / 2), maxext / 2);
            m_obsuringObjectsRootNode = new CQuadTreeNode(new Vector2((maxx + minx) / 2, (maxz + minz) / 2), maxext / 2);

            string path = Path.GetDirectoryName(Application.ExecutablePath) + "/" + filename;
            StreamReader fp = null;
            try
            {
                fp = new StreamReader(path);
            }
            catch (Exception)
            {
            }

            if (fp == null)
            {
                return;
            }

            string name = "";

            while (fp.EndOfStream == false)
            {
                string line = fp.ReadLine();

                if (line.StartsWith("//"))
                {
                    name = line.Substring(2);
                    continue;
                }
                string[] stringArray = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //if there's nothing there then go to the next line
                if (stringArray == null || stringArray.Length == 0)
                {
                    continue;
                }
                string type = stringArray[0];
                if (line.Contains("END_FILE"))
                {
                    break;
                }
                if (type.Contains(kCOLLISION_OBJECT_COUNT))
                {
                    if (stringArray.Length > 1)
                    {
                        m_numColObjStaticMeshes = Convert.ToInt32(stringArray[1]);
                    }
                }
                else if (type.Contains(kCOLLISION_OBJECT_SPHERE))
                {
                    float cx, cy, cz, radius;
                    if (stringArray.Length > 5)
                    {
                        cx = (float)Convert.ToDouble(stringArray[2]);
                        cy = (float)Convert.ToDouble(stringArray[3]);
                        cz = (float)Convert.ToDouble(stringArray[4]);
                        radius = (float)Convert.ToDouble(stringArray[5]);

                        CCollision_Sphere sphere = new CCollision_Sphere(new Vector3(cx, cy, cz), radius);
                        if (stringArray[1] == "Y")
                        {
                            m_obsuringObjectsRootNode.insertObject(sphere);
                        }
                        m_allObjectsRootNode.insertObject(sphere);
                    }
                }
                else if (type.Contains(kCOLLISION_OBJECT_AACYLINDER))
                {
                    float cx, cy, cz, radius, height;
                    if (stringArray.Length > 6)
                    {
                        cx = (float)Convert.ToDouble(stringArray[2]);
                        cy = (float)Convert.ToDouble(stringArray[3]);
                        cz = (float)Convert.ToDouble(stringArray[4]);
                        radius = (float)Convert.ToDouble(stringArray[5]);
                        height = (float)Convert.ToDouble(stringArray[6]);

                        CCollision_AACylinder cylinder = new CCollision_AACylinder(new Vector3(cx, cy + height / 2, cz), radius, height);
                        if (stringArray[1] == "Y")
                        {
                            m_obsuringObjectsRootNode.insertObject(cylinder);
                        }

                        m_allObjectsRootNode.insertObject(cylinder);
                    }
                }
                else if (type.Contains(kCOLLISION_OBJECT_AABB))
                {
                    Vector3 min;
                    Vector3 max;
                    if (stringArray.Length > 7)
                    {
                        min.X = (float)Convert.ToDouble(stringArray[2]);
                        min.Y = (float)Convert.ToDouble(stringArray[3]);
                        min.Z = (float)Convert.ToDouble(stringArray[4]);
                        max.X = (float)Convert.ToDouble(stringArray[5]);
                        max.Y = (float)Convert.ToDouble(stringArray[6]);
                        max.Z = (float)Convert.ToDouble(stringArray[7]);

                        CCollision_AABB aabb = new CCollision_AABB(min, max);
                        if (stringArray[1] == "Y")
                        {
                            m_obsuringObjectsRootNode.insertObject(aabb);
                        }
                        m_allObjectsRootNode.insertObject(aabb);
                    }
                }
                else if (type.Contains(kCOLLISION_OBJECT_OBB))
                {
                    if (stringArray.Length > 11)
                    {
                        Vector3 p0, p1, p2;
                        float height;
                        p0.X = (float)Convert.ToDouble(stringArray[2]);
                        p0.Y = (float)Convert.ToDouble(stringArray[3]);
                        p0.Z = (float)Convert.ToDouble(stringArray[4]);
                        p1.X = (float)Convert.ToDouble(stringArray[5]);
                        p1.Y = (float)Convert.ToDouble(stringArray[6]);
                        p1.Z = (float)Convert.ToDouble(stringArray[7]);
                        p2.X = (float)Convert.ToDouble(stringArray[8]);
                        p2.Y = (float)Convert.ToDouble(stringArray[9]);
                        p2.Z = (float)Convert.ToDouble(stringArray[10]);
                        height = (float)Convert.ToDouble(stringArray[11]);

                        if (stringArray[1] == "Y")
                        {
                            CCollision_OBB obb = new CCollision_OBB(p0, p1, p2, height, name);
                            m_obsuringObjectsRootNode.insertObject(obb);
                            m_allObjectsRootNode.insertObject(obb);
                        }
                        else
                        {
                            CCollision_OBB obb = new CCollision_OBB(p0, p1, p2, height, name);
                            m_allObjectsRootNode.insertObject(obb);
                        }
                    }
                }
                else if (type.Contains(kCOLLISION_OBJECT_QUAD))
                {
                    if (stringArray.Length > 14)
                    {
                        Vector3 a, b, c, d;

                        a.X = (float)Convert.ToDouble(stringArray[2]);
                        a.Y = (float)Convert.ToDouble(stringArray[3]);
                        a.Z = (float)Convert.ToDouble(stringArray[4]);
                        b.X = (float)Convert.ToDouble(stringArray[5]);
                        b.Y = (float)Convert.ToDouble(stringArray[6]);
                        b.Z = (float)Convert.ToDouble(stringArray[7]);
                        c.X = (float)Convert.ToDouble(stringArray[8]);
                        c.Y = (float)Convert.ToDouble(stringArray[9]);
                        c.Z = (float)Convert.ToDouble(stringArray[10]);
                        d.X = (float)Convert.ToDouble(stringArray[11]);
                        d.Y = (float)Convert.ToDouble(stringArray[12]);
                        d.Z = (float)Convert.ToDouble(stringArray[13]);

                        CCollision_Quad quad = new CCollision_Quad(a, b, c, d);
                        if (stringArray[1] == "Y")
                        {
                            m_obsuringObjectsRootNode.insertObject(quad);
                        }
                        m_allObjectsRootNode.insertObject(quad);
                    }
                }
            }
        }


        internal static CCollisionObject ReadCollisionObjectFromString(string lineString)
        {
            string[] stringArray = lineString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string type = stringArray[0];
            CCollisionObject newObject = null;

            if (type.Contains(kCOLLISION_OBJECT_SPHERE))
            {
                float cx, cy, cz, radius;
                if (stringArray.Length > 5)
                {
                    cx = (float)Convert.ToDouble(stringArray[2]);
                    cy = (float)Convert.ToDouble(stringArray[3]);
                    cz = (float)Convert.ToDouble(stringArray[4]);
                    radius = (float)Convert.ToDouble(stringArray[5]);

                    CCollision_Sphere sphere = new CCollision_Sphere(new Vector3(cx, cy, cz), radius);

                    newObject = sphere;
                }
            }
            else if (type.Contains(kCOLLISION_OBJECT_AACYLINDER))
            {
                float cx, cy, cz, radius, height;
                if (stringArray.Length > 6)
                {
                    cx = (float)Convert.ToDouble(stringArray[2]);
                    cy = (float)Convert.ToDouble(stringArray[3]);
                    cz = (float)Convert.ToDouble(stringArray[4]);
                    radius = (float)Convert.ToDouble(stringArray[5]);
                    height = (float)Convert.ToDouble(stringArray[6]);

                    CCollision_AACylinder cylinder = new CCollision_AACylinder(new Vector3(cx, cy + height / 2, cz), radius, height);

                    newObject = cylinder;
                }
            }
            else if (type.Contains(kCOLLISION_OBJECT_AABB))
            {
                Vector3 min;
                Vector3 max;
                if (stringArray.Length > 7)
                {
                    min.X = (float)Convert.ToDouble(stringArray[2]);
                    min.Y = (float)Convert.ToDouble(stringArray[3]);
                    min.Z = (float)Convert.ToDouble(stringArray[4]);
                    max.X = (float)Convert.ToDouble(stringArray[5]);
                    max.Y = (float)Convert.ToDouble(stringArray[6]);
                    max.Z = (float)Convert.ToDouble(stringArray[7]);

                    CCollision_AABB aabb = new CCollision_AABB(min, max);

                    newObject = aabb;
                }
            }
            else if (type.Contains(kCOLLISION_OBJECT_OBB))
            {
                if (stringArray.Length > 11)
                {
                    Vector3 p0, p1, p2;
                    float height;
                    p0.X = (float)Convert.ToDouble(stringArray[2]);
                    p0.Y = (float)Convert.ToDouble(stringArray[3]);
                    p0.Z = (float)Convert.ToDouble(stringArray[4]);
                    p1.X = (float)Convert.ToDouble(stringArray[5]);
                    p1.Y = (float)Convert.ToDouble(stringArray[6]);
                    p1.Z = (float)Convert.ToDouble(stringArray[7]);
                    p2.X = (float)Convert.ToDouble(stringArray[8]);
                    p2.Y = (float)Convert.ToDouble(stringArray[9]);
                    p2.Z = (float)Convert.ToDouble(stringArray[10]);
                    height = (float)Convert.ToDouble(stringArray[11]);

                    CCollision_OBB obb = new CCollision_OBB(p0, p1, p2, height, "unknown");
                    newObject = obb;
                }
            }
            else if (type.Contains(kCOLLISION_OBJECT_QUAD))
            {
                if (stringArray.Length > 14)
                {
                    Vector3 a, b, c, d;

                    a.X = (float)Convert.ToDouble(stringArray[2]);
                    a.Y = (float)Convert.ToDouble(stringArray[3]);
                    a.Z = (float)Convert.ToDouble(stringArray[4]);
                    b.X = (float)Convert.ToDouble(stringArray[5]);
                    b.Y = (float)Convert.ToDouble(stringArray[6]);
                    b.Z = (float)Convert.ToDouble(stringArray[7]);
                    c.X = (float)Convert.ToDouble(stringArray[8]);
                    c.Y = (float)Convert.ToDouble(stringArray[9]);
                    c.Z = (float)Convert.ToDouble(stringArray[10]);
                    d.X = (float)Convert.ToDouble(stringArray[11]);
                    d.Y = (float)Convert.ToDouble(stringArray[12]);
                    d.Z = (float)Convert.ToDouble(stringArray[13]);

                    CCollision_Quad quad = new CCollision_Quad(a, b, c, d);

                    newObject = quad;
                }
            }
            return newObject;
        }

        internal Vector3 checkCollisions(Vector3 oldPos, Vector3 newPos, float radius, float height, bool ipOnly)
        {
            bool ignoreY = true;
            Vector3 revisedPos = newPos;
            Vector3 boundingSphereCentre = (oldPos + newPos) * 0.5f;
            double boundingSphereRadius = (oldPos - newPos).Length();
            radius = 0.0f; // radius at 0 means this check acts like a ray cast - using a radius means projecting a circle instead

            Vector2 boundingCircleCentre = new Vector2(boundingSphereCentre.X, boundingSphereCentre.Z);
            List<CCollisionObject> colObjsStaticMeshes = new List<CCollisionObject>(100);
            getCollisionObjects(m_allObjectsRootNode, colObjsStaticMeshes, boundingCircleCentre, boundingSphereRadius);

            for (int i = 0; i < colObjsStaticMeshes.Count; i++)
            {
                CCollisionObject currentObject = colObjsStaticMeshes[i];
                if (currentObject.checkBoundingSphere(boundingSphereCentre, boundingSphereRadius, ignoreY))
                {
                    switch (currentObject.m_objectType)
                    {
                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AACYLINDER:
                            revisedPos = ((CCollision_AACylinder)currentObject).checkIntersection(oldPos, revisedPos, ignoreY, radius);
                            break;

                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AABB:
                            revisedPos = ((CCollision_AABB)currentObject).checkIntersection(oldPos, revisedPos, ignoreY, radius);
                            break;

                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_OBB:
                            revisedPos = ((CCollision_OBB)currentObject).checkIntersection(oldPos, revisedPos, ignoreY, radius);
                            break;

                        default:
                            break;
                    }
                }
            }
            return revisedPos;
        }

        // Check for collisions with the passed radius (its center and radius)
        internal bool checkCollisions(Vector3 newPos, float radius)
        {
            bool collison = false;
            bool ignoreY = true;
            Vector3 revisedPos = newPos;
            Vector3 boundingSphereCentre = newPos;
            double boundingSphereRadius = radius + 0.5f;

            Vector2 boundingCircleCentre = new Vector2(boundingSphereCentre.X, boundingSphereCentre.Z);
            List<CCollisionObject> colObjsStaticMeshes = new List<CCollisionObject>(100);
            getCollisionObjects(m_allObjectsRootNode, colObjsStaticMeshes, boundingCircleCentre, boundingSphereRadius);

            for (int i = 0; i < colObjsStaticMeshes.Count; i++)
            {
                CCollisionObject currentObject = colObjsStaticMeshes[i];
                if (currentObject.checkBoundingSphere(boundingSphereCentre, boundingSphereRadius, ignoreY))
                {
                    switch (currentObject.m_objectType)
                    {
                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AACYLINDER:
                            collison = ((CCollision_AACylinder)currentObject).checkCircleInterSection(revisedPos, radius / 2.0f); // Erm...
                            break;

                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AABB:
                            collison = ((CCollision_AABB)currentObject).checkCircleInterSection(revisedPos, radius);
                            break;

                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_OBB:
                            collison = ((CCollision_OBB)currentObject).checkCircleInterSection(revisedPos, radius);
                            break;

                        default:
                            break;
                    }
                }

                // Stop when a collision is found
                if (collison)
                {
                    break;
                }
            }
            return collison;
        }

        internal void getCollisionObjects(CQuadTreeNode node, List<CCollisionObject> list, Vector2 centre, double radius)
        {
            if ((node.m_centre - centre).Length() < radius + node.m_boundingCircle.m_radius)
            {
                list.AddRange(node.pObjList);
                for (int i = 0; i < 4; i++)
                {
                    if (node.pChild[i] != null)
                    {
                        getCollisionObjects(node.pChild[i], list, centre, radius);
                    }
                }
            }
        }

        // Used by server form to draw the zones colliders
        internal void getAllCollisionObjects(CQuadTreeNode node, List<CCollisionObject> list)
        {
            list.AddRange(node.pObjList);
            for (int i = 0; i < 4; i++)
            {
                if (node.pChild[i] != null)
                {
                    getAllCollisionObjects(node.pChild[i], list);
                }
            }
        }

        internal Vector3 checkIntersections(Vector3 oldPos, Vector3 newPos, bool ignoreY, float radius)
        {
            Vector3 revisedPos = newPos;
            Vector3 boundingSphereCentre = (oldPos + newPos) * 0.5f;
            Vector2 boundingCircleCentre = new Vector2(boundingSphereCentre.X, boundingSphereCentre.Z);
            double boundingSphereRadius = (oldPos - newPos).Length();
            List<CCollisionObject> obscuringObjects = new List<CCollisionObject>(100);
            getCollisionObjects(m_obsuringObjectsRootNode, obscuringObjects, boundingCircleCentre, boundingSphereRadius);
            radius = 0.0f; // radius at 0 means this check acts like a ray cast - using a radius means projecting a circle instead

            for (int i = 0; i < obscuringObjects.Count; i++)
            {
                if (obscuringObjects[i].checkBoundingSphere(boundingSphereCentre, boundingSphereRadius, ignoreY))
                {
                    switch (obscuringObjects[i].m_objectType)
                    {
                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AACYLINDER:
                            revisedPos = ((CCollision_AACylinder)obscuringObjects[i]).checkIntersection(oldPos, revisedPos, ignoreY, radius);
                            break;

                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AABB:
                            revisedPos = ((CCollision_AABB)obscuringObjects[i]).checkIntersection(oldPos, revisedPos, ignoreY, radius);
                            break;

                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_OBB:
                            revisedPos = ((CCollision_OBB)obscuringObjects[i]).checkIntersection(oldPos, revisedPos, ignoreY, radius);
                            break;

                        default:
                            break;
                    }
                }
            }
            return revisedPos;
        }
    }
}
