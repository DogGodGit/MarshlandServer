using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Microsoft.Xna.Framework;

namespace MainServer.Pathing
{
    class ASPartition
    {
        List<ASTriangle> m_triangles = new List<ASTriangle>();

        internal void AddTriangleToList(ASTriangle newTriangle)
        {
            m_triangles.Add(newTriangle);
        }
    }
}
