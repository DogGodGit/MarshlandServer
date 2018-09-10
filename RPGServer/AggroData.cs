using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{

    class AggroData
    {
        CombatEntity m_linkedCharacter = null;
        static double AGGRO_TIME_TO_LIVE = 300;

        float m_aggroRating=0;
        float m_assistAggroRating = 0;
        int m_totalDamage=0;
        DateTime m_timeSinceLastChecked;

        Queue<AggroDamage> m_aggroQueue = new Queue<AggroDamage>();


        internal CombatEntity LinkedCharacter
        {
            get { return m_linkedCharacter; }
        }

        internal float AggroRating
        {
           // set { m_aggroRating = value; }
            get { return m_aggroRating; }
        }
        internal float AssistAggroRating
        {
           // set { m_assistAggroRating = value; }
            get { return m_assistAggroRating; }
        }
        internal int TotalDamage
        {
            set { m_totalDamage = value; }
            get { return m_totalDamage; }
        }

        float SecondsSinceCheck
        {
            get
            {
                float seconds = 0;
                DateTime currentTime = DateTime.Now;
                TimeSpan timeSinceUpdate = currentTime - m_timeSinceLastChecked;

                seconds = (float)timeSinceUpdate.TotalSeconds;

                return seconds;

            }
        }
        
        internal void UpdateData()
        {
           // if (m_aggroRating <= 0||m_linkedCharacter==null)
            if ( m_linkedCharacter == null)
            {
                return;
            }
            float timeSinceUpdate = SecondsSinceCheck;
            double timeAggrosToLive = AGGRO_TIME_TO_LIVE;

            m_timeSinceLastChecked = DateTime.Now;
            double currentTime =Program.MainUpdateLoopStartTime();
            double earliestTimeToKeep =currentTime - timeAggrosToLive*m_linkedCharacter.AggroTickReduction;
            bool allAggrosRemoved = false;
            //remove a max of 100 to prevent an infinate loop
            int i = 0;

            while (allAggrosRemoved == false && m_aggroQueue.Count > 0 && i<100)
            //while ((allAggrosRemoved == true || m_aggroQueue.Count <= 0) && i < 100)
            {
                //check the oldest aggro data
                AggroDamage oldestAggro = m_aggroQueue.Peek(); 
                //if the time for it to be removed has passed
                if (oldestAggro.TimeAdded < earliestTimeToKeep)
                {
                    //remove the aggro from the overall value
                    m_aggroRating -= oldestAggro.AggroValue;
                    //remove it from the queue
                    m_aggroQueue.Dequeue();
                }
                else{
                    allAggrosRemoved = true;
                }
                i++;

            }

            //m_aggroRating = m_aggroRating - timeSinceUpdate * m_linkedCharacter.AggroTickReduction;
            //m_assistAggroRating = m_assistAggroRating - timeSinceUpdate * m_linkedCharacter.AggroTickReduction;
            //-ve aggro is accepted as calm needs to work
            /*if (m_aggroRating < 0)
            {
                m_aggroRating = 0;
            }*/
            if (m_assistAggroRating < 0)
            {
                m_assistAggroRating = 0;
            }
        }
        internal AggroData GetShallowCopy()
        {
            return (AggroData)this.MemberwiseClone();
        }
        internal void ReduceAggro(float remainingAmount)
        {
            AggroDamage[] aggroArray = new AggroDamage[m_aggroQueue.Count];
            m_aggroQueue.CopyTo(aggroArray,0);
            float totalAggro=0;
            for (int i = 0; i < aggroArray.Length; i++)
            {
                AggroDamage currentAggro = aggroArray[i];

                if (currentAggro != null)
                {
                    currentAggro.AggroValue = currentAggro.AggroValue * remainingAmount;

                    totalAggro += currentAggro.AggroValue;
                }

            }
            m_aggroRating = totalAggro;
        }
        internal void AddToAggro(float aggroAmount, double currentTime)
        {
            AggroDamage newAggroDamage = new AggroDamage(aggroAmount, currentTime);

            m_aggroQueue.Enqueue(newAggroDamage);
            m_aggroRating += aggroAmount;
        }
        public AggroData(CombatEntity linkedCharacter)
        {
            m_linkedCharacter = linkedCharacter;
            m_timeSinceLastChecked = DateTime.Now;
        }
        internal void ClearAggro()
        {
            m_aggroRating = 0;
            m_assistAggroRating = 0;
            m_aggroQueue.Clear();
        }
    }
    class AggroDamage
    {
        double m_timeAdded=0;
        float m_aggroValue = 0;
        internal double TimeAdded
        {
            get { return m_timeAdded; }
        }
        internal float AggroValue
        {
            get { return m_aggroValue; }
            set { m_aggroValue = value; }
        }
        public AggroDamage(float aggroValue, double timeAdded)
        {
            m_aggroValue = aggroValue;
            m_timeAdded = timeAdded;
        }
    }
}
