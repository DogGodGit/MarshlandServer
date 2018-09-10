using System.Collections.Generic;
using XnaGeometry;

namespace MainServer
{
     /// <summary>
    /// Stores the position a character was at a specified time
    /// Used for detemining a characters path
    /// </summary>
    class CharacterPathMarker
    {
        Vector3 m_position;
        double m_timestamp = 0;

        internal Vector3 Position
        {
            get { return m_position; }
        }
        internal double TimeStamp
        {
            get { return m_timestamp; }
        }
        internal CharacterPathMarker(Vector3 position, double timeStamp)
        {
            m_position = position;
            m_timestamp = timeStamp;
        }
    }
    /// <summary>
    /// holds the recent position data for a character
    /// </summary>
    class CharacterPath
    {

        static double CHARACTER_ROLLBACK_TIME = 1.0f;
        //temp public for drawing
        internal List<CharacterPathMarker> m_pastPath = new List<CharacterPathMarker>();

        internal CharacterPath()
        {

        }

        /// <summary>
        /// if the position list is nolonger relevent due to the character having teleported/spawned
        /// </summary>
        internal void ClearList()
        {
            m_pastPath.Clear();
        }

        // clear down entries once there is another that is old enough to be used
        internal void Update(double currentTime)
        {
            //what time should the client think it is
            double rollbackTime = currentTime - CHARACTER_ROLLBACK_TIME;
            //is your past path is large enough to need cut down
            if(m_pastPath.Count>1)
            {
                //has it reached a record that is to recent
                bool foundYoungEntry = false;
                //protect against an infinate loop
                int totalItterations = 0;
                //clear down all positions up to the latest one that is 2 seconds old or older
                for (int i = 1; i < m_pastPath.Count && foundYoungEntry == false && totalItterations<20; i++)
                {
                    totalItterations++;
                    CharacterPathMarker currentMarker = m_pastPath[i];
                    if (currentMarker != null)
                    {
                        if (currentMarker.TimeStamp > rollbackTime)
                        {
                            foundYoungEntry = true;
                        }
                        else
                        {
                            m_pastPath.RemoveAt(0);
                            i=0;
                        }
                    }
                    else
                    {
                        Program.Display("Error in CharacterPath::Update currentMarker is null");

                        m_pastPath.RemoveAt(0);
                        i = 0;
                    }
                }
            }

        }

        internal Vector3 GetRollbackPositionFromCurrentTime(double currentTime, Vector3 currentPosition)
        {
            //what time should the client think it is
            double rollbackTime = currentTime - CHARACTER_ROLLBACK_TIME;

            //if it has a marker to use as the start
            if(m_pastPath.Count > 0)
            {
                CharacterPathMarker startMarker = m_pastPath[0];
                Vector3 startPoint = startMarker.Position;
                double startTime = startMarker.TimeStamp;
                Vector3 endPoint = currentPosition;
                double endTime = currentTime;
                //does it have a second point
                if (m_pastPath.Count > 1)
                {
                    //if so use the second point for the line
                    CharacterPathMarker endMarker = m_pastPath[1];
                    if (endMarker != null)
                    {
                        endPoint = endMarker.Position;
                        endTime = endMarker.TimeStamp;
                    }
                }
                //the time for this segment 
                double totalTime = endTime - startTime;
                double timeOLeftInLine = totalTime - (rollbackTime - startTime);
                double t = 0;
                if (totalTime > 0)
                {
                    t = 1-(timeOLeftInLine / totalTime);
                    if (t > 1)
                    {
                        t = 1;
                    }
                    if(t < 0)
                    {
                        t = 0;
                    }
                }
                //what point should it be at now
                Vector3 currentPointOnLine = startPoint + (endPoint - startPoint)*(float)t;

                return currentPointOnLine;
            }


            //if all else fails return the currentPos
            return currentPosition;
        }
        internal void AddPosition(Vector3 position, double timeStamp)
        {
            CharacterPathMarker newMarker = new CharacterPathMarker(position, timeStamp);
            m_pastPath.Add(newMarker);
        }

    }
}
