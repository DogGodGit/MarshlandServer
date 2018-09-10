using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using XnaGeometry; 


namespace AStarMesh
{
    class ASPathFinder
    {

        ASMap m_theMap = null;

        internal ASPathingEntity pather;

        public ASMap TheMap
        {
            get { return m_theMap; }
        }
        internal ASPathFinder()
        {
            m_theMap = new ASMap();
            pather = new ASPathingEntity();
        }

        internal enum AS_PATHING_ERROR
        {
            NONE,
            INVALID_END,
            INVALID_START,
            NO_PATH,
            ON_SAME_TRIANGLE,
            PATHING_LIMIT_EXCEEDED
        };
        /*internal Vector3 m_startPoint;
        internal Vector3 m_endPoint;
        internal AS_PATHING_ERROR m_lastError = AS_PATHING_ERROR.NONE;
        internal ASTriangle m_startingTriangle = null;
        internal ASTriangle m_endTriangle = null;

        internal List<ASNode> m_openList = new List<ASNode>();
        internal List<ASNode> m_closedList = new List<ASNode>();

        internal List<Vector3> m_path = null;
        internal List<Vector3> m_shortenedPath = null;
        internal List<Vector3> m_optimisedPath = null;
        */
        internal void NextStep()
        {
            if (pather.Path != null)
            {
                return;
            }
            pather.Path = GetPath(pather);

        }
        internal void Reset()
        {
            pather.Reset();
           /* m_openList.Clear();
            m_closedList.Clear();
            m_endTriangle = null;
            m_startingTriangle = null;
            m_path = null;
            m_shortenedPath = null;*/
        }

