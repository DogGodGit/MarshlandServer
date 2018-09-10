using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaGeometry;


namespace AStarMesh
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
        List<ASNode> m_optimisedPath = null;
        ASNode m_endNode = null;

        bool m_pathCompleted = true;

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
            set { m_endNode = value; }
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
             get { return m_path; }
             set { m_path = value; }
        }
        internal List<Vector3> InnitialPath
        {
            get { return m_innitialPath; }
            set { m_innitialPath = value; }
        }
        internal List<ASNode> FullPath
        {
            get { return m_fullPath; }
            set { m_fullPath = value; }
        }
        internal List<ASNode> ShortenedPath
        {
            get { return m_shortenedPath; }
            set { m_shortenedPath = value; }
        }
        internal List<ASNode> OptimisedPath
        {
            get { return m_optimisedPath; }
            set { m_optimisedPath = value; }
        }
        internal bool PathComplete
        {
            get { return m_pathCompleted; }
            set { m_pathCompleted = value; }
        }

        internal void Reset()
        {
            m_pathCompleted = false;
            m_openList.Clear();
            m_closedList.Clear();
            m_endTriangle = null;
            m_startingTriangle = null;
            m_path = null;
            m_shortenedPath = null;
            m_fullPath = null;
            m_endNode = null;
            m_innitialPath = null;
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
    }
}
