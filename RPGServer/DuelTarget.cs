using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaGeometry;
using MainServer.Localise;

namespace MainServer
{
    class PVP_RatingLookup
    {
        public PVP_RatingLookup(int rating_id, double max_value)
        {
            m_max_value = max_value;
        }
        public double m_max_value;
    };
    static class PVP_RatingLookupManager
    {
        public static List<PVP_RatingLookup> ratings = new List<PVP_RatingLookup>();
        static PVP_RatingLookupManager()
        { }
        public static void loadLookups(Database db)
        {
            SqlQuery query=new SqlQuery(db,"select * from pvp_ratings order by pvp_Rating_id");
            while(query.Read())
            {
                PVP_RatingLookup newLookup=new PVP_RatingLookup(query.GetInt32("pvp_rating_id"),query.GetDouble("pvp_rating_max_value"));
                ratings.Add(newLookup);
            }
            query.Close();
        }
        public static int getRatingID(double rating_value)
        {
            for (int i = 0; i < ratings.Count; i++)
            {
                if (ratings[i].m_max_value >= rating_value)
                {
                    return i;
                }
            }
            return 0;
        }
    };
    class DuelTarget
    {
		// #localisation
		public class DuelTargetTextDB : TextEnumDB
		{
			public DuelTargetTextDB() : base(nameof(DuelTarget), typeof(TextID)) { }

			public enum TextID
			{
				ABOUT_TO_LEAVE_DUEL_AREA,       //"You are about to leave the duel area, if you leave the area then you will lose the duel"
				DUEL_DRAW,                      //"The duel ended in a draw"
				OTHER_WON_DUEL,                 //"{name0} won the duel"
				OTHER_FLED_SCENE,               //"{name0} fled the scene!"
				DUEL_TIME_OUT,                  //"Duel timed out"
				OPPONENT_LEFT_AREA,             //"The opponent has left the area"
				OPPONENT_OUT_OF_RANGE,          //"The opponent is out of range"
				OTHER_DEFEATED_IN_DUEL,         //"{owningCharacterName0} has defeated {duelCharacterName1} in a duel! {zoneDuelInfo2}"
				DUEL_END_STALEMATE,             //"The duel between {duelCharacterName0} and {owningCharacterName1} ended in a stalemate.{zoneDuelInfo2}"
				OTHER_DEFEATED_IN_DUEL_FLED,    //"{duelCharacterName0} has defeated {owningCharacterName1} in a duel! {owningCharacterName1} fled the scene!"
			}
		}
		public static DuelTargetTextDB textDB = new DuelTargetTextDB();

		internal enum DUEL_END_CONDITIONS
        {
            DEC_NONE = 0,
            DEC_DRAW = 1,
            DEC_VICTORY = 2,
            DEC_DEFEAT = 3,
            DEC_TIME_OUT = 4,
            DEC_RANGE_OUT = 5,
            DEC_UNKNOWN = 6

        };

        static int MAX_DUEL_DISTANCE = 25;
        internal static float MAX_TIME_FOR_DUEL = 300;
        internal static float COUNT_IN_TIME = 10;

        Character m_duelCharacter = null;
        double m_duelStartTime = 0;
        double m_maxDuelTime = 0;
        bool m_duelStarted = false;
        bool m_duelEnded = false;
        Vector3 m_duelStartPos = new Vector3(0);
        Zone m_duelStartZone = null;
        bool m_rangeWarningSent = false;
        bool m_itemsLocked = true;

        internal bool ItemsLocked
        {
            get { return m_itemsLocked; }
        }

        internal bool IsInProgress
        {
            get { return (m_duelEnded == false && m_duelStarted==true); }
        }

        internal Character DuelCharacter
        {
            get { return m_duelCharacter; }
        }
        internal Vector3 DuelStartPos
        {
            get { return m_duelStartPos; }
        }

        internal DuelTarget(Character target, double duelStartTime, double duelEndTime, Character owner)
        {
            m_duelCharacter = target;
            m_duelStartTime = duelStartTime;
            m_maxDuelTime = duelEndTime;

            m_duelStartZone = owner.CurrentZone;
            Vector3 startPosition = (m_duelCharacter.CurrentPosition.m_position + owner.CurrentPosition.m_position) / 2;

            m_duelStartPos = startPosition;
        }