        /// <summary>
        /// Returns a path of points from start point to end point
        /// </summary>
        /// <param name="startPoint">the position the entity started at</param>
        /// <param name="endPoint">the position the entity is aiming for</param>
        /// <param name="error">Was the path completed successfully, if not what was the problem encountered</param>
        /// <param name="openList">will normally pass in an empty list, in the event that the algorithm has taken too long this will not be cleared and can be passed back next frame</param>
        /// <param name="closedList">will normally pass in an empty list, in the event that the algorithm has taken too long this will not be cleared and can be passed back next frame</param>
        /// <returns></returns>
        List<Vector3> GetPath(ASPathingEntity pathingObject)
        {
            DateTime timeAtStart = DateTime.UtcNow;
            Vector3 startPoint = pathingObject.StartPoint;
            Vector3 endPoint = pathingObject.EndPoint;

            List<ASNode> openList = pathingObject.OpenList;
            List<ASNode> closedList = pathingObject.ClosedList;
            AS_PATHING_ERROR error = AS_PATHING_ERROR.NONE;
            ASTriangle startingTriangle = pathingObject.StartingTriangle;
            ASTriangle endTriangle = pathingObject.EndTriangle;
            ASNode endNode = null;
            //find starting triangle - only if the open list is empty
            if (openList.Count == 0)
            {
                //get it's partition
                //ASPartition closestPartition = m_theMap.GetPartitionForPosition(startPoint);
                //test against the polys in the partition
                if (startingTriangle == null)
                {
                    startingTriangle = m_theMap.GetClosestTriangleForPosition(startPoint);//closestPartition.GetClosestTriangleToPosition(startPoint);
                    if (startingTriangle == null)
                    {
                        error = AS_PATHING_ERROR.INVALID_START;
                        pathingObject.LastError = error;
                        return null;
                    }
                    pathingObject.StartingTriangle = startingTriangle;
                    //add the start point to the open List
                    double distanceToPoint = (startPoint - endPoint).Length();
                    ASNode startingNode = new ASNode(startingTriangle, null, 0.0f, distanceToPoint, startPoint);
                    openList.Add(startingNode);
                }
                
            }

            //find ending triangle
            //get it's partition
            //ASPartition closestEndPartition = m_theMap.GetPartitionForPosition(endPoint);
            //test against the polys in the partition
            if (endTriangle == null)
            {
                endTriangle = m_theMap.GetClosestTriangleForPosition(endPoint);
                pathingObject.EndTriangle = endTriangle;
            }

            //check there is an end position
            if (endTriangle == null)
            {
                error = AS_PATHING_ERROR.INVALID_END;
                pathingObject.LastError = error;
                return null;
            }

            if (endTriangle == startingTriangle)
            {
                error = AS_PATHING_ERROR.ON_SAME_TRIANGLE;
                pathingObject.LastError = error;
                return null;
            }

            bool pathFound = false;
            //how many passes can it do
            int iterationAllowance = 1;
            int currentIteration = 0;
            //while the open list has entries and the exit is not reached, and the time allocated has not been exhasted
            while (pathFound==false &&
                openList.Count>0&&
                currentIteration < iterationAllowance&&
                endNode==null

                )
                {
                    currentIteration++;
                    ASNode currentNode = openList[0];
                    ASTriangle currentTriangle = currentNode.Triangle;
                //for all links
                    for (int i = 0; i < 3; i++)
                    {
                        //get the link for this side
                        ASTriangle currentLink = currentTriangle.GetLink(i);
                        if (currentLink == null)
                        {
                            continue;
                        }
                        //Get the adjoining line
                        Line3D adjoiningLine = currentTriangle.GetSide(i);
                        //work out it's cost and estimate
                        // cost = previous entry point to current Side MidPoint
                        double cost = currentNode.Cost + (currentNode.EntryPoint - adjoiningLine.MidPoint).Length();
                        // heuristic = distance from entry line midpoint to endpoint
                        double heuristic = (endPoint - adjoiningLine.MidPoint).Length()*1.5f;
                        //create the thing - link to previous
                        Vector3 lineEntry = adjoiningLine.ClosestPointOnSegmentFromPoint(currentNode.EntryPoint);
                        ASNode newNode = new ASNode(currentLink, currentNode, cost, heuristic, lineEntry);

                        
                        //add to open List in correct place
                        bool nodeAdded = false;
                        for (int currentClosed = 0; currentClosed < closedList.Count && nodeAdded == false; currentClosed++)
                        {
                            ASNode comparingNode = closedList[currentClosed];
                            if (comparingNode == null)
                            {
                                continue;
                            }
                            if (comparingNode.Triangle == newNode.Triangle)
                            {
                                nodeAdded = true;
                            }
                        }
                        for (int currentOpen = 0; currentOpen < openList.Count && nodeAdded==false; currentOpen++)
                        {
                            ASNode comparingNode = openList[currentOpen];
                            if(comparingNode==null)
                            {
                                continue;
                            }
                            if (comparingNode.Triangle == newNode.Triangle)
                            {
                                nodeAdded = true;
                            }
                            if (comparingNode.TotalCost > newNode.TotalCost)
                            {
                                nodeAdded = true;
                                openList.Insert(currentOpen, newNode);
                                break;
                            }
                        }
                        //if it has the largest total cost add it to the end
                        if (nodeAdded == false)
                        {
                            openList.Add(newNode);
                        }

                        if (currentLink == endTriangle)
                        {
                            pathFound = true;
                            endNode = newNode;
                            break;
                        }

                        
                    }
                    //add current Link to the closed List
                    //closedList.Add(currentNode);
                    closedList.Insert(0, currentNode);
                 /* bool addedToClosed = false;
                    for (int currentClosed = 0; currentClosed < closedList.Count && addedToClosed == false; currentClosed++)
                    {
                        ASNode comparingNode = closedList[currentClosed];
                        if (comparingNode == null)
                        {
                            continue;
                        }
                        if (comparingNode.TotalCost > currentNode.TotalCost)
                        {
                            addedToClosed = true;
                            closedList.Insert(currentClosed, currentNode);
                        }
                    }
                    if (addedToClosed == false)
                    {
                        closedList.Add(currentNode);

                    }*/
                    openList.Remove(currentNode);

            //go to next
            }


            //if the path was found
            if (pathFound == true)
            {
                ASNode currentPathNode = endNode;
                //add all of the nodes to a list
                List<ASNode> fullNodeList = new List<ASNode>();
                List<ASNode> shortenedNodeList = new List<ASNode>();
                pathingObject.FullPath = fullNodeList;
                pathingObject.ShortenedPath = shortenedNodeList;
                currentPathNode = endNode;
                while (currentPathNode != null)
                {
                    fullNodeList.Add(currentPathNode);
                    currentPathNode = currentPathNode.Previous;
                }
                fullNodeList.Reverse();
                DateTime innitialPathTime = DateTime.UtcNow;
                GetShortestNodeListUsingReferenceNodesWithLineOptimising(fullNodeList, fullNodeList, startPoint, endPoint);
                //check the original points
                //return the path
                List<Vector3> pointsList = new List<Vector3>();
                currentPathNode = endNode;
                //currentPathNode = endNode;
                pointsList.Add(endPoint);
                while (currentPathNode != null)
                {
                    Vector3 newPoint = currentPathNode.EntryPoint;
                    pointsList.Add(newPoint);
                    currentPathNode = currentPathNode.Previous;
                }
                //DateTime innitialPathTime = DateTime.Now;
                pathingObject.InnitialPath = pointsList;
                
                //get the shortened path
                shortenedNodeList = GetShortestNodeListTwoSidedTo(fullNodeList, startPoint, endPoint);//GetShortestNodeListTo(fullNodeList, startPoint, endPoint);
              
                List<Vector3>  shortenedPath=null;
                if (shortenedPath == null)
                {
                    shortenedPath = new List<Vector3>();
                }
                shortenedPath.Clear();
                shortenedPath.Add(startPoint);
                //work out this shorter path
                for (int shortListIndex = 0; shortListIndex < shortenedNodeList.Count; shortListIndex++)
                {
                    currentPathNode = shortenedNodeList[shortListIndex];
                    Vector3 newPoint = currentPathNode.EntryPoint;
                    shortenedPath.Add(newPoint);
                }
                shortenedPath.Add(endPoint);
                    ////////////////////////////////////////////////////put the points together
                //return the path
              /*  List<Vector3> pointsList = new List<Vector3>();
                
                currentPathNode = endNode;
                pointsList.Add(endPoint);
                while (currentPathNode != null)
                {
                    Vector3 newPoint = currentPathNode.EntryPoint;
                    pointsList.Add(newPoint);
                    currentPathNode = currentPathNode.Previous;
                }
                */
                DateTime fullPathTime = DateTime.UtcNow;
                TimeSpan timeinnitialPath = innitialPathTime-timeAtStart;
                TimeSpan timefullPathTime = fullPathTime - timeAtStart;
                 //innitialPathTime.Subtract(timeAtStart);
                 //fullPathTime.Subtract(timeAtStart);
                Console.WriteLine("time to innitial Path " + timeinnitialPath.TotalMilliseconds);
                Console.WriteLine("time to final Path " + timefullPathTime.TotalMilliseconds);
                 Console.WriteLine("itterations " + currentIteration);
                Console.WriteLine("ClosedList size = "+closedList.Count);
                Console.WriteLine("OpenList size = " + openList.Count);
                //openList.Clear();
                //closedList.Clear();
                return shortenedPath;

            }
            pathingObject.LastError = error;
            //otherwise what was the error
            //the pathing failed this cycle
            return null;
        }
        /// <summary>
        /// Returns a list of Nodes removing any that do not need to be passed through
        /// judges from start to end
        /// </summary>
        /// <param name="fullNodeList"></param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        List<ASNode> GetShortestNodeListTo(List<ASNode> fullNodeList, Vector3 startPoint, Vector3 endPoint)
        {
            List<ASNode> shortenedNodeList = new List<ASNode>();
            //where was the direct Path to the destination broken last
            int lastPathBreak = 0;
            ASNode lastPathBreakNode = null;
            Vector3 currentLineStart = startPoint;
            //make it go from start to finnish
            
            for (int originalListIndex = 0; originalListIndex < fullNodeList.Count; originalListIndex++)
            {
                //check if each step to this node can be skipped
                ASNode currentBaseNode = fullNodeList[originalListIndex];
                bool directPath = true;
                Vector3 currentLineEnd = currentBaseNode.EntryPoint;
                //for each node from the last break
                for (int originalList2ndIndex = lastPathBreak + 1; originalList2ndIndex < originalListIndex && directPath == true; originalList2ndIndex++)
                {
                    ASNode currentCompareNode = fullNodeList[originalList2ndIndex];
                    if (currentCompareNode == currentBaseNode)
                    {
                        continue;
                    }
                    //does this line intersect the link line
                    bool linePassesThrough = currentCompareNode.LinePassesThroughLinkedLine(currentLineStart, currentLineEnd,true);

                    //if not it must be broken in the last link
                    if (linePassesThrough == false)
                    {
                        directPath = false;
                        lastPathBreak = originalListIndex - 1;
                        if (lastPathBreak >= 0)
                        {
                            lastPathBreakNode = fullNodeList[lastPathBreak];
                            shortenedNodeList.Add(lastPathBreakNode);
                            currentLineStart = lastPathBreakNode.EntryPoint;
                        }
                    }
                }
            }
            Vector3 pathLineEnd = endPoint;
            bool directEndPath = true;
            //check to the endPoint
            for (int originalList2ndIndex = lastPathBreak + 1; originalList2ndIndex < fullNodeList.Count && directEndPath == true; originalList2ndIndex++)
            {
                ASNode currentCompareNode = fullNodeList[originalList2ndIndex];

                //does this line intersect the link line
                bool linePassesThrough = currentCompareNode.LinePassesThroughLinkedLine(currentLineStart, pathLineEnd,true);

                //if not it must be broken in the last link
                if (linePassesThrough == false)
                {
                    directEndPath = false;
                    lastPathBreak = fullNodeList.Count - 1;
                    if (lastPathBreak >= 0)
                    {
                        lastPathBreakNode = fullNodeList[lastPathBreak];
                        shortenedNodeList.Add(lastPathBreakNode);
                        currentLineStart = lastPathBreakNode.EntryPoint;
                    }
                }
            }
            return shortenedNodeList;
        }
        /// <summary>
        /// Returns a list of Nodes removing any that do not need to be passed through
        /// jusges from start to min, end to mid, then combines
        /// </summary>
        /// <param name="fullNodeList"></param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        List<ASNode> GetShortestNodeListTwoSidedTo(List<ASNode> fullNodeList, Vector3 startPoint, Vector3 endPoint)
        {
            List<ASNode> shortenedNodeList = new List<ASNode>();
            //where was the direct Path to the destination broken last
           // int lastPathBreak = 0;
           // ASNode lastPathBreakNode = null;
            Vector3 currentLineStart = startPoint;
            List<ASNode> endToMid = new List<ASNode>();
            List<ASNode> startToMid = new List<ASNode>();
            for (int i = 0; i < fullNodeList.Count; i++)
            {
                if (i > fullNodeList.Count / 2)
                {
                    endToMid.Add(fullNodeList[i]);
                }
                else
                {
                    startToMid.Add(fullNodeList[i]);
                }
            }
            //startToMid.Reverse();
            endToMid.Reverse();

            Vector3 endMid=startPoint;
            Vector3 startMid=endPoint;

            ASNode endMidNode = null;
            ASNode startMidNode = null;

            if(startToMid.Count>0)
            {
                startMidNode = startToMid.Last();
                endMid = startToMid.Last().EntryPoint;
            }
            if (endToMid.Count > 0)
            {
                endMidNode = endToMid.Last();
                startMid = endToMid.Last().EntryPoint;
            }

            List<ASNode> shortEndToMid = GetShortestNodeListTo(endToMid, endPoint, endMid);
            List<ASNode> shortStartToMid =  GetShortestNodeListTo(startToMid, startPoint, startMid);
                //make it go from start to finnish
                // fullNodeList.Reverse();
            shortenedNodeList = shortStartToMid;

            if (shortStartToMid.Count == 0 || shortStartToMid.Last() != startMidNode)
            {
                shortenedNodeList.Add(startMidNode);
            }
            if(endMidNode!=startMidNode)
            {
                if ((shortEndToMid.Count==0)||(shortEndToMid.Last() != endMidNode))
                {
                    shortenedNodeList.Add(endMidNode);
                }
            }
            shortEndToMid.Reverse();
            for (int i = 0; i < shortEndToMid.Count; i++)
            {
                shortenedNodeList.Add(shortEndToMid[i]);
            }
            shortenedNodeList = GetShortestNodeListUsingReferenceNodes(shortenedNodeList, fullNodeList,startPoint, endPoint);
            GetShortestNodeListUsingReferenceNodesWithLineOptimising(shortenedNodeList, fullNodeList, startPoint, endPoint);
                return shortenedNodeList;
        }
        /// <summary>
        /// Reduces a node list while checking all items pass through a separate list of nodes
        /// Used For optimising an already shortened list
        /// </summary>
        /// <param name="fullNodeList"></param>
        /// <param name="refNodeList"></param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        List<ASNode> GetShortestNodeListUsingReferenceNodes(List<ASNode> fullNodeList, List<ASNode> refNodeList, Vector3 startPoint, Vector3 endPoint)
        {
            List<ASNode> shortenedNodeList = new List<ASNode>();
            //where was the direct Path to the destination broken last
            int lastPathBreak = 0;
            int lastrefPathBreak = 0;
            ASNode lastPathBreakNode = null;
            Vector3 currentLineStart = startPoint;
            //make it go from start to finnish
            for (int originalListIndex = 0; originalListIndex < fullNodeList.Count; originalListIndex++)
            {
                //check if each step to this node can be skipped
                ASNode currentBaseNode = fullNodeList[originalListIndex];
                bool directPath = true;
                Vector3 currentLineEnd = currentBaseNode.EntryPoint;
                //for each node from the last break
                for (int originalList2ndIndex = lastrefPathBreak + 1; originalList2ndIndex < refNodeList.Count && directPath == true; originalList2ndIndex++)
                {
                    ASNode currentCompareNode = refNodeList[originalList2ndIndex];
                    if (currentCompareNode == currentBaseNode)
                    {
                        break;
                    }
                    //does this line intersect the link line
                    bool linePassesThrough = currentCompareNode.LinePassesThroughLinkedLine(currentLineStart, currentLineEnd,true);

                    //if not it must be broken in the last link
                    if (linePassesThrough == false)
                    {
                        directPath = false;
                        lastPathBreak = originalListIndex - 1;
                        
                        if (lastPathBreak >= 0)
                        {
                            lastPathBreakNode = fullNodeList[lastPathBreak];
                            for (int i = 0; i < refNodeList.Count ; i++)
                            {
                                if (refNodeList[i] == lastPathBreakNode)
                                {
                                    lastrefPathBreak = i;
                                    break;
                                }
                            }
                            shortenedNodeList.Add(lastPathBreakNode);
                            currentLineStart = lastPathBreakNode.EntryPoint;
                        }
                    }
                }
            }
            Vector3 pathLineEnd = endPoint;
            bool directEndPath = true;
            //check to the endPoint
            for (int originalList2ndIndex = lastPathBreak + 1; originalList2ndIndex < fullNodeList.Count && directEndPath == true; originalList2ndIndex++)
            {
                ASNode currentCompareNode = fullNodeList[originalList2ndIndex];

                //does this line intersect the link line
                bool linePassesThrough = currentCompareNode.LinePassesThroughLinkedLine(currentLineStart, pathLineEnd,true);

                //if not it must be broken in the last link
                if (linePassesThrough == false)
                {
                    directEndPath = false;
                    lastPathBreak = fullNodeList.Count - 1;
                    if (lastPathBreak >= 0)
                    {
                        lastPathBreakNode = fullNodeList[lastPathBreak];
                        shortenedNodeList.Add(lastPathBreakNode);
                        currentLineStart = lastPathBreakNode.EntryPoint;
                    }
                }
            }
            return shortenedNodeList;
        }
                
