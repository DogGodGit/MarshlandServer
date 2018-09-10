using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using XnaGeometry;


namespace MainServer
{
    class ASPathingEntity
    {
        internal Vector3 m_startPoint = new Vector3(0);
        Vector3 m_endPoint = new Vector3(0);
        ASPathFinder.AS_PATHING_ERROR m_lastError = ASPathFinder.AS_PATHING_ERROR.NONE;
        ASTriangle m_startingTriangle = null;
        ASTriangle m_endTriangle = null;

        List<ASNode> m_openList = new List<ASNode>();
        List<ASNode> m_closedList = new List<ASNode>();


        List<Vector3> m_innitialPath = null;
        List<Vector3> m_path =null;
        List<ASNode> m_fullPath = null;
        List<ASNode> m_shortenedPath = null;
        ASNode m_endNode = null;
        
        internal Vector3 StartPoint
        {
            get { return m_startPoint; }
        }
        internal Vector3 EndPoint
        {
            get { return m_endPoint; }
        }
        internal ASNode EndNode
        {
            get { return m_endNode; }
        }
        internal ASPathFinder.AS_PATHING_ERROR LastError
        {
            set { m_lastError = value; }
            get { return m_lastError; }
        }
        internal ASTriangle StartingTriangle
        {
            get { return m_startingTriangle; }
            set { m_startingTriangle = value; }
        }
        internal ASTriangle EndTriangle
        {
            get { return m_endTriangle; }
            set { m_endTriangle = value; }
        }

        internal List<ASNode> OpenList
        {
            get { return m_openList; }
        }
        internal List<ASNode> ClosedList
        {
            get { return m_closedList; }
        }

        internal List<Vector3> Path
        {
             get { 


                 return m_path;
             }
             set {
                
                 m_path = value; 
             }
        }
        internal List<Vector3> InnitialPath
        {
            set { m_innitialPath = value; }
        }
        internal List<ASNode> FullPath
        {
            set { m_fullPath = value; }
        }
        internal List<ASNode> ShortenedPath
        {
            set { m_shortenedPath = value; }
        }

        internal void Reset()
        {
            m_openList.Clear();
            m_closedList.Clear();
            m_endTriangle = null;
            m_startingTriangle = null;
            m_path = null;
            m_shortenedPath = null;
            m_fullPath = null;
            m_endNode = null;
            m_innitialPath = null;
            m_lastError = ASPathFinder.AS_PATHING_ERROR.NONE;
        }
       
        internal void SetUpForSearch(Vector3 currentPosition,Vector3 destination)
        {

           
              
            
            Reset();

            m_startPoint = currentPosition;
            m_endPoint = destination;
        }
        internal ASPathingEntity()
        {

        }

        internal bool OnRootTo(Vector3 position)
        {
            if (Path != null && Path.Count > 0)
            {
                Vector3 lastPoint = Path.Last();
                if((position - lastPoint).LengthSquared() < ServerControlledEntity.MAX_SQUARED_COLLISION_ERROR)
                {
                    return true;
                }
            }
            if(Path == null){
                if ((position - m_endPoint).LengthSquared() < ServerControlledEntity.MAX_SQUARED_COLLISION_ERROR)
                {
                    return true;
                }
            }

            return false;

        }
    }
}