        internal void Update(double currentTime, Character owningCharacter)
        {
            float warningPercent = 0.75f;
            if (m_duelStarted == false)
            {
                if (Utilities.Difference2D(owningCharacter.CurrentPosition.m_position, m_duelStartPos) > MAX_DUEL_DISTANCE * warningPercent)
                {
                    if (owningCharacter.m_player != null  && m_rangeWarningSent == false)
                    {
						string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.ABOUT_TO_LEAVE_DUEL_AREA);
						Program.processor.sendSystemMessage(locText, owningCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.PVP);
						m_rangeWarningSent = true;
                    }

                }
                if (m_duelCharacter.Dead == true||owningCharacter.Dead == true)
                {
                    EndDuel(owningCharacter);
                    m_duelCharacter.CurrentDuelTarget.EndDuel(m_duelCharacter);
                    owningCharacter.m_zone.SendDuelEnd(owningCharacter.m_player, m_duelCharacter.m_character_id, DUEL_END_CONDITIONS.DEC_NONE, "", m_duelCharacter);

                    m_duelCharacter.m_zone.SendDuelEnd(m_duelCharacter.m_player, owningCharacter.m_character_id, DUEL_END_CONDITIONS.DEC_NONE, "", owningCharacter);
                    return;
                }
                if (currentTime >= m_duelStartTime)
                {
                    
                    //add each to the hate list
                    BeginDuel(owningCharacter);
                    m_duelCharacter.CurrentDuelTarget.BeginDuel(m_duelCharacter);

                }
            }
            else if (m_duelEnded == false)
            {
                DUEL_END_CONDITIONS endCondition = DUEL_END_CONDITIONS.DEC_NONE;
                bool duelShouldEnd = false;
                string endConditionString = "";
                string zoneDuelInfo = "";
                DUEL_END_CONDITIONS othersEndCondition = DUEL_END_CONDITIONS.DEC_NONE;
                //did one side forfeit
                //if so the reward will need to be done sepparatly
                bool forfeit = false;
                //check it's still valid
                //is either dead Yet
                if (owningCharacter.Dead == true && m_duelCharacter.Dead == true)
                {
                    //draw 
                    duelShouldEnd = true;
					endConditionString = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.DUEL_DRAW);
                    endCondition = DUEL_END_CONDITIONS.DEC_DRAW;
                }
                else if (owningCharacter.Dead == true)
                {
                    //the opponent Won
                    duelShouldEnd = true;
					string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_WON_DUEL);
					locText = string.Format(locText, m_duelCharacter.Name);
					endConditionString = locText;
                    endCondition = DUEL_END_CONDITIONS.DEC_DEFEAT;
                }
                else if (m_duelCharacter.Dead == true)
                {
                    //this one won
                    duelShouldEnd = true;
					string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_WON_DUEL);
					locText = string.Format(locText, owningCharacter.Name);
					endConditionString = locText;
                    endCondition = DUEL_END_CONDITIONS.DEC_VICTORY;
                }
                    //if they have logged or lagged out 
                else if (m_duelCharacter.Destroyed == true)
                {
                    duelShouldEnd = true;
					string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_WON_DUEL);
					locText = string.Format(locText, owningCharacter.Name);
					endConditionString = locText;

					locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_FLED_SCENE);
					locText = string.Format(locText, m_duelCharacter.Name);
					zoneDuelInfo = locText;
                    forfeit = true;
                    endCondition = DUEL_END_CONDITIONS.DEC_VICTORY;
                }
                //has one left the zone or is now disconnected

                //has it timed out
                if (currentTime >= m_maxDuelTime)
                {
                    duelShouldEnd = true;
					endConditionString = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.DUEL_TIME_OUT);
                    endCondition = DUEL_END_CONDITIONS.DEC_TIME_OUT;
                }
                //if they are somehow now in different zones
                if (owningCharacter.CurrentZone != m_duelStartZone)
                {
                    duelShouldEnd = true;
                    forfeit = true;
					endConditionString = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OPPONENT_LEFT_AREA);
					string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_FLED_SCENE);
					locText = string.Format(locText, owningCharacter.Name);
					zoneDuelInfo = locText;
                    endCondition = DUEL_END_CONDITIONS.DEC_DEFEAT;//.DEC_RANGE_OUT;
                }
                else if (m_duelCharacter.CurrentZone!= m_duelStartZone)
                {
                    duelShouldEnd = true;
                    forfeit = true;
					endConditionString = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OPPONENT_LEFT_AREA);
					string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_FLED_SCENE);
					locText = string.Format(locText, m_duelCharacter.Name);
					zoneDuelInfo = locText;
                    endCondition = DUEL_END_CONDITIONS.DEC_VICTORY;//.DEC_RANGE_OUT;
                }
                else if(Utilities.Difference2D(owningCharacter.CurrentPosition.m_position, m_duelStartPos)>MAX_DUEL_DISTANCE)
                {
                    duelShouldEnd = true;
                    forfeit = true;
					endConditionString = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OPPONENT_OUT_OF_RANGE);
					string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_FLED_SCENE);
					locText = string.Format(locText, owningCharacter.Name);
					zoneDuelInfo = locText;
                    endCondition = DUEL_END_CONDITIONS.DEC_DEFEAT;//.DEC_RANGE_OUT;
                }
                    else if(Utilities.Difference2D(owningCharacter.CurrentPosition.m_position, m_duelStartPos)>MAX_DUEL_DISTANCE)
                {
                    duelShouldEnd = true;
                    forfeit = true;
					endConditionString = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OPPONENT_OUT_OF_RANGE);
					string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_FLED_SCENE);
					locText = string.Format(locText, owningCharacter.Name);
					zoneDuelInfo = locText;
                    endCondition = DUEL_END_CONDITIONS.DEC_DEFEAT;//.DEC_RANGE_OUT;
                }
                else if (Utilities.Difference2D(m_duelCharacter.CurrentPosition.m_position, m_duelStartPos) > MAX_DUEL_DISTANCE)
                {
                    duelShouldEnd = true;
                    forfeit = true;
					endConditionString = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OPPONENT_OUT_OF_RANGE);
					string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_FLED_SCENE);
					locText = string.Format(locText, m_duelCharacter.Name);
					zoneDuelInfo = locText;
                    endCondition = DUEL_END_CONDITIONS.DEC_VICTORY;//.DEC_RANGE_OUT;
                }

                if (Utilities.Difference2D(owningCharacter.CurrentPosition.m_position, m_duelStartPos) > MAX_DUEL_DISTANCE * warningPercent)
                {
                    if (owningCharacter.m_player!=null&&duelShouldEnd == false && m_rangeWarningSent == false)
                    {
						string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.ABOUT_TO_LEAVE_DUEL_AREA);
						Program.processor.sendSystemMessage(locText, owningCharacter.m_player, false, SYSTEM_MESSAGE_TYPE.PVP);
                        m_rangeWarningSent = true;
                    }
                    
                }
                //if not cancel or put an end to the duel
                if (duelShouldEnd == true)
                {
                    string zoneDuelResult = "";
                    EndDuel(owningCharacter);
                    m_duelCharacter.CurrentDuelTarget.EndDuel(m_duelCharacter);
                    owningCharacter.m_zone.SendDuelEnd(owningCharacter.m_player, m_duelCharacter.m_character_id, endCondition, endConditionString, m_duelCharacter);
                   /* DUEL_END_CONDITIONS*/ othersEndCondition = endCondition;
                    if (endCondition == DUEL_END_CONDITIONS.DEC_VICTORY)
                    {
						string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_DEFEATED_IN_DUEL);
						locText = string.Format(locText, owningCharacter.Name, m_duelCharacter.Name, zoneDuelInfo);
						zoneDuelResult = locText;
                        othersEndCondition = DUEL_END_CONDITIONS.DEC_DEFEAT;
                        //time to punnish the forfeit
                        if (forfeit == true)
                        {
                            m_duelCharacter.ForfeitDuel();
                        }
                    }
                    else if(endCondition == DUEL_END_CONDITIONS.DEC_DEFEAT)
                    {
						string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_DEFEATED_IN_DUEL);
						locText = string.Format(locText, m_duelCharacter.Name, owningCharacter.Name, zoneDuelInfo);
						zoneDuelResult = locText;
                        othersEndCondition = DUEL_END_CONDITIONS.DEC_VICTORY;
                        //time to punnish the forfeit
                        if (forfeit == true)
                        {
                            owningCharacter.ForfeitDuel();
                        }
                    }
                    else if (endCondition == DUEL_END_CONDITIONS.DEC_DRAW)
                    {
						string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.DUEL_END_STALEMATE);
						locText = string.Format(locText, m_duelCharacter.Name, owningCharacter.Name, zoneDuelInfo);
						zoneDuelResult = locText;
                    }
                    m_duelCharacter.m_zone.SendDuelEnd(m_duelCharacter.m_player, owningCharacter.m_character_id, othersEndCondition, endConditionString,owningCharacter);

                    if (zoneDuelResult.Length > 0)
                    {
                        m_duelStartZone.SendZoneSystemMessage("^o"+zoneDuelResult+"^0", false, SYSTEM_MESSAGE_TYPE.PVP);
                    }
                }

                //if the duel has come to its end assign any awards then sever the link
                if (m_duelEnded)
                {
                    owningCharacter.CurrentDuelTarget = null;
                    m_duelCharacter.CurrentDuelTarget = null;
                }
            }
            else
            {
                //the link should have already been severed, this is an error
                //report then attempt to sever the link
                Program.Display("error in DuelTarget Update, Complete Duel entered Update loop ");
                owningCharacter.CurrentDuelTarget = null;
                m_duelCharacter.CurrentDuelTarget = null;
            }
        }

        internal void BeginDuel(Character owningCharacter)
        {
            m_duelStarted = true;
            owningCharacter.PVPTypeChanged();
            owningCharacter.AddToHateList(m_duelCharacter);
            owningCharacter.ConductedHotileAction();

            if (Program.m_LogAnalytics)
            {
                AnalyticsMain logAnalytics = new AnalyticsMain(false);
                logAnalytics.LogPVPStarted(owningCharacter.m_player, m_duelCharacter);
            }
            //owningCharacter.InCombat = true;
     
        }
        internal void EndDuel(Character owningCharacter)
        {
            m_duelEnded = true;
            
            if(m_duelStarted)
            {
                owningCharacter.PVPTypeChanged();
                owningCharacter.RemoveFromHateList(m_duelCharacter);
                if (Program.m_LogAnalytics)
                {
                    AnalyticsMain logAnalytics = new AnalyticsMain(false);
                    logAnalytics.LogPVPEnded(owningCharacter.m_player);
                }
            }
           


        }
        /// <summary>
        /// ends the duel for both players
        /// </summary>
        /// <param name="owningCharacter"></param>
        /// <param name="infoString">why was the duel ended (eg. dissconnect)</param>
        internal void ForceEndDuel(Character owningCharacter, string infoString)
        {
            bool ownerLost = Program.processor.InShutDown == false;
            EndDuel(owningCharacter);
            if (m_duelCharacter.CurrentDuelTarget != null)
            {
                m_duelCharacter.CurrentDuelTarget.EndDuel(m_duelCharacter);
            }
            if (ownerLost)
            {
                owningCharacter.ForfeitDuel();
            }
            owningCharacter.CurrentDuelTarget = null;
            m_duelCharacter.CurrentDuelTarget = null;


            
            if (ownerLost)
            {
                if (m_duelStarted == true)
                {
                    string zoneDuelResult = "";
                    DUEL_END_CONDITIONS endCondition = DUEL_END_CONDITIONS.DEC_DEFEAT;
                    owningCharacter.m_zone.SendDuelEnd(owningCharacter.m_player, m_duelCharacter.m_character_id, endCondition, "", m_duelCharacter);
                    /* DUEL_END_CONDITIONS*/
                    DUEL_END_CONDITIONS othersEndCondition = endCondition;
                    if (endCondition == DUEL_END_CONDITIONS.DEC_DEFEAT)
                    {
						string locText = Localiser.GetString(textDB, owningCharacter.m_player, (int)DuelTargetTextDB.TextID.OTHER_DEFEATED_IN_DUEL_FLED);
						locText = string.Format(locText, m_duelCharacter.Name, owningCharacter.Name);
						zoneDuelResult = locText;
                        othersEndCondition = DUEL_END_CONDITIONS.DEC_VICTORY;
                    }

                    m_duelCharacter.m_zone.SendDuelEnd(m_duelCharacter.m_player, owningCharacter.m_character_id, othersEndCondition, "", owningCharacter);

                    if (zoneDuelResult.Length > 0)
                    {
                        m_duelStartZone.SendZoneSystemMessage("^o" + zoneDuelResult + "^0", false, SYSTEM_MESSAGE_TYPE.PVP);
                    }
                }
                else
                {
                    DUEL_END_CONDITIONS endCondition = DUEL_END_CONDITIONS.DEC_UNKNOWN;
                    DUEL_END_CONDITIONS othersEndCondition = DUEL_END_CONDITIONS.DEC_UNKNOWN;
                    owningCharacter.m_zone.SendDuelEnd(owningCharacter.m_player, m_duelCharacter.m_character_id, endCondition, "", m_duelCharacter);
                    m_duelCharacter.m_zone.SendDuelEnd(m_duelCharacter.m_player, owningCharacter.m_character_id, othersEndCondition, "", owningCharacter);
                }
            }
           
        }


    }
}