        void GetShortestNodeListUsingReferenceNodesWithLineOptimising(List<ASNode> fullNodeList, List<ASNode> refNodeList, Vector3 startPoint, Vector3 endPoint)
        {
            
            //where was the direct Path to the destination broken last
            int lastPathBreak = 0;
           // int lastrefPathBreak = 0;
           // ASNode lastPathBreakNode = null;
            
            //make it go from start to finnish
            for (int originalListIndex = 0; originalListIndex < (fullNodeList.Count); originalListIndex++)
            {
                //check if each step to this node can be skipped

                
                ASNode currentBaseNode = fullNodeList[originalListIndex];
                ASNode prevBaseNode = null;
                ASNode nextBaseNode = null;
                if (originalListIndex < (fullNodeList.Count - 1))
                {
                    nextBaseNode = fullNodeList[originalListIndex + 1];
                }
                if (originalListIndex > 0 && fullNodeList.Count > 1)
                {
                    prevBaseNode = fullNodeList[originalListIndex - 1];
                }
                
                bool directPath = true;
                Vector3 currentLineStart = startPoint;
                Vector3 currentLineEnd = currentBaseNode.EntryPoint;

                int startRefIndex = 0;
                int currentRefIndex = refNodeList.Count;
                int endRefIndex = refNodeList.Count;
                //work out the positions in the reference list
                for (int i = 0; i < refNodeList.Count; i++)
                {
                    if (refNodeList[i] == prevBaseNode)
                    {
                        startRefIndex = i;
                        
                    }
                    if (refNodeList[i] == currentBaseNode)
                    {
                        currentRefIndex = i;
                        
                    }
                    if (refNodeList[i] == nextBaseNode)
                    {
                        endRefIndex = i;
                        break;
                    }

                }
                Vector3 testStartPoint = startPoint;
                Vector3 testEndPoint = endPoint;

                if (prevBaseNode != null)
                {
                    testStartPoint = prevBaseNode.EntryPoint;
                }
                if (nextBaseNode != null)
                {
                    testEndPoint = nextBaseNode.EntryPoint;
                }
                currentLineStart = testStartPoint;//startPoint;

                currentBaseNode.LinePassesThroughLinkedLine(testStartPoint, testEndPoint, ref currentLineEnd);
                
                //for each node to the mid point
                for (int originalList2ndIndex = startRefIndex + 1; originalList2ndIndex < currentRefIndex && directPath == true; originalList2ndIndex++)
                {
                    ASNode currentCompareNode = refNodeList[originalList2ndIndex];
                    if (currentCompareNode == currentBaseNode)
                    {
                        break;
                    }
                    //does this line intersect the link line
                    bool linePassesThrough = currentCompareNode.LinePassesThroughLinkedLine(currentLineStart, currentLineEnd,true);

                    //if not it must be broken in the last link
                    if (linePassesThrough == false)
                    {
                        directPath = false;
                        lastPathBreak = originalListIndex - 1;

                    }
                }
                currentLineStart = currentLineEnd;
                currentLineEnd = testEndPoint;
                //for each node from the mid point
                for (int originalList2ndIndex = currentRefIndex + 1; (originalList2ndIndex < endRefIndex) && directPath == true; originalList2ndIndex++)
                {
                    ASNode currentCompareNode = refNodeList[originalList2ndIndex];
                    if (currentCompareNode == currentBaseNode)
                    {
                        break;
                    }
                    //does this line intersect the link line
                    bool linePassesThrough = currentCompareNode.LinePassesThroughLinkedLine(currentLineStart, currentLineEnd,true);

                    //if not it must be broken in the last link
                    if (linePassesThrough == false)
                    {
                        directPath = false;
                        lastPathBreak = originalListIndex - 1;
                    }
                }
                if (directPath)
                {
                    currentBaseNode.EntryPoint = currentLineStart;
                }
            }
            

            
        }
    
    }
}
