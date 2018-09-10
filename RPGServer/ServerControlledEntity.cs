
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Diagnostics;
using MainServer.Combat;
using XnaGeometry;
using MainServer.Localise;


namespace MainServer
{
	class RandomPatrolSettings
	{
		float m_radius = 20;
		int m_probabilityWait = 0;
		float m_minWaitTime = 0;
		float m_maxWaitTime = 1;


		public float Radius
		{
			set { m_radius = value; }
			get { return m_radius; }
		}
		public int ProbabilityWait
		{
			set { m_probabilityWait = value; }
			get { return m_probabilityWait; }
		}
		public float MinWaitTime
		{
			set { m_minWaitTime = value; }
			get { return m_minWaitTime; }
		}
		public float MaxWaitTime
		{
			set { m_maxWaitTime = value; }
			get { return m_maxWaitTime; }
		}


	}
	class PatrolPoint
	{
		Vector3 m_position;
		double m_stopTime = 0;
        
		public Vector3 Position
		{
			set { m_position = value; }
			get { return m_position; }
		}
		public double StopTime
		{
			set { m_stopTime = value; }
			get { return m_stopTime; }
		}


		public PatrolPoint() { }

		~PatrolPoint() { }
	}
	class HiddenEntities
	{
		CombatEntity m_theEntity = null;
		double m_timeStamp = 0;

		internal HiddenEntities(CombatEntity theEntity, double timeStamp)
		{
			m_theEntity = theEntity;
			m_timeStamp = timeStamp;
		}
		static internal bool EntityIsInList(List<HiddenEntities> theList, CombatEntity theEntity)
		{
			for (int i = 0; i < theList.Count; i++)
			{
				if (theList[i].m_theEntity == theEntity)
				{
					return true;
				}
			}
			return false;
		}
		static internal void ClearOldData(List<HiddenEntities> theList, double oldestTime)
		{
			bool hiddenUpToDate = false;
			try
			{
				for (int i = 0; i < theList.Count && hiddenUpToDate == false; i++)
				{
					//should the current damage be applied
					HiddenEntities currentDamage = theList[i];
					if (currentDamage == null)
					{
						theList.RemoveAt(i);
						i--;
						continue;
					}
					else if (currentDamage.m_timeStamp < oldestTime)
					{

						theList.RemoveAt(i);
						i--;
						continue;
					}
					else
					{
						hiddenUpToDate = true;
					}
				}
			}
			catch (Exception e)
			{
				Program.Display("exception in HiddenEntities::ClearOldData loop : " + e.Message + ": " + e.StackTrace);
			}
		}
	}

	class ServerControlledEntity : CombatEntity, IEntitySocialStanding
	{
		// #localisation
		public class ServerControlledEntityTextDB : TextEnumDB
		{
			public ServerControlledEntityTextDB() : base(nameof(ServerControlledEntity), typeof(TextID)) { }

			public enum TextID
			{
				OTHER_SEE_THROUGH_TRICKERY,         //"{name0} is able to see though your trickery."
			}
		}
		public static new ServerControlledEntityTextDB textDB = new ServerControlledEntityTextDB();

		#region enums
		public enum NPC_MOVEMENT_AI
		{
			NPC_MOVEMENT_AI_STAND,
			NPC_MOVEMENT_AI_ROAM,
			NPC_MOVEMENT_AI_PATROL,
			NPC_MOVEMENT_AI_ATTACKING,
			NPC_MOVEMENT_AI_RETURNING
		};

		public enum SCE_AI_STATE
		{
			IDLE,
			RETURNING,
			IN_COMBAT
		}
		#endregion

		#region constants
		const int SQUARED_POSITION_SEND_DIST = 4225;

		/// <summary>
		/// the time in seconds before the aggro is automatically checked
		/// </summary>
		internal const double AGGRO_RECHECK_TIME = 10;
		/// <summary>
		/// the min time in seconds that the aggro will be rechecked after aggro has been changed
		/// </summary>
		internal const double AGGRO_MODIFIED_RECHECK_TIME = 0.75;
		const float AGGRO_SWITCH_VALUE = 0.1f;

		const float AGGRO_PERCENT_LOST_PER_LEVEL = 1.25f;
		const float TIME_TILL_DESPAWN = 20;
		const float SKILL_COOLDOWN_TIME = 2;
		const float RESEND_POS_TO_ZONE_DIST_SQR = 2500;

		const bool USE_CHASE_BACKTRACKING = true;
		/// <summary>
		/// How much % aggro is ignored per meter 
		/// </summary>
		const float BASE_AGGRO_DROM_P_M = 0.01f;
		/// <summary>
		/// At what fraction of speed does the the amount aggro is reduced no longer change
		/// </summary>
		const float MIN_SPEED_AGGRO_REDUCED = 0.1f;
		/// <summary>
		/// what is the min aggro remaining for distant targets
		/// </summary>
		const float MIN_REMAIN_AGGRO = 0.05f;

		/// <summary>
		/// Time to wait before looking for players in range
		/// </summary>
		static double TIME_BETWEEN_IDLE_AGGRO_CHECKS = 0.25f;
		static double TIME_ENTITY_REMAINS_HIDDEN = 10.0;

		//    static int MAX_OWNER_LVLS_ABOVE = 50;
		//    static int MAX_OWNER_LVLS_BELOW = 40;

		/// <summary>
		/// number of levels above the mob to start decreasing the aggro done
		/// </summary>
		internal static int AGGRO_DEGRADE_START_LVL_ABOVE = 20;
		/// <summary>
		/// number of levels above the mob to stop decreasing the aggro done (aggro that will remain = MIN_AGGRO_ADDED)
		/// </summary>
		internal static int AGGRO_DEGRADE_END_LVL_ABOVE = 50;
		/// <summary>
		/// number of levels below the mob to start increasing the aggro done
		/// </summary>
		internal static int AGGRO_INCREASE_START_LVL_BELOW = 10;
		/// <summary>
		/// Amount to increase aggro done per level below AGGRO_INCREASE_START_LVL_BELOW
		/// </summary> 
		internal static float AGGRO_INCREASE_INCREMENT_PER_LEVEL = 0.2f;
		/// <summary>
		/// Minimum amount of aggro to remain if the player's lvl is above the mob's lvl
		/// </summary>
		internal static float MIN_AGGRO_ADDED = 0.1f;

		/// <summary>
		/// what is the min % of a base stat that must remain dispite status 
		/// </summary>
		internal static float MOB_MIN_STAT_REMAINS = 0.1f;

		//the levels above the player after which a mob cannot be locked 
		internal static float LOCK_UPPER_LVL_LIMIT = 50;
		//the levels above the player after which a mob requires more damage to lock
		internal static float LOCK_UPPER_DEGRADE_START = 20;
		//the levels below the player after which a mob cannot be locked 
		internal static float LOCK_LOWER_LVL_LIMIT = 40;
		//the levels below the player after which a mob requires more damage to lock
		internal static float LOCK_LOWER_DEGRADE_START = 20;

		static internal float MIN_HEALTH_FOR_LOCK = 0.1f;
		#endregion
		
		#region stl functions
		

		private static int CompareAggroDataByDamage(AggroData first, AggroData second)
		{

			if (first == null)
			{
				if (second == null)
				{
					return 0;
				}

				return -1;

			}
			if (second == null)
			{
				return 1;
			}

			if (first.TotalDamage > second.TotalDamage)
			{
				return -1;
			}
			else if (first.TotalDamage < second.TotalDamage)
			{
				return 1;
			}

			return 0;
		}
		private static int CompareAggroDataByAggro(AggroData first, AggroData second)
		{

			if (first == null)
			{
				if (second == null)
				{
					return 0;
				}

				return -1;

			}
			if (second == null)
			{
				return 1;
			}

			if (first.AggroRating > second.AggroRating)
			{
				return -1;
			}
			else if (first.AggroRating < second.AggroRating)
			{
				return 1;
			}

			return 0;
		}
		#endregion
		
		#region variables
		float m_percentHealthForLock = MIN_HEALTH_FOR_LOCK;
		MobSkillTable m_skillTable = null;
		ASPathingEntity m_pathingObject = new ASPathingEntity();
		public SCE_AI_STATE m_aiState = SCE_AI_STATE.IDLE;
		public bool m_waiting = false;
		public bool m_JustDied = false;
		double m_remainingWaitTime = 0;
		CombatManager m_combatManager = null;
		public CombatAI m_combatAI = null;
		Zone m_zone = null;


		public Vector3 m_spawnPosition;
		internal Vector3 m_spawnDirection;
		ActorPosition m_currentDestination;
		ActorPosition m_currentPathDestination = new ActorPosition();
		Vector3 m_failedAttackTargetPosition = new Vector3(-999999);
		bool m_hasFailedAttackTargetPosition = false;
		Vector3 m_failedAttackPosition = new Vector3(-999999);
		Vector3 m_lastZoneUpdatePos = new Vector3(-999999);
		float m_failureMinRetryDistance = 5;
		ActorPosition m_chaseStart;
		NPC_MOVEMENT_AI m_movementAI;
		NPC_MOVEMENT_AI m_defaultMovementAI;
		MobTemplate m_template = null;

		List<AggroData> m_aggroList = new List<AggroData>();
		/// <summary>
		/// A list of enemies that have recently successfully hidden from the mob
		/// </summary>
		List<HiddenEntities> m_hiddenEntities = new List<HiddenEntities>();
		/// <summary>
		/// A List of entities that have recently been discovered while hidden
		/// </summary>
		List<HiddenEntities> m_discoveredEntities = new List<HiddenEntities>();
		double m_currentSpeed = 0;
		double m_patrolSpeed = 1;
		RandomPatrolSettings m_roamingSettings = null;
		int m_currentWaypoint;
		double m_deleteTimer = TIME_TILL_DESPAWN;
	    public int m_spawnPointID;
	    public bool m_willDespawn;
        //double m_despawnTimer


		bool m_DestinationUpdate = false;
		bool m_justStopped = false;

		List<PatrolPoint> m_patrolPoints;
		public List<Character> m_nearbyPlayers = new List<Character>();
		public List<Character> m_PlayersToUpdate = new List<Character>();

		int m_currentHitpoints = 0;
		double m_lastUpdateSent = 0;
		/// <summary>
		/// The time at which the mob can search for a player target within range
		/// </summary>
		double m_timeForNextLookForTarget = 0;

		//prevents the mob moving immediatly after skill use or an attack
		double m_timeTillCanMove = 0;

		List<CombatDamageMessageData> m_recentDamages = new List<CombatDamageMessageData>();
		float m_backtrackDist = 0;
		#endregion
		
		#region Properties
		internal CombatAI MobCombatAI
		{
			get { return m_combatAI; }
		}
		internal ActorPosition ChaseStart
		{
			get { return m_chaseStart; }
		}
		override internal Zone CurrentZone
		{
			get
			{
				return m_zone;
			}
		}
		internal bool JustStopped
		{
			set { m_justStopped = value; }
			get { return m_justStopped; }
		}
		public double PatrolSpeed
		{
			set { m_patrolSpeed = value; }
		}
		public NPC_MOVEMENT_AI MovementAI
		{
			get { return m_movementAI; }
		}

		public MobTemplate Template
		{
			get { return m_template; }
		}

		/*public String Name
		{
			get { return m_template.m_name; }
		}*/
		public int FactionID
		{
			get { return m_template.m_factionID; }
		}
		public int OpinionBase
		{
			get { return m_template.m_opinionBase; }
		}
		public float AggroRange
		{
			get { return m_template.m_aggroRange; }
		}
		public float FollowRange
		{
			get { return m_template.m_followRange; }
		}
		internal List<AggroData> AggroList
		{
			get { return m_aggroList; }
		}
		internal override CombatManager TheCombatManager
		{
			get
			{
				return m_combatManager;
			}
		}
		internal override string Name
		{
			get
			{
				if (m_template == null)
				{
					return "";
				}
				return m_template.m_name;
			}
		}
		internal MobSkillTable SkillTable
		{
			get { return m_skillTable; }
		}
		internal List<CombatDamageMessageData> RecentDamages
		{
			get { return m_recentDamages; }
		}
		internal float BacktrackDist
		{
			get { return m_backtrackDist; }
		}
		internal float MaxBacktrackDist
		{
			get { return m_radius * 3; }
		}
        #endregion
        
		#region Initialisation
		public ServerControlledEntity(MobTemplate template, Vector3 position, int serverID, Zone zone,int spawnPointID,bool willDespawn)
		{
			//set our gathering type first, we'll need it in later setup for setting damage reduction
			if (template.m_mobType == 1)
			{
				Type = EntityType.Mob;
				Gathering = LevelType.fish;
			}
			else
			{
				Type = EntityType.Mob;
			}

			CurrentPosition = new ActorPosition();

			m_currentDestination = new ActorPosition();
			m_template = template;
			SetUp(serverID);
			CurrentPosition.m_position = position;
			m_currentDestination.m_position = position;
			m_currentPathDestination.m_position = position;
			m_lastZoneUpdatePos = position;
			Level = template.m_level;
			m_currentWaypoint = 0;
			m_spawnPosition = position;
			m_zone = zone;
			CompiledStats.MaxAttackRange = template.m_maxAttackRange;
			BlocksAttacks = template.m_blocksAttacks;
			m_radius = template.m_radius;
		    m_spawnPointID = spawnPointID;
			m_combatManager = zone.m_combatManager;
		    m_willDespawn = willDespawn;

			


			// Program.Display(Name + "'s max energy = "+MaxEnergy + " and current energy = "+CurrentEnergy);
			//BaseMaxEnergy = 150;
			//CurrentEnergy = 150;

			m_skillTable = new MobSkillTable(template.m_availableSkills, template.m_skillSets);
			m_combatAI = new CombatAI(this);
			m_reportTime = template.m_reportTime;
			m_projectileSpeed = template.m_projectileSpeed;
			//CompileStats();
			m_timeForNextLookForTarget = Program.MainUpdateLoopStartTime() + TIME_BETWEEN_IDLE_AGGRO_CHECKS;
			m_defaultInterestTypes = partitioning.ZonePartition.ENTITY_TYPE.ET_PLAYER;


		}
		~ServerControlledEntity()
		{

		}

		void SetUp(int serverID)
		{
			ServerID = serverID;
			m_movementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_STAND;
			m_defaultMovementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_STAND;

			m_pathingObject.Path = new List<Vector3>();
			m_chaseStart = new ActorPosition();

			CurrentPosition.m_position = new Vector3(0, 0, 0);
			m_currentDestination.m_position = new Vector3(0, 0, 0);

			CurrentPosition.m_direction = new Vector3(1, 0, 0);
			m_currentDestination.m_direction = new Vector3(1, 0, 0);
			m_currentHitpoints = m_template.m_maxHitpoints;

			SetUpCombatEnt();

		}
		#endregion

		/// <summary>
		/// Updates the server controlled entity based on it's current state
		/// </summary>
		/// <param name="timeSinceLastUpdate"></param>
		public void Update(double timeSinceLastUpdate)
		{
			//UpdateCombat(timeSinceLastUpdate);
			//check if the mob is dead and prepare to delete it if it is
			UpdateDeath(timeSinceLastUpdate);
			double currentTime = Program.MainUpdateLoopStartTime();
			if (m_hiddenEntities.Count > 0)
			{
				HiddenEntities.ClearOldData(m_hiddenEntities, currentTime - TIME_ENTITY_REMAINS_HIDDEN);
			}
			if (m_discoveredEntities.Count > 0)
			{
				HiddenEntities.ClearOldData(m_discoveredEntities, currentTime - TIME_ENTITY_REMAINS_HIDDEN);
			}


			//if it's dead don't move
			if (Dead)
			{
				base.Update(CurrentPosition);
				if (m_hiddenEntities.Count > 0)
				{
					m_hiddenEntities.Clear();
				}
				if (m_discoveredEntities.Count > 0)
				{
					m_discoveredEntities.Clear();
				}
				return;
			}
			//call the update for the correct state

			switch (m_aiState)
			{
				case SCE_AI_STATE.IDLE:
					StateUpdate_Idle(timeSinceLastUpdate);
					break;
				case SCE_AI_STATE.RETURNING:
					StateUpdate_Returning(timeSinceLastUpdate);
					break;
				case SCE_AI_STATE.IN_COMBAT:
					StateUpdate_Attack(timeSinceLastUpdate);
					break;
				default:
					m_aiState = SCE_AI_STATE.IDLE;
					break;

			}

			//do the basic update
			base.Update(CurrentPosition);

			//if the entity is not in combat then clear recent damages
			if (m_aiState != SCE_AI_STATE.IN_COMBAT)
			{
				m_recentDamages.Clear();
			}

			//UpdateMovement(timeSinceLastUpdate);
			//check the position to Send zone Update

			if (m_zone != null && Utilities.Difference2DSquared(m_lastZoneUpdatePos, CurrentPosition.m_position) > RESEND_POS_TO_ZONE_DIST_SQR)
			{
				//add self to the list that need updates sent
				m_zone.AddMobToSendList(this);
				//update the last send position
				m_lastZoneUpdatePos = CurrentPosition.m_position;
			}

		}
		internal void CheckOwner()
		{

			if (m_lockOwner != null)
			{
				//List<Character> lockOwners = m_lockOwner.GetCharacters;
				bool maintainsOwnership = IsValidOwner(m_lockOwner);//false;
				/* for (int i = 0; i < lockOwners.Count&&maintainsOwnership==false; i++)
				 {
					 Character currentOwner = lockOwners[i];
					 if (currentOwner != null)
					 {
						 if (currentOwner.Dead == false && 
							 (Utilities.Difference2DSquared(currentOwner.CurrentPosition.m_position, ChaseStart.m_position) < (FollowRange * FollowRange))&&
							 EntityHasAggro(currentOwner)==true&&
							 m_level<(currentOwner.m_level+MAX_OWNER_LVLS_ABOVE)
							 )
						 {
							 maintainsOwnership = true;
						 }
					 }
				 }*/
				if (maintainsOwnership == false)
				{
					ITargetOwner oldOwner = LockOwner;
					LockOwner.ResignOwnership(this);
					ResetDamageByTargetOwner(oldOwner);
					List<AggroData> highestAggro = GetHighestDamagingAggroList(oldOwner);
					Character newOwner = GetOwnerFromHighestAggro(highestAggro);// GetHighestDamagingAggroList(oldOwner);
					//check it is not the old owner
					if (newOwner != null && newOwner != oldOwner && newOwner.CharacterParty != oldOwner)
					{
						if (newOwner.CharacterParty != null)
						{
							newOwner.CharacterParty.TakeOwnership(this);
						}
						else
						{
							newOwner.TakeOwnership(this);
						}
						newOwner.NotifyOwnershipTaken(this);
					}
				}

			}
		}
		internal override bool ConductingHostileAction()
		{

			if (m_combatAI != null)
			{
				if (m_combatAI.MainTarget != null)
				{
					return true;
				}
			}
			return base.ConductingHostileAction();
		}

        #region combatAI

        internal string GetCombatDebugString()
		{
			
			string debugString = "";


			//combat variables

			//skills

			//current
			if (CurrentSkill != null)
			{
				if (CurrentSkillTarget != null)
				{
					debugString += "Has Current Skill " + CurrentSkill.Template.SkillName + ". Targetting " + CurrentSkillTarget.Name + ".";
				}
				else
				{
					debugString += "Has Current Skill " + CurrentSkill.Template.SkillName + ". Targetting Null Target.";
				}
			}
			else
			{
				if (CurrentSkillTarget != null)
				{
					debugString += "Has Current Skill Null. Targetting " + CurrentSkillTarget.Name + ".";
				}
				else
				{
					debugString += "Has No Current Skill.";
				}
			}
			//next
			if (NextSkill != null)
			{
				if (NextSkillTarget != null)
				{
					debugString += "Has Next Skill " + NextSkill.Template.SkillName + ". Targetting " + NextSkillTarget.Name + ".";
				}
				else
				{
					debugString += "Has Next Skill " + NextSkill.Template.SkillName + ". Targetting Null Target.";
				}
			}
			else
			{
				if (NextSkillTarget != null)
				{
					debugString += "Has Next Skill Null. Targetting " + NextSkillTarget.Name + ".";
				}
				else
				{
					debugString += "Has No Next Skill.";
				}
			}
			//attack target
			if (AttackTarget != null)
			{
				debugString += " Attacking " + AttackTarget.Name + ".";
			}
			else
			{
				debugString += "Not Attacking.";
			}
			//action complete
			debugString += " Current action complete = " + m_actionInProgress + ".";
			//time till next action
			debugString += " Time action complete = " + m_timeActionWillComplete + ".";
			//path variables
			if (m_pathingObject.Path != null)
			{
				debugString += " Path exists with count " + m_pathingObject.Path.Count + ".";
			}
			else
			{
				debugString += " Path is null.";
			}

			return debugString;
		}
		
		/// <summary>
		/// Called if there has been a change to the aggro to require it to be checked sooner than expected
		/// </summary>
		void AggroNeedsRechecked(double currentTime)
		{
			if (m_combatAI != null)
			{
				m_combatAI.AggroNeedsRechecked(currentTime, this);
			}
		}

		AggroData GetAggroForEntity(CombatEntity theTarget)
		{
			AggroData theAggro = null;
			for (int i = 0; i < m_aggroList.Count && theAggro == null; i++)
			{
				AggroData currentAggro = m_aggroList[i];
				if (currentAggro != null && currentAggro.LinkedCharacter == theTarget)
				{
					theAggro = currentAggro;
				}
			}



			return theAggro;

		}
		/// <summary>
		/// Returns the Combat Entity with the highest aggro rating
		/// entities in different zones are removed
		/// Entities within attack range are taken into account over ones out of range if the mob cannot move
		///
		/// </summary>
		/// <param name="thePosition">The current position the mob will return to</param>
		/// <returns></returns>
		CombatEntity GetHighestAggro(ActorPosition thePosition)
		{
            // A fish won't do anything but swim in a brook
            // He can't write his name or read a book
            // And to fool the people is his only thought
            // Though he slippery - he still gets caught
            // But then if that sort of life is what you wish
            // You may grow up to be a fish
            // (As requested by Richard Clifford)
            if (Gathering == LevelType.fish && m_aggroList.Count > 1)
            {
                return AttackTarget;
            }

			bool canMove = true;
			bool canReachCurrentAggro = true;
			bool canReachHighestAggro = true;
			if (StatusPreventsActions.Move == true)
			{
				canMove = false;
			}
			if (AggroList.Count == 0)
				return null;
			/*
			for (int i = 0; i < m_currentStatusEffects.Count(); i++)
			{
				if (m_currentStatusEffects[i].Template.StatusEffectID == EFFECT_ID.GRASPING_ROOTS || (m_currentStatusEffects[i].Template.StatusEffectID == EFFECT_ID.MAX_ROOT))
				{
					canMove = false;
				}
			}*/
			AggroData highestAggro = null;
			float scaledHighestAggroRating = 0;
			AggroData highestHelperAggro = null;
			AggroData currentTargetAggro = null;
			float scaledCurrentAggroRating = 0;
			//
			float baseDistanceAggroDegrade = BASE_AGGRO_DROM_P_M;
			float currentSpeedMod = GetSpeedModMultiplier();
			//clamp
			if (currentSpeedMod < MIN_SPEED_AGGRO_REDUCED) { currentSpeedMod = MIN_SPEED_AGGRO_REDUCED; }
			float speedAggroModifier = 1 / currentSpeedMod;
			float distanceAggroDegrade = baseDistanceAggroDegrade * speedAggroModifier;
			string baseAggroString = "base aggro values currentSpeedMod = " + currentSpeedMod + " speedAggroModifier = " + speedAggroModifier + " distanceAggroDegrade = " + distanceAggroDegrade;
			for (int i = 0; i < m_aggroList.Count; i++)
			{
				AggroData currentAggro = m_aggroList[i];

				//remove any invalid aggro's
				if (currentAggro == null || currentAggro.LinkedCharacter == null)
				{
					m_aggroList.Remove(currentAggro);
					i--;
					continue;
				}

				//remove any aggro's for characters in different Zones 
				//or who are dead  
				//or destroyed
				//or are not an enemy
				CombatManager aggrosCombatManager = currentAggro.LinkedCharacter.TheCombatManager;
				if ((aggrosCombatManager == null) || (m_combatManager != aggrosCombatManager) || currentAggro.LinkedCharacter.Destroyed == true || IsEnemyOf(currentAggro.LinkedCharacter) == false)
				{
					m_aggroList.Remove(currentAggro);
					i--;
					continue;
				}

				//they are not valid if they are dead
				//or out of range
				if ((IsWithinChaseRange(currentAggro.LinkedCharacter, thePosition) == false) || (currentAggro.LinkedCharacter.Dead == true) || (OtherEntityCannotBeTargettedBecauseOFStatusEffect(currentAggro.LinkedCharacter) == true))
				{
					if (currentAggro.LinkedCharacter.Dead == true)
					{
						//forget how much you hate this person
						currentAggro.ClearAggro();
					}
					continue;
				}

				//let the data start to count Down
				currentAggro.UpdateData();

				//remember the current target
				if (currentAggro.LinkedCharacter == AttackTarget)
				{
					currentTargetAggro = currentAggro;
				}
				bool canReachCurrent = true;
				//find the distance to this target
				float distanceToTarget = CombatEntity.GetDistanceBetweenEntities(this, currentAggro.LinkedCharacter);
				if (canMove == false)
				{
					//float attackRange = Radius + MaxAttackRange + currentAggro.LinkedCharacter.Radius;
					float attackRange = CompiledStats.MaxAttackRange;
					//float distanceToTarget = Utilities.Difference2D(CurrentPosition.m_position, currentAggro.LinkedCharacter.CurrentPosition.m_position);

					canReachCurrent = (distanceToTarget <= attackRange);
				}
				//don't include the attack range
				distanceToTarget -= CompiledStats.MaxAttackRange;
				if (distanceToTarget < 0)
				{
					distanceToTarget = 0;
				}
				//scale the aggro based on distance
				float aggroDropped = distanceToTarget * distanceAggroDegrade;
				//how much aggro should be dropped off
				float aggroToRemain = 1 - aggroDropped;
				//clamp final rate
				if (aggroToRemain < MIN_REMAIN_AGGRO)
				{
					aggroToRemain = MIN_REMAIN_AGGRO;
				}
				//calculate currentAggro
				float currentScaledAggro = currentAggro.AggroRating * aggroToRemain;
				if (Program.m_Aggro_debugging)
				{
					string aggroData = currentAggro.LinkedCharacter.ServerID + currentAggro.LinkedCharacter.Name + " aggro evaluated\n original aggro = " + currentAggro.AggroRating + " new aggro = " + currentScaledAggro;
					aggroData += "\n distance = " + distanceToTarget + "| aggro dropped = " + aggroDropped + "| aggroToRemain = " + aggroToRemain;
					aggroData += "\n" + baseAggroString;
					Program.Display(aggroData);
				}
				//if there is is no highest aggro
				if ((highestAggro == null))
				{
					//have they harmed the mob
					if (currentAggro.AggroRating > 0)
					{
						highestAggro = currentAggro;
						scaledHighestAggroRating = currentScaledAggro;
					}
					//check if they are assisting the enemy
					else
					{
						if (highestHelperAggro == null)
						{
							highestHelperAggro = currentAggro;
						}
						else if (currentAggro.AssistAggroRating > highestHelperAggro.AssistAggroRating)
						{
							highestHelperAggro = currentAggro;
						}
					}
					canReachHighestAggro = canReachCurrent;
				}
				else if ((highestAggro != null) &&
					((currentScaledAggro > scaledHighestAggroRating) ||
					((canReachCurrent == true) && (canReachHighestAggro == false))))
				{
					if ((canReachCurrent == true) || (canReachHighestAggro == false))
					{
						scaledHighestAggroRating = currentScaledAggro;
						highestAggro = currentAggro;
						canReachHighestAggro = canReachCurrent;
					}
				}

			}

			//check

			//if no attacker was found go for the healer
			if (highestAggro == null)
			{
				highestAggro = highestHelperAggro;
			}
			if (highestAggro == null)
			{
				return null;
			}

			if ((highestAggro != null) && (currentTargetAggro != null))
			{
				//float distanceToTarget = Utilities.Difference2D(CurrentPosition.m_position, currentTargetAggro.LinkedCharacter.CurrentPosition.m_position);
				float distanceToTarget = CombatEntity.GetDistanceBetweenEntities(this, currentTargetAggro.LinkedCharacter);
				float aggrodistanceToTarget = distanceToTarget;
				//don't include the attack range
				aggrodistanceToTarget -= CompiledStats.MaxAttackRange;
				if (aggrodistanceToTarget < 0)
				{
					aggrodistanceToTarget = 0;
				}
				//scale the aggro based on distance
				float aggroDropped = aggrodistanceToTarget * distanceAggroDegrade;
				//how much aggro should be dropped off
				float aggroToRemain = 1 - aggroDropped;
				//clamp final rate
				if (aggroToRemain < MIN_REMAIN_AGGRO)
				{
					aggroToRemain = MIN_REMAIN_AGGRO;
				}
				//calculate currentAggro
				scaledCurrentAggroRating = currentTargetAggro.AggroRating * aggroToRemain;

				if (Program.m_Aggro_debugging)
				{
					string aggroData = "Current Target Aggro" + currentTargetAggro.LinkedCharacter.ServerID + " " + currentTargetAggro.LinkedCharacter.Name + " aggro evaluated\noriginal aggro = " + currentTargetAggro.AggroRating + " new aggro = " + scaledCurrentAggroRating;
					aggroData += "\ndistance = " + distanceToTarget + "| aggro dropped = " + aggroDropped + "| aggroToRemain = " + aggroToRemain;
					aggroData += "\n" + baseAggroString;
					Program.Display(aggroData);
				}
				int aggroToBeat = (int)(scaledCurrentAggroRating + 0.1 * (1.0f + AGGRO_SWITCH_VALUE));
				if (canMove == false)
				{
					//float attackRange = Radius + MaxAttackRange + currentTargetAggro.LinkedCharacter.Radius;
					float attackRange = CompiledStats.MaxAttackRange;

					canReachCurrentAggro = (distanceToTarget <= attackRange);

				}
				if (scaledHighestAggroRating <= aggroToBeat && aggroToBeat > 0)
				{
					if ((canReachCurrentAggro == true) || (canReachHighestAggro == false))
					{
						highestAggro = currentTargetAggro;
						if (Program.m_Aggro_debugging)
						{
							Program.Display("Current attack target retained");
						}
					}
				}
			}
			return highestAggro.LinkedCharacter;
		}
		internal bool EntityHasAggro(CombatEntity theEntity)
		{
			bool entityFound = false;
			for (int i = 0; i < m_aggroList.Count && entityFound == false; i++)
			{
				AggroData currentAggro = m_aggroList[i];
				if (currentAggro.LinkedCharacter == theEntity)
				{
					entityFound = true;
				}
			}
			return entityFound;
		}
		internal CombatEntity GetNthHighestAggro(int n, ActorPosition thePosition, float maxRangeSQR)
		{
			//this will ensure the aggro is up to date
			GetHighestAggro(thePosition);
			//sort the list by the raw aggro value
			m_aggroList.Sort(CompareAggroDataByAggro);
			//make a list of all applicable
			List<AggroData> applicableAggro = new List<AggroData>();
			//set to true once all aggro above 0 has been added
			bool noMoreAggro = false;
			for (int currentAggroIndex = 0; currentAggroIndex < m_aggroList.Count && noMoreAggro == false; currentAggroIndex++)
			{
				AggroData currentAggro = m_aggroList[currentAggroIndex];
				if (currentAggro != null && currentAggro.AggroRating > 0)
				{
					//check the distance 
					if (currentAggro.LinkedCharacter != null && Utilities.Difference2DSquared(currentAggro.LinkedCharacter.CurrentPosition.m_position, thePosition.m_position) < maxRangeSQR)
					{
						applicableAggro.Add(currentAggro);
					}
				}
				else
				{
					noMoreAggro = true;
				}
			}
			int totalAvailableAggro = applicableAggro.Count;
			if (totalAvailableAggro > 0)
			{
				int valueToUse = n % totalAvailableAggro;

				return m_aggroList[valueToUse].LinkedCharacter;
			}

			return null;			
		}

        internal CombatEntity GetNthRandomAggro(int n, ActorPosition thePosition, float maxRangeSQR)
        {
            //this will ensure the aggro is up to date
            GetHighestAggro(thePosition);
            //sort the list by the raw aggro value
            m_aggroList.Sort(CompareAggroDataByAggro);
            //make a list of all applicable
            List<AggroData> applicableAggro = new List<AggroData>();
            //set to true once all aggro above 0 has been added
            bool noMoreAggro = false;
            for (int currentAggroIndex = 0; currentAggroIndex < m_aggroList.Count && noMoreAggro == false; currentAggroIndex++)
            {
                AggroData currentAggro = m_aggroList[currentAggroIndex];
                if (currentAggro != null && currentAggro.AggroRating > 0)
                {
                    //check the distance 
                    if (currentAggro.LinkedCharacter != null && Utilities.Difference2DSquared(currentAggro.LinkedCharacter.CurrentPosition.m_position, thePosition.m_position) < maxRangeSQR)
                    {
                        applicableAggro.Add(currentAggro);
                    }
                }
                else
                {
                    noMoreAggro = true;
                }
            }
            int totalAvailableAggro = applicableAggro.Count;
            if (totalAvailableAggro > 0)
            {
                int valueToUse = Program.m_rand.Next(n % totalAvailableAggro);
                return m_aggroList[valueToUse].LinkedCharacter;
            }

            return null;
        }

        internal void AggroReduction(float remainingAmount)
		{

			AggroData[] aggroArray = new AggroData[m_aggroList.Count];
			m_aggroList.CopyTo(aggroArray);
			for (int i = 0; i < m_aggroList.Count; i++)
			{
				AggroData currentAggro = m_aggroList[i];

				//remove any invalid aggro's
				if (currentAggro == null || currentAggro.LinkedCharacter == null)
				{
					m_aggroList.Remove(currentAggro);
					i--;
					continue;
				}

				currentAggro.ReduceAggro(remainingAmount);

			}

			AggroNeedsRechecked(Program.MainUpdateLoopStartTime());
		}


		
		/// <summary>
		/// removes any agro from the list that is linked to the entered Entity
		/// also removes null links
		/// </summary>
		/// <param name="entityToRemove"></param>
		internal void RemoveFromAggroLists(CombatEntity entityToRemove)
		{
			for (int i = 0; i < m_aggroList.Count; i++)
			{
				AggroData currentAggro = m_aggroList[i];
				if (currentAggro == null || currentAggro.LinkedCharacter == null || currentAggro.LinkedCharacter == entityToRemove)
				{
					m_aggroList.Remove(currentAggro);
					i--;
					continue;
				}
			}
		}
		/// <summary>
		/// If the entity is on the aggro list add the value to the aggro data
		/// if not then discard it
		/// this does not take aggro modifier into account, calling function should apply this if required
		/// </summary>
		/// <param name="entityToAddAgro">The entity who's agro data needs changed</param>
		/// <param name="aggroToAdd">amount to change the Data By</param>
		internal override void AddToAggroValueToExistingData(CombatEntity entityToAddAgro, float aggroToAdd)
		{
			if (entityToAddAgro == null)
			{
				return;
			}
			if (aggroToAdd > 0)
			{
				float aggroMulti = GetAggroMultiForEntity(entityToAddAgro);
				aggroToAdd = aggroToAdd * aggroMulti;
			}
			for (int i = 0; i < m_aggroList.Count; i++)
			{
				AggroData currentAggro = m_aggroList[i];
				if (currentAggro != null && currentAggro.LinkedCharacter != null && currentAggro.LinkedCharacter == entityToAddAgro)
				{
					/*currentAggro.AggroRating += aggroToAdd;
					if (currentAggro.AggroRating < 0)
						currentAggro.AggroRating = 0;*/
					double currentTime = Program.MainUpdateLoopStartTime();

					currentAggro.AddToAggro(aggroToAdd, currentTime);
					AggroNeedsRechecked(currentTime);
				}
			}
		}
		float GetAggroMultiForEntity(CombatEntity entityToAddAgro)
		{
			float lvlDiff = entityToAddAgro.Level -  Level;
			float aggroMulti = 1;
			if (lvlDiff > AGGRO_DEGRADE_START_LVL_ABOVE)
			{
				if (lvlDiff > AGGRO_DEGRADE_END_LVL_ABOVE)
				{
					aggroMulti = MIN_AGGRO_ADDED;
				}
				else
				{
					float ratio = (lvlDiff - AGGRO_DEGRADE_START_LVL_ABOVE) / (AGGRO_DEGRADE_END_LVL_ABOVE - AGGRO_DEGRADE_START_LVL_ABOVE);
					aggroMulti = (1 - ratio) * (1 - MIN_AGGRO_ADDED);
					aggroMulti += MIN_AGGRO_ADDED;
				}
				if (aggroMulti > 1)
				{
					Program.Display("***********aggroMulti = " + aggroMulti + "****************");
				}

			}
			else if (-lvlDiff > AGGRO_INCREASE_START_LVL_BELOW)
			{
				int levelBelowIncStart = (-(int)lvlDiff) - AGGRO_INCREASE_START_LVL_BELOW;
				aggroMulti = 1 + levelBelowIncStart * AGGRO_INCREASE_INCREMENT_PER_LEVEL;
				// Program.Display("***********aggroMulti = " + aggroMulti + "****************");
			}
			return aggroMulti;
		}
		/// <summary>
		/// If the entity is on the aggro list add the value to the aggro data
		/// otherwise add a new data
		/// this does not take aggro modifier into account, calling function should apply this if required
		/// </summary>
		/// <param name="entityToAddAgro">The entity who's agro data needs changed</param>
		/// <param name="aggroToAdd">amount to change the Data By</param>
		internal override void AddToAggroValue(CombatEntity entityToAddAgro, float aggroToAdd)
		{
			if (entityToAddAgro == null)
			{
				return;
			}

			
			if (aggroToAdd > 0)
			{
				float aggroMulti = GetAggroMultiForEntity(entityToAddAgro);
				aggroToAdd = aggroToAdd * aggroMulti;
			}
			double currentTime = Program.MainUpdateLoopStartTime();
			for (int i = 0; i < m_aggroList.Count; i++)
			{
				AggroData currentAggro = m_aggroList[i];
				if (currentAggro != null && currentAggro.LinkedCharacter != null && currentAggro.LinkedCharacter == entityToAddAgro)
				{
					currentAggro.AddToAggro(aggroToAdd, currentTime);
					AggroNeedsRechecked(currentTime);
					return;
				}
			}
			AggroData theAggroData = new AggroData(entityToAddAgro);
			m_aggroList.Add(theAggroData);

			theAggroData.AddToAggro(aggroToAdd, currentTime);
			AggroNeedsRechecked(currentTime);


		}
		internal void RequestAssistance(ServerControlledEntity mobInNeed, CombatEntity target)
		{
			if (AttackTarget == null && AggroRange > 0)
			{
				if (target != null)
				{
                   
                    //where is the attacker
                    Vector3 characterPosition = target.CurrentPosition.m_position;
					//can you see the attacker
					Vector3 canReachPoint = m_combatManager.zone.CheckCollisions(CurrentPosition.m_position, characterPosition, 1, 2, false);
					//are they close to somewhere you have failed to reach
					double distFromLastFail = (m_failedAttackTargetPosition - characterPosition).Length();
					//can you reach them
					if ((canReachPoint - characterPosition).LengthSquared() < MAX_SQUARED_COLLISION_ERROR &&
						distFromLastFail > m_failureMinRetryDistance)
					{
						AddToAggroValue(target, 1);						
					}
				}

				if (mobInNeed.AggroList.Count > 0)
				{
					for (int i = 0; i < mobInNeed.AggroList.Count; i++)
					{
						AggroData currentAggro = mobInNeed.AggroList[i];
						if (currentAggro != null && currentAggro.AggroRating > 0 && currentAggro.LinkedCharacter != null)
						{
							AddToAggroValue(currentAggro.LinkedCharacter, 0);
						}
					}
				}
			}
		}

		internal bool CheckForFailedPathToEnt(CombatEntity target)
		{
			if (m_pathingObject.LastError == ASPathFinder.AS_PATHING_ERROR.NO_PATH && target != null)
			{
				m_failedAttackTargetPosition = target.CurrentPosition.m_position;
				m_hasFailedAttackTargetPosition = true;
				m_failedAttackPosition = CurrentPosition.m_position;
				return true;
			}
			return false;
		}
		internal CombatEntity GetValidTarget(bool isInCombat)
		{
			double currentTime = Program.MainUpdateLoopStartTime();
			CombatEntity validTarget = null;

			ActorPosition thePosition = CurrentPosition;
			if (isInCombat == true)
			{
				thePosition = m_chaseStart;
			}
			//if someone is causing aggro
			//attack them 1st
			CombatEntity newTarget = GetHighestAggro(thePosition);
			//only look for aggro range target when out of combat 
			if (newTarget == null && isInCombat == false)
			{
				if (currentTime > m_timeForNextLookForTarget)
				{
					//look for a new target
					CombatEntity NextAttackTarget = LookForPlayerWithinAggro();
					if (NextAttackTarget != null)
					{
						newTarget = NextAttackTarget;
					}
					m_timeForNextLookForTarget = currentTime + TIME_BETWEEN_IDLE_AGGRO_CHECKS;
				}
			}
			if (newTarget != null)
			{
				validTarget = newTarget;
			}



			return validTarget;
		}
		/// <summary>
		/// Start Attacking the designated target
		/// </summary>
		/// <param name="isInCombat">was the mob in combat prior to this</param>
		internal void AttackNewTarget(bool isInCombat, CombatEntity newTarget)
		{
			//if the mob was not in combat set it's return spot
			/*if (isInCombat == false)
			{
				m_chaseStart.m_position = CurrentPosition.m_position;
				m_chaseStart.m_yangle = CurrentPosition.m_yangle;
			}*/
			//do something with your new target
			m_combatManager.StartAttackingEntity(this, newTarget);
			//InCombat = true;
			m_movementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_ATTACKING;
			m_aiState = SCE_AI_STATE.IN_COMBAT;
		}

		CombatEntity LookForPlayerWithinAggro()
		{
			CombatEntity nextAttackTarget = null;
			if (AggroRange > 0)
			{

				Zone zone = m_combatManager.zone;
				float aggroSquared = AggroRange * AggroRange;
				double distanceToNextTarget = 0;
				

				
				//look for player who is too close
				//check all players
				float maxRange = AggroRange;
				List<Player> playerList = new List<Player>();
				CurrentZone.PartitionHolder.AddPlayersInRangeToList(this, CurrentPosition.m_position, maxRange, playerList, MainServer.partitioning.ZonePartition.ENTITY_TYPE.ET_ENEMY, null);

				for (int i = 0; i < playerList.Count; i++)
				{
					Character character = playerList[i].m_activeCharacter;
					Vector3 characterPosition = character.m_CharacterPosition.m_position;
					double currentCharacterDistance = Utilities.Difference2DSquared(CurrentPosition.m_position, characterPosition);
					double distFromLastFail = (m_failedAttackTargetPosition - characterPosition).Length();
					float currentAggroSquared = aggroSquared;
					float newRange = AggroRange;
					if (character.Dead == true)
					{
						continue;
					}
					if (character.Level > Level)
					{
						int diff = character.Level - Level;
						float percentAgroRange = 1 - (AGGRO_PERCENT_LOST_PER_LEVEL * diff / 100.0f);
						if (percentAgroRange < 0)
						{
							percentAgroRange = 0;
						}
						newRange = AggroRange * percentAgroRange;
						currentAggroSquared = newRange * newRange;
					}

                    //is the player an ally of my faction?
                    if (character.FactionManager.CheckReputationAgainstEntity(this.Template.FactionInfluences))
                    {
                        //if so set the aggro distance to 0f so only the player attacking will make me hostile
                        currentAggroSquared = 0f;
                    }

                    if (!character.m_InLimbo &&
						character.IsEnemyOf(this) == true &&
						(currentCharacterDistance < currentAggroSquared) &&
						(distFromLastFail > m_failureMinRetryDistance))
					{
						//check if they are visable
						Vector3 canReachPoint = m_combatManager.zone.CheckIntersections(CurrentPosition.m_position, characterPosition, true, character.Radius);
						
						//check if they are hidden by status effect
						if ((canReachPoint - characterPosition).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
						{
							//Program.Display("I can see the Player");
							bool hidden = false;
							//float percentRange = 1.0f;

							if (character.StatusPreventsActions.Detection == true)
							{
								hidden = OtherEntityIsHiddenFromAggroByStatusEffect(character, characterPosition);
							}
							//the mob found a hidden playe, do they have a chance to see through the hide
							if (hidden == true && m_template.m_spot_hidden > 0)
							{
								//try to spot them
								hidden = AttemptToSpotFromHidden(character);								
							}

							//if they are not hidden and closer than the current target
							if (!hidden)
							{
								if ((nextAttackTarget == null) || (currentCharacterDistance < distanceToNextTarget))
								{									
									SetChaseStart(CurrentPosition);									
									ConductedHotileAction();
									nextAttackTarget = character;
									distanceToNextTarget = currentCharacterDistance;
								}
							}
						}
						else
						{
							//Program.Display("I can't see the Player");
						}
					}
				}
			}

			return nextAttackTarget;
		}
		/// <summary>
		/// try to spot a hidden entity using the m_template.m_spot_hidden value
		/// </summary>
		/// <param name="theEntity"></param>
		/// <returns>true if the entity remains hidden</returns>
		internal override bool AttemptToSpotFromHidden(CombatEntity theEntity)
		{

			bool hidden = true;
			//have they already managed to hide (check list)
			bool alreadyTested = HiddenEntities.EntityIsInList(m_hiddenEntities, theEntity);
			if (HiddenEntities.EntityIsInList(m_discoveredEntities, theEntity))
			{
				alreadyTested = true;
				hidden = false;
			}

			//otherwise test if they manage to hide
			if (alreadyTested == false)
			{
				int maxAmount = 100000;
				int testAmount = (int)(m_template.m_spot_hidden * maxAmount);
				int result = Program.getRandomNumber(maxAmount);
				if (result > testAmount)
				{
					//if they stay hidden add to the list of those who you failed to spot
					HiddenEntities newEntity = new HiddenEntities(theEntity, Program.MainUpdateLoopStartTime());
					m_hiddenEntities.Add(newEntity);

				}
				else
				{
					//remember you have spotted them in the last 10s
					HiddenEntities newEntity = new HiddenEntities(theEntity, Program.MainUpdateLoopStartTime());
					m_discoveredEntities.Add(newEntity);
					if (theEntity.Type == EntityType.Player)
					{
						Character character = (Character)theEntity;
						if (character != null)
						{
							string locText = Localiser.GetString(textDB, character.m_player, (int)ServerControlledEntityTextDB.TextID.OTHER_SEE_THROUGH_TRICKERY);
							string locName = MobTemplateManager.GetLocaliseMobName(character.m_player, Template.m_templateID);
							locText = string.Format(locText, locName);
							Program.processor.sendSystemMessage(locText, character.m_player, false, SYSTEM_MESSAGE_TYPE.BATTLE);
						}
					}


					hidden = false;
				}
				//Program.Display(GetIDString() + " AttemptToSpotFromHidden on " + theEntity.GetIDString() + " result " + result + " compared to " + testAmount+ " of "+ maxAmount);
			}

			return hidden;
		}
		internal bool OtherEntityCannotBeTargettedBecauseOFStatusEffect(CombatEntity character)
		{
			if (character.StatusPreventsActions.Detection == true)
			{
				for (int j = 0; j < character.m_currentCharacterEffects.Count; j++)
				{
					CharacterEffect statusEffect = character.m_currentCharacterEffects[j];
					switch (statusEffect.StatusEffect.Template.EffectType)
					{
						case EFFECT_TYPE.PLAYING_DEAD:
							{
								if (Level <= statusEffect.StatusEffect.m_effectLevel.getUnModifiedAmount())
								{
									bool remainsHidden = AttemptToSpotFromHidden(character);
									if (remainsHidden)
									{
										return true;
									}
								}
								break;
							}
						default:
							{
								break;
							}
					}
				}
			}

			return false;
		}
		bool OtherEntityIsHiddenFromAggroByStatusEffect(CombatEntity character, Vector3 characterPosition)
		{
			float aggroSquared = AggroRange * AggroRange;
			double currentCharacterDistance = Utilities.Difference2DSquared(CurrentPosition.m_position, characterPosition);
			double distFromLastFail = (m_failedAttackTargetPosition - characterPosition).Length();
			float currentAggroSquared = aggroSquared;
			float newRange = AggroRange;
			bool hidden = false;


			for (int j = 0; j < character.m_currentCharacterEffects.Count; j++)
			{
				StatusEffect statusEffect = character.m_currentCharacterEffects[j].StatusEffect;
				switch (statusEffect.Template.EffectType)
				{
					case EFFECT_TYPE.HIDE:// EFFECT_ID.HIDE:
						{
							if (Level <= statusEffect.m_effectLevel.getUnModifiedAmount())
							{
								hidden = true;
							}
							break;
						}
					case EFFECT_TYPE.PLAYING_DEAD:
						{
							if (Level <= statusEffect.m_effectLevel.getUnModifiedAmount())
							{
								hidden = true;
							}
							break;
						}
					case EFFECT_TYPE.INVISIBILITY://EFFECT_ID.INVISIBILITY_POTION:
						{
							hidden = true;
							break;
						}
					//case EFFECT_ID.CAMOFLAGE_PERM:
					case EFFECT_TYPE.CAMOUFLAGE://EFFECT_ID.CAMOUFLAGE:
						{
							float percentRange = 1.0f;
							percentRange = percentRange - ((float)statusEffect.m_effectLevel.getUnModifiedAmount() / 100);
							/*if (m_level <= statusEffect.m_effectLevel.m_amount)
							{
                                                
							}*/
							if (percentRange < 0)
							{
								percentRange = 0;
							}
							float camoRange = newRange * percentRange;
							currentAggroSquared = camoRange * camoRange;
							if (currentCharacterDistance > currentAggroSquared)
							{
								hidden = true;
							}
							break;
						}
					case EFFECT_TYPE.CAMOUFLAGE_2://EFFECT_ID.CAMOFLAGE_2:
						{
							if (Level <= statusEffect.m_effectLevel.getUnModifiedAmount())
							{
								float percentRange = 1.0f;
								percentRange = percentRange * 0.3f;
								/*if (m_level <= statusEffect.m_effectLevel.m_amount)
								{
                                                
								}*/
								if (percentRange < 0)
								{
									percentRange = 0;
								}
								float camoRange = newRange * percentRange;
								currentAggroSquared = camoRange * camoRange;
								if (currentCharacterDistance > currentAggroSquared)
								{
									hidden = true;
								}
							}
							break;
						}
				}
			}

			return hidden;

		}

		CombatEntity GetMainTarget()
		{

			CombatEntity mainTarget = m_combatAI.GetMainTarget(this);//null;
			/*CombatEntity targetToCheck = AttackTarget;
			if (targetToCheck != null)
			{
				if ((targetToCheck.Destroyed==false) && (!targetToCheck.Dead) && (targetToCheck.TheCombatManager == m_combatManager) &&
					(Utilities.Difference2DSquared(targetToCheck.CurrentPosition.m_position, m_chaseStart.m_position) < FollowRange * FollowRange))
				{
					mainTarget = targetToCheck;
				}
			}*/

			return mainTarget;
		}
		#endregion //combatAI
		
		#region death
		/// <summary>
		/// Checks if the Entity has just Died
		/// </summary>
		/// <param name="timeSinceLastUpdate">The time since the last update cycle was called</param>
		void UpdateDeath(double timeSinceLastUpdate)
		{
			//if the mob has just died, do any special processing
			if (!m_Dead && m_compiledStats.m_currentHealth <= 0)
			{
				Character lastAttacker = GetKiller();
				m_zone.recordCombatFinishStats(lastAttacker, this, Zone.COMBAT_WINNER.PLAYER);
				m_JustDied = true;
				Died();

			}
			//if the mob dies count down to respawn
			if (Dead)
			{
				m_deleteTimer -= timeSinceLastUpdate;

			}
		}



	    /// <summary>
		/// Returns the character who either individually of with a party did the most damage to the mob
		/// </summary>
		/// <returns></returns>
		internal Character GetKiller()
		{
			Character killer = null;

			//put the individuals into order
			m_aggroList.Sort(CompareAggroDataByDamage);
			//add the aggro's into parties
			for (int i = 0; i < m_aggroList.Count; i++)
			{
				AggroData currentAggroData = m_aggroList[i];

				if (currentAggroData != null)
				{
					CombatEntity combatEnt = currentAggroData.LinkedCharacter;
					//Are they a character
					if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
					{
						Character currentCharacter = (Character)combatEnt;
						//do they have a party
						if (currentCharacter.CharacterParty != null)
						{
							//if so compile the entire parties damage
							for (int j = i + 1; j < m_aggroList.Count; j++)
							{
								AggroData aggroData = m_aggroList[j];
								if (aggroData != null)
								{
									combatEnt = aggroData.LinkedCharacter;
									if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
									{
										Character possiblePartyCharacter = (Character)combatEnt;
										if (currentCharacter.CharacterParty == possiblePartyCharacter.CharacterParty)
										{
											currentAggroData.TotalDamage += aggroData.TotalDamage;
											m_aggroList.Remove(aggroData);
											j--;
										}
									}
								}

							}
						}
					}
				}
			}

			m_aggroList.Sort(CompareAggroDataByDamage);

			if (m_aggroList.Count > 0)
			{
				//check every one on the aggro list untill the killer is found
				for (int i = 0; i < m_aggroList.Count && killer == null; i++)
				{
					AggroData currentAggroData = m_aggroList[i];

					if (currentAggroData != null)
					{
						//get the entity
						CombatEntity combatEnt = currentAggroData.LinkedCharacter;
						//Are they a character
						if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
						{
							//check they can be rewarded
							//otherwise go to the next group
							Character currentCharacter = (Character)combatEnt;
							bool isInRange = currentCharacter.PlayerOrPartyIsInRange(CurrentPosition.m_position, Zone.MAX_PARTY_EXP_SHARE_DISTANCE_SQR, TheCombatManager);
							if (isInRange == true)
							{
								killer = currentCharacter;
							}
						}
					}
				}
				/*AggroData currentAggroData = m_aggroList[0];

				if (currentAggroData != null)
				{
					CombatEntity combatEnt = currentAggroData.LinkedCharacter;
					//Are they a character
					if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
					{
						Character currentCharacter = (Character)combatEnt;
						killer = currentCharacter;
					}
				}*/
			}


			return killer;
		}
		/// <summary>
		/// Returns the character who either individually of with a party did the most damage to the mob
		/// </summary>
		/// <returns></returns>
		internal List<AggroData> GetHighestDamagingAggroList(ITargetOwner ownerToExcude)
		{
			// Character killer = null;

			List<AggroData> aggroListCopy = new List<AggroData>(m_aggroList);
			List<AggroData> aggroListDuplicates = new List<AggroData>();

			//put the individuals into order
			aggroListCopy.Sort(CompareAggroDataByDamage);
			//add the aggro's into parties
			for (int i = 0; i < aggroListCopy.Count; i++)
			{
				AggroData currentAggroData = aggroListCopy[i].GetShallowCopy();

				if (currentAggroData != null)
				{
					CombatEntity combatEnt = currentAggroData.LinkedCharacter;
					//Are they a character
					if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
					{
						Character currentCharacter = (Character)combatEnt;
						if (currentCharacter != ownerToExcude && currentCharacter.CharacterParty != ownerToExcude)
						{
							aggroListDuplicates.Add(currentAggroData);
						}
						//do they have a party
						if (currentCharacter.CharacterParty != null)
						{
							//if so compile the entire parties damage
							for (int j = i + 1; j < aggroListCopy.Count; j++)
							{
								AggroData aggroData = aggroListCopy[j];
								if (aggroData != null)
								{
									combatEnt = aggroData.LinkedCharacter;
									if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
									{
										Character possiblePartyCharacter = (Character)combatEnt;
										if (currentCharacter.CharacterParty == possiblePartyCharacter.CharacterParty)
										{
											currentAggroData.TotalDamage += aggroData.TotalDamage;
											aggroListCopy.Remove(aggroData);
											j--;
										}
									}
								}

							}
						}
					}
				}
			}

			aggroListDuplicates.Sort(CompareAggroDataByDamage);
			return aggroListDuplicates;
			/*if (aggroListDuplicates.Count > 0)
			{
				//check every one on the aggro list untill the killer is found
				for (int i = 0; i < aggroListDuplicates.Count && killer == null; i++)
				{
					AggroData currentAggroData = aggroListDuplicates[i];

					if (currentAggroData != null)
					{
						//get the entity
						CombatEntity combatEnt = currentAggroData.LinkedCharacter;
						//Are they a character
						if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
						{
							//check they can be rewarded
							//otherwise go to the next group
							Character currentCharacter = (Character)combatEnt;
							bool isInRange = currentCharacter.PlayerOrPartyIsInRange(CurrentPosition.m_position, Zone.MAX_PARTY_EXP_SHARE_DISTANCE_SQR, TheCombatManager);
							if (isInRange == true)
							{
								killer = currentCharacter;
							}
						}
					}
				}

			}


			return killer;*/
		}

		bool IsValidOwner(ITargetOwner targetOwner)
		{
			if (targetOwner == null)
			{
				return false;
			}

			List<Character> ownerCharacters = targetOwner.GetCharacters;
			bool validForOwnership = false;
			for (int i = 0; i < ownerCharacters.Count && validForOwnership == false; i++)
			{
				Character currentOwner = ownerCharacters[i];
				if (currentOwner != null)
				{
					if (currentOwner.Dead == false &&
						//if the owner within the chase start range or is the mob still idle so has not set chase start
						((Utilities.Difference2DSquared(currentOwner.CurrentPosition.m_position, ChaseStart.m_position) < (FollowRange * FollowRange)) || m_aiState == SCE_AI_STATE.IDLE) &&
						currentOwner.TheCombatManager == TheCombatManager &&
						EntityHasAggro(currentOwner) == true &&
						 currentOwner.GetRelevantLevel(this) < (Level+ LOCK_UPPER_LVL_LIMIT)
						&& (currentOwner.GetRelevantLevel(this) > (Level- LOCK_LOWER_LVL_LIMIT))
						)
					{
						validForOwnership = true;
					}
				}
			}
			return validForOwnership;
		}
		/// <summary>
		/// Returns a valid owner from the list of aggro and damage that has been done to the mob
		/// </summary>
		/// <param name="aggroListDuplicates">A list of aggro data sorted by damage</param>
		/// <returns></returns>
		internal Character GetOwnerFromHighestAggro(List<AggroData> aggroListDuplicates)
		{
			//the list is sorted by damage
			//after one fails the damage check there is no point going further as they will all fail it
			bool passesDamageChecks = true;
			Character killer = null;
			if (aggroListDuplicates.Count > 0)
			{
				//check every one on the aggro list untill the killer is found
				for (int i = 0; i < aggroListDuplicates.Count && killer == null && passesDamageChecks; i++)
				{
					AggroData currentAggroData = aggroListDuplicates[i];

					if (currentAggroData != null)
					{
						//get the entity
						CombatEntity combatEnt = currentAggroData.LinkedCharacter;
						//Are they a character
						if (combatEnt != null && combatEnt.Type == CombatEntity.EntityType.Player)
						{
							//check they can be rewarded
							//otherwise go to the next group
							Character currentCharacter = (Character)combatEnt;

							bool validOwner = false;

							if (currentCharacter.CharacterParty != null)
							{
								validOwner = IsValidOwner(currentCharacter.CharacterParty);
							}
							else
							{
								validOwner = IsValidOwner(currentCharacter);
							}
							bool doneEnoughDamage = DamageByTargetOwnerOver(m_percentHealthForLock, currentCharacter);
							if (doneEnoughDamage == false)
							{
								passesDamageChecks = false;
							}

							//bool isInRange = currentCharacter.PlayerOrPartyIsInRange(CurrentPosition.m_position, Zone.MAX_PARTY_EXP_SHARE_DISTANCE_SQR, TheCombatManager);
							//bool inLvlRange = m_level < (currentCharacter.m_level + MAX_OWNER_LVLS_ABOVE);
							if (validOwner == true && doneEnoughDamage == true)
							{
								killer = currentCharacter;
							}
						}
					}
				}

			}


			return killer;
		}

		public bool ToBeDestroyed()
		{
			if (Dead && m_deleteTimer < 0)
				return true;

			return false;
		}
		#endregion//death
		
		#region States

		#region Idle State
		void StateUpdate_Idle(double timeSinceLastUpdate)
		{
			UpdateIdle(timeSinceLastUpdate);
			if (CanMove() == true)
			{
				m_DestinationUpdate = MoveIdle(timeSinceLastUpdate);
			}
			CheckForTransitionsIdle();

		}
		void UpdateIdle(double timeSinceLastUpdate)
		{


		}
		bool MoveIdle(double timeSinceLastUpdate)
		{


			return MoveAlongSetRoute(timeSinceLastUpdate);//didMove;
		}
		void CheckForTransitionsIdle()
		{
			//is there anyone to target
			//if someone is causing aggro
			//attack them 1st
			CombatEntity newTarget = GetValidTarget(false);
			if (newTarget != null)
			{
				AddToAggroValue(newTarget, 0);
				m_movementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_ATTACKING;
				m_aiState = SCE_AI_STATE.IN_COMBAT;
				SetChaseStart(CurrentPosition);
				//AttackNewTarget(false, newTarget);
				//if (m_aggroList.Count > 0)
				//{
				//if you have started attacking and they are on your aggro List
				//m_combatManager.zone.RequestAssistance(this, newTarget);
				//stop where you were going
				/*SetDestination(m_currentPosition.m_position);
				m_justStopped = true;*/
				StopAtCurrentPosition();
				m_combatAI.BattleStarted(this);
				//}
			}
			else if (CurrentHealth < MaxHealth)
			{

				/*m_chaseStart.m_position = CurrentPosition.m_position;
				m_chaseStart.m_yangle = CurrentPosition.m_yangle;*/
				SetChaseStart(CurrentPosition);
				Return();
			}
			//check if their failedAttack pos can be reset
			if (m_hasFailedAttackTargetPosition)
			{
				if ((m_failedAttackPosition - CurrentPosition.m_position).Length() > m_failureMinRetryDistance)
				{
					m_failedAttackTargetPosition = new Vector3(-999999);
					m_hasFailedAttackTargetPosition = false;
				}
			}
		}
		#endregion //Idle State

		#region Returning State
		void StateUpdate_Returning(double timeSinceLastUpdate)
		{
			UpdateReturning(timeSinceLastUpdate);
			if (CanMove() == true)
			{
				m_DestinationUpdate = MoveReturning(timeSinceLastUpdate);
			}
		}
		void UpdateReturning(double timeSinceLastUpdate)
		{

		}
		bool MoveReturning(double timeSinceLastUpdate)
		{
			bool didMove = false;
			m_currentSpeed = MaxSpeed * 1;
			didMove = FollowPath(timeSinceLastUpdate);//= HandleReturning(timeSinceLastUpdate);
			if (m_pathingObject.Path == null && m_pathingObject.LastError == ASPathFinder.AS_PATHING_ERROR.NO_PATH)
			{
				List<Vector3> path = new List<Vector3>();
				path.Add(m_currentDestination.m_position);
				m_pathingObject.Path = path;
				m_currentPathDestination.m_position = m_currentPosition.m_position;//m_currentDestination.m_position;
				EntityPartitionCheck();
				//m_DestinationUpdate = true;
			}

			if ((m_pathingObject.Path != null) && (m_pathingObject.Path.Count == 0))
			{//if it is not at the chase start point them move them there

				if (Gathering == LevelType.none && ((m_chaseStart.m_position - CurrentPosition.m_position).LengthSquared() > 0.01))
				{
					CurrentPosition.m_yangle = m_chaseStart.m_yangle;

					CurrentPosition.m_position = m_chaseStart.m_position;
					CurrentPosition.m_direction = m_chaseStart.m_direction;

					Program.Display("(" + ServerID + ")" + Name + " was almost moved by boss movement glitch");
					StopAtCurrentPosition();
				}
				HandleReturning(timeSinceLastUpdate);

			}
			return didMove;
		}

		#endregion //Returning State

		#region Attack State
		void StateUpdate_Attack(double timeSinceLastUpdate)
		{
			UpdateAttack(timeSinceLastUpdate);
			if (CanMove() == true)
			{
				m_DestinationUpdate = MoveAttack(timeSinceLastUpdate);
			}

			CheckForTransitionsAttack();

		}
		void UpdateAttack(double timeSinceLastUpdate)
		{
			/*/ m_InCombat = true;
			bool canTakePartInCombat = ((m_movementAI != NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_RETURNING) && (!Dead));
            
			//the mob cannot take part in combat if it is returning or is dead
			if (canTakePartInCombat == true)
			{
				//if there is a target
				//if enough time has passed
				//check for new highest aggro target  
				double currentTime = Program.SecondsFromReferenceDate();
				if ((m_aggroList.Count > 0) && (currentTime > m_aggroRecheckTime + m_timeAtLastAggroCheck))
				{
					CombatEntity newTarget = GetValidTarget(true);
					if (newTarget != null)
					{
						AttackNewTarget(true, newTarget);
					}
					m_timeAtLastAggroCheck = currentTime;
				}
				CombatEntity mainTarget = GetMainTarget();
				//if the pathing failed
				//forget the innitial target and go to the next
				if (m_pathingObject.LastError == ASPathFinder.AS_PATHING_ERROR.NO_PATH && mainTarget != null)
				{
					m_failedAttackTargetPosition = mainTarget.CurrentPosition.m_position;
					m_failedAttackPosition = CurrentPosition.m_position;
					RemoveFromAggroLists(mainTarget);
					mainTarget = null;
				}
                

				//if there is no valid main target look for a new one
				if (mainTarget == null)
				{
					//choose a new Target
					CombatEntity newTarget = GetValidTarget(true);
					if (newTarget != null)
					{
						AttackNewTarget(true, newTarget);
					}
					else
					{
						//combat is over, return
						Return();
					}
				}
				else
				{
					/*if (NextSkill == null && CurrentSkill == null)
					{
                        

						SkillTemplate template = SkillTemplateManager.GetItemForID(SKILL_TYPE.SHARP_SHOT);
						if (template != null)
						{
							EntitySkill theSkill = new EntitySkill(template);
							m_combatManager.UseSkillOnEntity(theSkill, this, mainTarget);
						}
					}*/
			//}
			double currentTime = Program.MainUpdateLoopStartTime();

			m_combatAI.Update(this, timeSinceLastUpdate, currentTime);

			//}

		}
		bool MoveAttack(double timeSinceLastUpdate)
		{			
			return m_combatAI.MoveDueToCombat(this, timeSinceLastUpdate);
		}
		void CheckForTransitionsAttack()
		{
			//if nothing valid to kill
			//then return
			CombatEntity mainTarget = GetMainTarget();
			if (m_aiState == SCE_AI_STATE.IN_COMBAT && (mainTarget == null || m_pathingObject.LastError == ASPathFinder.AS_PATHING_ERROR.NO_PATH))
			{

				m_combatAI.ResetCombatAI(this);
				if (m_pathingObject.LastError == ASPathFinder.AS_PATHING_ERROR.NO_PATH && mainTarget != null)
				{
					m_failedAttackTargetPosition = mainTarget.CurrentPosition.m_position;
					m_hasFailedAttackTargetPosition = true;
					m_failedAttackPosition = CurrentPosition.m_position;
				}
				Return();
			}
		}
		#endregion //Attack State

		#endregion //States
		
		#region patrol methods
		/// <summary>
		/// sets the mob to return to it's home combat start point
		/// </summary>
		public void Return()
		{
			ClearDownAggroLists();
			m_recentDamages.Clear();
		    m_combatAI.MainTarget = null;
            Vector3 returnPosition = this.Gathering == LevelType.fish ? m_spawnPosition : m_chaseStart.m_position;
            SetDestination(returnPosition);

			ResetStatModifiers();
			ResetStatusEffects();

			m_currentSpeed = MaxSpeed;
			m_movementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_RETURNING;

			m_combatManager.StopAttacking(this);
			m_DestinationUpdate = true;
			InCombat = false;
			m_aiState = SCE_AI_STATE.RETURNING;

			// Set "NextSkill" and "NextSkillTarget" to null, to prevent mob casting skill while returning to start position
			NextSkill = null;
			NextSkillTarget = null;
		}
		void ResetStatusEffects()
		{
			for (int i = m_currentCharacterEffects.Count - 1; i >= 0; i--)
			{
				m_statusListChanged = true;
				m_currentCharacterEffects.RemoveAt(i);
				StatusCancelConditions.Reset();
				StatusPreventsActions.Reset();
			}

			for (int i = 0; i < m_template.m_permStatusEffects.Count; i++)
			{
				m_statusListChanged = true;

				CharacterEffectParams param = new CharacterEffectParams();
				param.charEffectId = m_template.m_permStatusEffects[i].m_effectID;
				param.caster = this;
				param.level = m_template.m_permStatusEffects[i].m_level;
				param.aggressive = false;
				param.PVP = false;
				param.statModifier = 0;
				CharacterEffectManager.InflictNewCharacterEffect(param, this);
			}
		}

		private bool HandleReturning(double timeSinceLastUpdate)
		{
			bool reachedDestination = true;

			if (reachedDestination)
			{

				//reset heading
				if (this.CurrentPosition.m_direction != this.m_spawnDirection)
				{
					this.CurrentPosition.m_direction = m_spawnDirection;
					this.SendEntityChangedDirectionMessage();
				}

				CurrentEnergy = MaxEnergy;
				CurrentHealth = MaxHealth;
				ResetStatusEffects();
				CurrentSkillTarget = null;
				CurrentSkill = null;
				HostileEntities.Clear();
				InCombat = false;
				NextSkillTarget = null;
				if (AttackTarget != null)
				{
					AttackTarget = null;
					m_combatManager.StopAttacking(this);
				}
				AttackTarget = null;

				NextSkill = null;
				ActionInProgress = false;
				ClearDownAggroLists();
				m_movementAI = m_defaultMovementAI;
				m_combatAI.ResetCombatAI(this);
				m_combatAI.BattleEnded(this);
				m_aiState = SCE_AI_STATE.IDLE;
				m_recentDamages.Clear();
				CombatDamageMessageData messageData = new CombatDamageMessageData();
				TakeDamage(messageData);
				if (m_lockOwner != null)
				{
					m_lockOwner.ResignOwnership(this);
				}
				m_backtrackDist = 0;


			}
			return false;
		}

		/// <summary>
		/// Checks if the entity is currently stopped by status effects
		/// </summary>
		/// <returns></returns>
		bool CanMove()
		{
			bool canMove = true;

			if (m_compiledStats.RunSpeed <= 0)
			{
				canMove = false;
			}
			if (!canMove)
			{
				if (!JustStopped)
				{
					m_DestinationUpdate = false;
				}
			}
			return canMove;
		}
		public void SetPatrol(List<PatrolPoint> patrolPoints)
		{
			m_movementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_PATROL;
			m_defaultMovementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_PATROL;
			m_patrolPoints = patrolPoints;
			m_currentWaypoint = 0;
			m_currentDestination.m_position = patrolPoints[0].Position;
			SetDestination(patrolPoints[0].Position);
			m_waiting = false;
			m_remainingWaitTime = 0;
		}
		public void SetRoamSettings(RandomPatrolSettings roamSettings)
		{

			m_movementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_ROAM;
			m_defaultMovementAI = NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_ROAM;
			m_roamingSettings = roamSettings;
		}
		/// <summary>
		/// updates the default movemetn of a creature
		/// will carry on a patrol or move to new random points, or stand there
		/// </summary>
		internal bool MoveAlongSetRoute(double timeSinceLastUpdate)
		{
			//move in the correct patrol style
			bool didMove = false;
			if (m_movementAI == NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_PATROL)
			{
				didMove = Patrol(timeSinceLastUpdate);
			}
			else if (m_movementAI == NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_ROAM)
			{
				didMove = Roam(timeSinceLastUpdate);
			}
			else if (m_movementAI != NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_STAND)
			{
				didMove = Stand(timeSinceLastUpdate);
				m_movementAI = m_defaultMovementAI;
			}

			if (m_pathingObject.Path == null && m_pathingObject.LastError == ASPathFinder.AS_PATHING_ERROR.NO_PATH)
			{
				List<Vector3> path = new List<Vector3>();
				path.Add(m_currentDestination.m_position);
				m_pathingObject.Path = path;
				m_currentPathDestination.m_position = m_currentPosition.m_position;
			}

			return didMove;
		}
		/// <summary>
		/// Used to change the start position when required
		/// some mobs start pos may move with another mob or may follow an object
		/// </summary>
		/// <param name="newStart"></param>
		internal void SetChaseStart(ActorPosition newStart)
		{
			m_chaseStart.m_position = newStart.m_position;
			m_chaseStart.m_yangle = newStart.m_yangle;
		}
		bool HandleAttackMovement(double timeSinceLastUpdate)
		{

			bool chaseDestinationChanged = false;

			chaseDestinationChanged = Chase(timeSinceLastUpdate);
			//MoveTowardsDestination(timeSinceLastUpdate);
			return chaseDestinationChanged;

		}
		internal bool MoveTowardsAttackTarget(double timeSinceLastUpdate)
		{

			double currentTime = Program.MainUpdateLoopStartTime();
			//it won't change destination if there is a skill pending or a battle action in progress
			bool midAttack = false;

			//this stalls movement until the attack report time
			if (ActionInProgress == true || currentTime < TimeActionWillComplete)
			{
				midAttack = true;
			}

			if (m_aiState != SCE_AI_STATE.IN_COMBAT || m_timeTillCanMove > currentTime || midAttack == true)
			{
				return m_DestinationUpdate;
			}
			bool didMove = false;
			if ((CurrentSkill == null) && (NextSkill == null))
			{
				HandleAttackMovement(timeSinceLastUpdate);
			}

			didMove = FollowPath(timeSinceLastUpdate);

			if ((m_pathingObject.Path == null) || ((m_pathingObject.Path != null) && (m_pathingObject.Path.Count > 0)))
			{
				return didMove;
			}

			return didMove;
		}
		bool IsWithinChaseRange(CombatEntity combatEntity, ActorPosition position)
		{
			if (combatEntity == null)
			{
				return false;
			}
			if (Utilities.Difference2DSquared(combatEntity.CurrentPosition.m_position, position.m_position) >= FollowRange * FollowRange)
			{
				return false;
			}
			return true;
		}
		bool Chase(double timeSinceLastUpdate)
		{

			m_currentSpeed = MaxSpeed * 1;
			CombatEntity target = m_combatAI.GetRunToTarget();
			if (target == null)
			{
				return false;
			}

			//check if current destination is close enough to attack
			float attackRange = CompiledStats.MaxAttackRange;// +AttackTarget.Radius;
			if (m_combatAI != null)
			{
				attackRange = m_combatAI.PreferredCombatRange;
			}
			Vector3 currentDestinationVector = target.CurrentPosition.m_position - m_currentDestination.m_position;
			currentDestinationVector.Y = 0;


			double distToAttackTarget = currentDestinationVector.Length();

			//don't include their radius
			//distToAttackTarget -= Radius;
			//don't include their radius
			distToAttackTarget -= (target.Radius + Radius);

			//do they need to move at all or are they in attack range

			// check if it's waiting for a path that it will backtrack itself
			bool preparingToSelfCorrect = false;
			if ((m_pathingObject.Path == null) && USE_CHASE_BACKTRACKING == true && m_pathingObject.OnRootTo(m_currentDestination.m_position))
			{
				preparingToSelfCorrect = true;
			}
			if ((distToAttackTarget <= attackRange) && ((distToAttackTarget >= 0 || preparingToSelfCorrect == true)))
			{
				//check if it's at the end of the path
				if (m_combatAI != null && ((m_pathingObject.Path != null) && (m_pathingObject.Path.Count == 0)))
				{
					m_combatAI.ReachedDesiredPosition();
				}
				return false;
			}


			Vector3 destinationVector = target.CurrentPosition.m_position - CurrentPosition.m_position;
			destinationVector.Y = 0;
			float distanceToTarget = Utilities.Difference2D(target.CurrentPosition.m_position, CurrentPosition.m_position);

			bool backingOff = false;
			//don't include their radius
			distanceToTarget -= (target.Radius + Radius);
            //distanceToTarget -= AttackTarget.Radius;

            // ignore static mobs, they can't move
            if (m_template.m_immobile == false && distanceToTarget < 0 && m_backtrackDist < MaxBacktrackDist)
            {
                //go a little further go make sure your far enough
                float extraRange = 0.5f;
                if (attackRange / 2 < extraRange)
                {
                    extraRange = attackRange / 2;
                }
                distanceToTarget *= -1;
                distanceToTarget += extraRange;
                destinationVector = destinationVector * -1;
                backingOff = true;
            }

			//float distanceToMove = distanceToTarget - attackRange;
			//don't go backwards
			if (distanceToTarget <= 0)
			{
				if (CurrentPosition.m_position != m_currentDestination.m_position)
				{
					m_currentDestination.m_position = CurrentPosition.m_position;
					return true;
				}
				return false;
			}
			//get close enough to attack
			//take the attack range off the distance to go
			//if your backing off you are already in range
			if (backingOff == false)
			{
				//remove a 1/4 of the range to allow for
				float rangeCutdown = attackRange / 4;
				if (rangeCutdown < 0.5f && attackRange > 0.5f)
				{
					rangeCutdown = 0.5f;
				}
				distanceToTarget -= (attackRange - rangeCutdown);
			}
			destinationVector.Normalize();
			Vector3 scaledDestinationVector = destinationVector * distanceToTarget;
			Vector3 destinationPosition = CurrentPosition.m_position + scaledDestinationVector;

			//now check the destination for collisions
			//if it can't find anywhere acceptable then use the innitial point
			bool positionPassed = false;
			Vector3 collisionPosition = m_zone.CheckCollisions(CurrentPosition.m_position, destinationPosition, Radius, 1, false);
			if ((collisionPosition - destinationPosition).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
			{
				positionPassed = true;
			}
			double radianChangePerTest = Math.PI / 4;
			double currentRadianTest = radianChangePerTest;
			Vector3 vectorFromTarget = destinationPosition - target.CurrentPosition.m_position;

			//if it fails collision try moving in a different direction
			while (currentRadianTest <= Math.PI && positionPassed == false)
			{
				Matrix rotateMatrix = Matrix.CreateRotationY((float)currentRadianTest);

				//test positive
				Vector3 rotatedScaledDestinationVector = vectorFromTarget;
				rotatedScaledDestinationVector = Vector3.Transform(rotatedScaledDestinationVector, rotateMatrix);

				Vector3 rotatedPosition = target.CurrentPosition.m_position + rotatedScaledDestinationVector;
				Vector3 rotationCollisionPosition = m_zone.CheckCollisions(CurrentPosition.m_position, rotatedPosition, Radius, 1, false);
				if ((rotationCollisionPosition - rotatedPosition).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
				{
					positionPassed = true;
					destinationPosition = rotatedPosition;
					destinationVector = -rotatedScaledDestinationVector;
					destinationVector.Normalize();
				}
				//test -ve
				if (positionPassed == false)
				{
					rotateMatrix = Matrix.CreateRotationY((float)-currentRadianTest);

					//test positive
					rotatedScaledDestinationVector = vectorFromTarget;
					rotatedScaledDestinationVector = Vector3.Transform(rotatedScaledDestinationVector, rotateMatrix);

					rotatedPosition = target.CurrentPosition.m_position + rotatedScaledDestinationVector;
					rotationCollisionPosition = m_zone.CheckCollisions(CurrentPosition.m_position, rotatedPosition, Radius, 1, false);
					if ((rotationCollisionPosition - rotatedPosition).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
					{
						positionPassed = true;
						destinationPosition = rotatedPosition;
						destinationVector = -rotatedScaledDestinationVector;
						destinationVector.Normalize();
					}
				}
				currentRadianTest += radianChangePerTest;
			}

			if ((destinationPosition - m_currentDestination.m_position).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
			{
				return false;
			}

			SetDestination(destinationPosition);
			if (USE_CHASE_BACKTRACKING == true && m_pathingObject != null && m_pathingObject.Path == null && backingOff == false)
			{
				// pathDestination = target.CurrentPosition.m_position;
				AmendDestination(target.CurrentPosition.m_position);
			}

			if (backingOff)
			{
				m_backtrackDist += Utilities.Difference2D(CurrentPosition.m_position, destinationPosition);
			}

			m_currentDestination.m_direction = destinationVector;
			m_currentDestination.CorrectAngleForDirection();
			return true;
		}
		void BackTrackChasePath()
		{
			//check the attack targets position
			//if there is no attack target or you are already close enough to attack then its probably not a valid chase
			//the path will be to the target, 
			//Now backtrack until at the correct range
			//what is the desired range
			CombatEntity target = null;
			float attackRange = CompiledStats.MaxAttackRange;// +AttackTarget.Radius;
			if (m_combatAI != null)
			{
				target = m_combatAI.GetRunToTarget(); //AttackTarget;
				attackRange = m_combatAI.PreferredCombatRange;
			}
			//add the radius to the distance you wish to move back

			//if (backingOff == false)
			{
				//remove a 1/4 of the range to allow for
				float rangeCutdown = attackRange / 4;
				if (rangeCutdown < 0.5f && attackRange > 0.5f)
				{
					rangeCutdown = 0.5f;
				}
				attackRange -= (attackRange - rangeCutdown);
			}

			if (target != null)
			{
				attackRange += (target.Radius + Radius);
			}

			//check the attack targets position
			//if there is no attack target or you are already close enough to attack then its probably not a valid chase
			if (target == null || Utilities.Difference2D(target.CurrentPosition.m_position, CurrentPosition.m_position) < attackRange)
			{
				return;
			}
			//you want to be a bit closer than needed
			List<Vector3> newPath = m_pathingObject.Path;
			//somethings gone wrong, pullout
			if (newPath == null)
			{
				Program.Display("BackTrackChasePath tried to backtrack null path for mob " + ServerID);
				return;
			}

			//need to go backwards until you have moved back enough
			bool pathSet = false;
			double totalDistanceBacktracked = 0;
			//for this point
			for (int i = newPath.Count - 2; i >= -1 && pathSet == false; i--)
			{
				//get the points
				Vector3 currentPoint = new Vector3(999999);
				if (i >= 0)
				{
					currentPoint = newPath[i];
				}
				else
				{
					currentPoint = CurrentPosition.m_position;
				}
				Vector3 previousPoint = newPath[i + 1];
				//what is the distance from the last point
				Vector3 lineVector = currentPoint - previousPoint;
				lineVector.Y = 0;
				double distToPoint = lineVector.Length();
				//if its more than the range then Go Part way and set the point
				if (distToPoint + totalDistanceBacktracked >= attackRange)
				{
					double distanceToMoveOnVec = attackRange - totalDistanceBacktracked;
					Vector3 newFinalPoint = previousPoint;

					if (distanceToMoveOnVec <= 0)
					{
						newFinalPoint = previousPoint;
					}
					else if (distanceToMoveOnVec >= distToPoint)
					{
						newFinalPoint = currentPoint;
					}
					else
					{
						double amountAlongVec = distanceToMoveOnVec / distToPoint;

						newFinalPoint = previousPoint + lineVector * amountAlongVec;

					}

					newPath[i + 1] = newFinalPoint;
					pathSet = true;
				}
				else
				{
					//keep track of how far back the path has moved
					totalDistanceBacktracked += distToPoint;
					//remove the point that should not be considered
					newPath.RemoveAt(i + 1);
				}
			}
			m_currentDestination.m_position = newPath.Last();

			//change the destination on the pathing object

		}
		internal override void StopTheEntity()
		{
			StopAtCurrentPosition();
			base.StopTheEntity();
		}
		bool Roam(double timeSinceLastUpdate)
		{
			m_currentSpeed = m_patrolSpeed;


			if (m_waiting == true)
			{

				m_remainingWaitTime -= timeSinceLastUpdate;
				if (m_remainingWaitTime < 0)
				{
					m_waiting = false;

					float randX = (Program.getRandomNumber((int)(2 * m_roamingSettings.Radius * 100))) / 100.0f - m_roamingSettings.Radius;
					float randY = 0;
					float randZ = (Program.getRandomNumber((int)(2 * m_roamingSettings.Radius * 100))) / 100.0f - m_roamingSettings.Radius;
					SetDestination(m_spawnPosition + new Vector3(randX, randY, randZ)); ;
					return true;
				}
				m_currentSpeed = 0;
				return false;
			}
			bool destinationChanged = FollowPath(timeSinceLastUpdate);

			if ((m_pathingObject.Path == null) || ((m_pathingObject.Path != null) && (m_pathingObject.Path.Count > 0)))
			{
				return destinationChanged;
			}

			double distanceThisFrame = (float)timeSinceLastUpdate * m_currentSpeed;
			Vector3 vectorBetweenTarget = m_currentDestination.m_position - CurrentPosition.m_position;
			vectorBetweenTarget.Y = 0;
			double distanceBetweenTarget = vectorBetweenTarget.Length();

			vectorBetweenTarget.Normalize();
			//if you have points on the path go to the next point

			//if your path is empty then go direct

			if (distanceBetweenTarget > 0)
			{
				//CurrentPosition.m_position = CurrentPosition.m_position + vectorBetweenTarget * distanceThisFrame;
			}
			if (distanceThisFrame > distanceBetweenTarget || ((m_pathingObject.Path != null) && (m_pathingObject.Path.Count == 0)))
			{

				if (Program.getRandomNumber(100) < m_roamingSettings.ProbabilityWait)
				{
					m_waiting = true;
					m_remainingWaitTime = Program.getRandomNumber((int)((m_roamingSettings.MaxWaitTime - m_roamingSettings.MinWaitTime) * 100)) / 100 + m_roamingSettings.MinWaitTime;
				}
				else
				{
					float randX = (Program.getRandomNumber((int)(2 * m_roamingSettings.Radius * 100))) / 100.0f - m_roamingSettings.Radius;
					float randY = 0;
					float randZ = (Program.getRandomNumber((int)(2 * m_roamingSettings.Radius * 100))) / 100.0f - m_roamingSettings.Radius;
					SetDestination(m_spawnPosition + new Vector3(randX, randY, randZ));
					return false;// true;
				}
				//send out next point
			}

			return false;
		}
		bool Patrol(double timeSinceLastUpdate)
		{
			m_currentSpeed = m_patrolSpeed;
			if (m_waiting == true)
			{
				m_remainingWaitTime -= timeSinceLastUpdate;

				if (m_remainingWaitTime < 0)
				{
					m_waiting = false;
					m_currentWaypoint++;
					if (m_currentWaypoint >= m_patrolPoints.Count)
					{
						m_currentWaypoint = 0;
					}
					SetDestination(m_patrolPoints[m_currentWaypoint].Position);
					return true;
				}
				m_currentSpeed = 0;
				return false;
			}

			bool destinationChanged = FollowPath(timeSinceLastUpdate);

			if ((m_pathingObject.Path == null) || ((m_pathingObject.Path != null) && (m_pathingObject.Path.Count > 0)))
			{
				return destinationChanged;
			}

			double distanceThisFrame = timeSinceLastUpdate * m_currentSpeed;
			Vector3 vectorBetweenTarget = m_currentDestination.m_position - CurrentPosition.m_position;
			double distanceBetweenTarget = vectorBetweenTarget.Length();

			vectorBetweenTarget.Normalize();
			if (distanceBetweenTarget > 0)
			{
				//CurrentPosition.m_position = CurrentPosition.m_position + vectorBetweenTarget * distanceThisFrame;
			}
			if (distanceThisFrame > distanceBetweenTarget)
			{

				if (m_patrolPoints[m_currentWaypoint].StopTime > 0)
				{
					m_waiting = true;
					m_remainingWaitTime = m_patrolPoints[m_currentWaypoint].StopTime;
				}
				else
				{
					m_currentWaypoint++;
					if (m_currentWaypoint >= m_patrolPoints.Count)
					{
						m_currentWaypoint = 0;
					}
					SetDestination(m_patrolPoints[m_currentWaypoint].Position);
					return false;//true;
				}
				//send out next point        
			}

			return false;
		}

		bool Stand(double timeSinceLastUpdate)
		{

			bool destinationChanged = FollowPath(timeSinceLastUpdate);
			//if your following a path back to your start position then keep going
			if ((m_pathingObject.Path == null) || ((m_pathingObject.Path != null) && (m_pathingObject.Path.Count > 0)))
			{
				return destinationChanged;
			}
			//if your far from your home point but not trying to get their then try to get there
			double distFromSpawnPoint = Utilities.Difference2DSquared(CurrentPosition.m_position, m_spawnPosition);
			if (distFromSpawnPoint > MAX_SQUARED_COLLISION_ERROR)
			{
				SetDestination(m_spawnPosition);
			}
			else
			{
				if (CurrentPosition.m_direction != m_spawnDirection)
				{
					destinationChanged = true;
				}
				CurrentPosition.m_direction = m_spawnDirection;
				CurrentPosition.CorrectAngleForDirection();
				m_currentPathDestination.m_direction = m_spawnDirection;
				m_currentPathDestination.CorrectAngleForDirection();
			}

			return destinationChanged;
		}

		void SetDestination(Vector3 destination)
		{

			m_currentDestination.m_position = destination;
			m_pathingObject.SetUpForSearch(m_currentPosition.m_position, destination);
			//if you'r already their just stop
			if (Utilities.Difference2DSquared(CurrentPosition.m_position, destination) < MAX_SQUARED_COLLISION_ERROR)
			{
				List<Vector3> path = new List<Vector3>();
				m_pathingObject.Path = path;
				m_currentPathDestination.m_position = CurrentPosition.m_position;

				return;
			}

			//if you can get to the target or astar is disabled then run directly there
			bool canRunDirect = false;
			Vector3 runDirectPos = m_zone.CheckCollisions(m_currentPosition.m_position, destination, 1, 1, false);
			if ((runDirectPos - destination).LengthSquared() < MAX_SQUARED_COLLISION_ERROR)
			{
				canRunDirect = true;
			}

			if (canRunDirect == true || Program.m_AIMapEnabled == false)
			{
				// Program.Display("I can reach the Player");
				List<Vector3> path = new List<Vector3>();
				path.Add(destination);
				m_pathingObject.Path = path;
				m_currentPathDestination.m_position = CurrentPosition.m_position;

				//m_DestinationUpdate = true;
			}
		}
		/// <summary>
		/// This requests a new Destination
		/// Does not check collisions
		/// used when a collision check has failed so a chase path will now go to the target and then amand the path
		/// </summary>
		/// <param name="newDestination"></param>
		void AmendDestination(Vector3 newDestination)
		{
			//Program.Display(GetIDString() + " amended destination to " + newDestination.ToString());
			m_pathingObject.SetUpForSearch(m_currentPosition.m_position, newDestination);

		}

		bool FollowPath(double timeSinceLastUpdate)
		{
			bool mobMoved = false;
			bool destinationChanged = false;
			List<Vector3> path = m_pathingObject.Path;
			//if you do not yet have a path
			if (path == null)
			{
				double startTime = NetTime.Now;
				//you have nowhere to go so stay where you are
				m_currentPathDestination.m_position = CurrentPosition.m_position;
				path = m_zone.PathFinder.GetPath(m_pathingObject);
				if (path == null)
				{

					if (m_pathingObject.LastError != ASPathFinder.AS_PATHING_ERROR.PATHING_LIMIT_EXCEEDED)
					{
						//tell the mob it's fath failed
					}
					if (m_pathingObject.LastError != ASPathFinder.AS_PATHING_ERROR.NONE && m_pathingObject.LastError != ASPathFinder.AS_PATHING_ERROR.ON_SAME_TRIANGLE)
					{
						if (Program.m_LogPathingErrors)
						{
							Program.Display("Mob spawn " + m_serverID + " has pathing error " + m_pathingObject.LastError + " trying to get from " + m_pathingObject.StartPoint + " to " + m_pathingObject.EndPoint + " took " + NetTime.ToReadable(NetTime.Now - startTime));
						}
					}
					return false;
				}
				m_pathingObject.Path = path;
				//this path needs amended
				if (m_aiState == SCE_AI_STATE.IN_COMBAT && USE_CHASE_BACKTRACKING == true)
				{
					BackTrackChasePath();

				}
			}

			if (path.Count == 0)
			{
				//you have nowhere to go so stay where you are
				m_currentPathDestination.m_position = CurrentPosition.m_position;
				//perhaps call a reaches end of path function
				return false;
			}

			Vector3 currentDestination = path.First();
			if (m_currentPathDestination.m_position != currentDestination)
			{
				Vector3 pathDirection = currentDestination - CurrentPosition.m_position;
				pathDirection.Y = 0;
				if (pathDirection.LengthSquared() > 0)
				{
					pathDirection.Normalize();
					CurrentPosition.m_direction = pathDirection;
				}

				m_currentPathDestination.m_position = currentDestination;

				destinationChanged = true;
			}

			double distanceThisFrame = timeSinceLastUpdate * m_currentSpeed;
			Vector3 vectorBetweenTarget = currentDestination - CurrentPosition.m_position;
			vectorBetweenTarget.Y = 0;
			double distanceBetweenTarget = vectorBetweenTarget.Length();

			vectorBetweenTarget.Normalize();
			if (distanceBetweenTarget > 0)
			{
				CurrentPosition.m_position = CurrentPosition.m_position + vectorBetweenTarget * distanceThisFrame;
				mobMoved = true;
			}
			if (distanceThisFrame > distanceBetweenTarget)
			{

				m_pathingObject.Path.RemoveAt(0);

				CurrentPosition.m_position = currentDestination;
				if (path.Count > 0)
				{
					m_currentPathDestination.m_position = m_pathingObject.Path[0];
					destinationChanged = true;
					if (Program.m_A_Star_Debugging)
					{
						Program.Display("Reached position = " + CurrentPosition.m_position.X + "," + CurrentPosition.m_position.Y + "," + CurrentPosition.m_position.Z + " " + " next destination =" + m_currentPathDestination.m_position.X + "," + m_currentPathDestination.m_position.Y + "," + m_currentPathDestination.m_position.Z);
					}

				}
				mobMoved = true;
				//send out next point



			}
			if (mobMoved == true)
			{
				EntityPartitionCheck();
			}
			return destinationChanged;
		}

		#endregion
		
		#region Write To Network Messages
		public bool WriteDestinationToMessage(NetOutgoingMessage updatemsg, int mobNumber, Character character)
		{
			bool added = false;
			double currentDistanceToMobDestination = Utilities.Difference2DSquared(m_currentPathDestination.m_position, character.m_CharacterPosition.m_position);
			double currentDistanceToMobCurrent = Utilities.Difference2DSquared(CurrentPosition.m_position, character.m_CharacterPosition.m_position);
			double projectedDistanceToMobDestination = Utilities.Difference2DSquared(m_currentPathDestination.m_position, character.m_ProjectedPosition.m_position);
			double projectedDistanceToMobCurrent = Utilities.Difference2DSquared(CurrentPosition.m_position, character.m_ProjectedPosition.m_position);
			bool willBeInterested = (currentDistanceToMobDestination < SQUARED_POSITION_SEND_DIST ||
				currentDistanceToMobCurrent < SQUARED_POSITION_SEND_DIST ||
				projectedDistanceToMobDestination < SQUARED_POSITION_SEND_DIST ||
				projectedDistanceToMobCurrent < SQUARED_POSITION_SEND_DIST);

			if (((NetTime.Now - m_lastUpdateSent > 5 && (m_movementAI == NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_PATROL || m_movementAI == NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_ROAM)) || (m_DestinationUpdate || JustStopped == true))
				&& willBeInterested && (m_currentSpeed > 0 || JustStopped == true))
			{
				//  Program.Display("patrol update " + distanceToMobCurrent + " " + distanceToMobDestination + " " + m_currentDestination.m_position.X + "," + m_currentDestination.m_position.Y + "," + m_currentDestination.m_position.Z);
				//write the server ID
				updatemsg.WriteVariableUInt32((uint)mobNumber);
				//work out the eta 
				Vector3 diff = m_currentPathDestination.m_position - CurrentPosition.m_position;
				diff.Y = 0;
				double timeToGetThere = 0;
				if (m_currentSpeed > 0)
				{
					timeToGetThere = diff.Length() / m_currentSpeed;
				}
				//  Program.Display("distance to mob " + diff.Length() + " speed " + m_currentSpeed + " time to get there " + timeToGetThere);

				updatemsg.Write(NetTime.Now + timeToGetThere);
				updatemsg.Write((float)CurrentPosition.m_position.X);
				updatemsg.Write((float)CurrentPosition.m_position.Z);

				updatemsg.Write((float)m_currentPathDestination.m_position.X);
				updatemsg.Write((float)m_currentPathDestination.m_position.Z);
				//Program.Display("start=" + CurrentPosition.m_position.X + "," + CurrentPosition.m_position.Z + " end=" + m_currentDestination.m_position.X + "," + m_currentDestination.m_position.Z);
				if (m_aiState == SCE_AI_STATE.RETURNING)
				{
					updatemsg.Write((byte)1);
				}
				else
				{
					updatemsg.Write((byte)0);
				}
				m_lastUpdateSent = NetTime.Now;
				added = true;
			}
			return added;
		}
		public bool WriteDestinationToMessageNoChecks(NetOutgoingMessage updatemsg, Character character)
		{
			bool added = false;
			double currentDistanceToMobDestination = 0;//Utilities.Difference2DSquared(m_currentPathDestination.m_position, character.m_CharacterPosition.m_position);
			double currentDistanceToMobCurrent = 0;// Utilities.Difference2DSquared(CurrentPosition.m_position, character.m_CharacterPosition.m_position);
			double projectedDistanceToMobDestination = 0;// Utilities.Difference2DSquared(m_currentPathDestination.m_position, character.m_ProjectedPosition.m_position);
			double projectedDistanceToMobCurrent = 0;// Utilities.Difference2DSquared(CurrentPosition.m_position, character.m_ProjectedPosition.m_position);

			if (character != null)
			{
				currentDistanceToMobDestination = Utilities.Difference2DSquared(m_currentPathDestination.m_position, character.m_CharacterPosition.m_position);
				currentDistanceToMobCurrent = Utilities.Difference2DSquared(CurrentPosition.m_position, character.m_CharacterPosition.m_position);
				projectedDistanceToMobDestination = Utilities.Difference2DSquared(m_currentPathDestination.m_position, character.m_ProjectedPosition.m_position);
				projectedDistanceToMobCurrent = Utilities.Difference2DSquared(CurrentPosition.m_position, character.m_ProjectedPosition.m_position);
			}
			bool willBeInterested = (currentDistanceToMobDestination < SQUARED_POSITION_SEND_DIST ||
				currentDistanceToMobCurrent < SQUARED_POSITION_SEND_DIST ||
				projectedDistanceToMobDestination < SQUARED_POSITION_SEND_DIST ||
				projectedDistanceToMobCurrent < SQUARED_POSITION_SEND_DIST);

			if (willBeInterested)
			{
				//  Program.Display("patrol update " + distanceToMobCurrent + " " + distanceToMobDestination + " " + m_currentDestination.m_position.X + "," + m_currentDestination.m_position.Y + "," + m_currentDestination.m_position.Z);
				//write the server ID
				updatemsg.WriteVariableUInt32((uint)m_serverID);
				//work out the eta 
				Vector3 diff = m_currentPathDestination.m_position - CurrentPosition.m_position;
				double timeToGetThere = 0;
				if (m_currentSpeed > 0)
				{
					timeToGetThere = diff.Length() / m_currentSpeed;
				}
				//  Program.Display("distance to mob " + diff.Length() + " speed " + m_currentSpeed + " time to get there " + timeToGetThere);

				updatemsg.Write(NetTime.Now + timeToGetThere);
				Vector3 currentPos = CurrentPosition.m_position;
				if (diff.LengthSquared() < 0.01)
				{
					currentPos = currentPos - (0.01f * CurrentPosition.m_direction);
				}
				updatemsg.Write((float)currentPos.X);
				updatemsg.Write((float)currentPos.Z);

				updatemsg.Write((float)m_currentPathDestination.m_position.X);
				updatemsg.Write((float)m_currentPathDestination.m_position.Z);
				//Program.Display("start=" + CurrentPosition.m_position.X + "," + CurrentPosition.m_position.Z + " end=" + m_currentDestination.m_position.X + "," + m_currentDestination.m_position.Z);
				//if (m_movementAI == NPC_MOVEMENT_AI.NPC_MOVEMENT_AI_RETURNING)
				if (m_aiState == SCE_AI_STATE.RETURNING)
				{
					updatemsg.Write((byte)1);
				}
				else
				{
					updatemsg.Write((byte)0);
				}
				m_lastUpdateSent = NetTime.Now;
				added = true;
			}
			return added;
		}


		#endregion
		
		#region combat

		void SetUpCombatEnt()
		{
			//JT STATS CHANGES 12_2011
			BaseMaxEnergy = m_template.m_maxEnergy;
			//this will need to be done when the final stats have been worked out
			CurrentHealth = MaxHealth;
			CurrentEnergy = MaxEnergy;
			BaseAttack = m_template.m_attack;
			BaseDefence = m_template.m_defence;
			BaseAttackSpeed = m_template.m_attack_speed;



			BaseArmourValue = m_template.m_armour_value;
			Radius = m_template.m_radius;
			for (int i = 0; i < m_template.m_damageTypes.Count; i++)
			{
				FloatForID currentType = m_template.m_damageTypes[i];
				m_baseStats.SetOtherDamageType(currentType.m_bonusType, currentType.m_amount);
			}
			for (int i = 0; i < m_template.m_bonusTypes.Count; i++)
			{
				FloatForID currentType = m_template.m_bonusTypes[i];
				m_baseStats.SetBonusType(currentType.m_bonusType, currentType.m_amount);
			}
			for (int i = 0; i < m_template.m_avoidanceTypes.Count; i++)
			{
				FloatForID currentType = m_template.m_avoidanceTypes[i];
				m_baseStats.SetAvoidanceType((AVOIDANCE_TYPE)currentType.m_bonusType, currentType.m_amount);
			}
			for (int i = 0; i < m_template.m_immunityTypes.Count; i++)
			{
				FloatForID currentType = m_template.m_immunityTypes[i];
				m_baseStats.SetImmunityType(currentType.m_bonusType, currentType.m_amount);
			}
			for (int i = 0; i < m_template.m_damageReductionTypes.Count; i++)
			{
				FloatForID currentType = m_template.m_damageReductionTypes[i];
				m_baseStats.SetDamageReductionType(currentType.m_bonusType, currentType.m_amount);
				//manually set all non fish to be immune to fishing damage
				if(this.Gathering == LevelType.none)
					m_baseStats.SetDamageReductionType((int)DAMAGE_TYPE.FISHING_DAMAGE, 1);
			}

			m_baseStats.MaxHealth = m_template.m_maxHitpoints;
			m_baseStats.MaxEnergy = m_template.m_maxEnergy;
			m_baseStats.Attack = m_template.m_attack;
			m_baseStats.Armour = m_template.m_armour_value;
			m_baseStats.AttackSpeed = m_template.m_attack_speed;
			Radius = m_template.m_radius;

			MaxSpeed = 7;
			//Compile all of the stats
			CompileStats();

			CurrentHealth = MaxHealth;
			CurrentEnergy = MaxEnergy;

		}
		void ClearDownAggroLists()
		{

			m_aggroList.Clear();
		}
		/// <summary>
		/// This will remove the damage from the health and signal that the entities health has changed so needs to be resent
		/// </summary>
		/// <param name="theDamage"></param>
		internal override void TakeDamage(CombatDamageMessageData theDamage)
		{
			
            base.TakeDamage(theDamage);
            
			m_recentDamages.Add(theDamage);

		}
		/// <summary>
		/// called when a skill starts it's casting time
		/// </summary>
		internal override void StartCasting()
		{
			StopAtCurrentPosition();
			base.StartCasting();
		}
		internal override void ActUponDamage(int damage, CombatEntity caster, CombatManager.ATTACK_TYPE attackType, int attackID, bool aggressive, double aggroModifier)
		{
			if (caster != null && caster != this && aggressive)
			{

				//if the aggro list is currently empty, now is the time to ask for help
				if (m_aggroList.Count == 0)
				{
					CurrentZone.RequestAssistance(this, caster, Zone.MAX_ASSIST_DISTANCE);
				}

				float aggroValue = 0;
				if (damage > 0)
				{
					//add this aggro to the data
					aggroValue += (float)(damage * caster.AggroModifier);
				}
				if (attackType == CombatManager.ATTACK_TYPE.SKILL)
				{
					//get the skill
					SkillTemplate theSkillTemplate = SkillTemplateManager.GetItemForID((SKILL_TYPE)attackID);
					//add the aggro
					if (theSkillTemplate != null)
					{
						aggroValue += (float)aggroModifier;
					}
				}
				AddToAggroValue(caster, aggroValue);
				AggroData aggroData = GetAggroForEntity(caster);
				if (aggroData != null && damage > 0)
				{
					aggroData.TotalDamage += damage;
				}
				/* // check if in range
				 //add this data to the aggro list
				 //find the agro data for this caster
				 AggroData theAggroData = null;
				 for (int i = 0; i < m_aggroList.Count; i++)
				 {
					 AggroData currentAggroData = m_aggroList[i];
					 if (currentAggroData != null && currentAggroData.LinkedCharacter == caster)
					 {
						 theAggroData = currentAggroData;
					 }
				 }
                
                
                
				 if(theAggroData==null)
				 //if you cant find the agressor add them to the list
				 {

					 theAggroData = new AggroData(caster);
					 m_aggroList.Add(theAggroData);
				 }
				 double currentTime = Program.SecondsFromReferenceDate();
				 if (damage>0){
				 //add this aggro to the data
					 float aggroDamage = (float)(damage * caster.AggroModifier);
					 theAggroData.AddToAggro(aggroDamage,currentTime);
					 //theAggroData.AggroRating += damage*caster.AggroModifier;
					 theAggroData.TotalDamage += damage;
					 AggroNeedsRechecked(currentTime);
				 }
				 if (attackType == CombatManager.ATTACK_TYPE.SKILL)
				 {
					 //get the skill
					 SkillTemplate theSkillTemplate = SkillTemplateManager.GetItemForID((SKILL_TYPE)attackID);
					 //add the aggro
					 if (theSkillTemplate != null)
					 {
						 float aggroDamage = (float)aggroModifier;
                        
						 theAggroData.AddToAggro(aggroDamage, currentTime);
						 AggroNeedsRechecked(currentTime);
						 //theAggroData.AggroRating += 10 * caster.AggroModifier;
					 }
                    
				 }*/

				if (caster.Type == EntityType.Player && m_lockOwner == null)
				{
					Character ownerCharacter = (Character)caster;
					if (ownerCharacter != null)
					{
						ITargetOwner newOwner = ownerCharacter;
						if (ownerCharacter.CharacterParty != null)
						{
							newOwner = ownerCharacter.CharacterParty;
						}
						bool validOwner = IsValidOwner(newOwner);

						bool doneEnoughDamage = false;
						//do not calculate the damage if the owner is not valid
						if (validOwner == true)
						{
							doneEnoughDamage = DamageByTargetOwnerOver(m_percentHealthForLock, newOwner);
						}

						if (validOwner == true && doneEnoughDamage == true)
						{
							newOwner.TakeOwnership(this);
							newOwner.NotifyOwnershipTaken(this);
						}
					}
				}
			}
		}
		bool DamageByTargetOwnerOver(float percent, ITargetOwner owner)
		{


			bool damageOver = false;
			// the minimum level of target owner to check
			int minAvailLevel = Level  - (int)LOCK_LOWER_LVL_LIMIT;
			// the maximum level of target owner to check
			int maxAvailLevel = Level + (int)LOCK_UPPER_LVL_LIMIT;
			// the level below which the contribution should be reduced
			int lowerDegradeLevel = Level - (int)LOCK_LOWER_DEGRADE_START;
			// the level above which the contribution should be reduced
			int upperDegradeLevel = Level + (int)LOCK_UPPER_DEGRADE_START;
			float upperRange = LOCK_UPPER_LVL_LIMIT - LOCK_UPPER_DEGRADE_START;
			float lowerRange = LOCK_LOWER_LVL_LIMIT - LOCK_LOWER_DEGRADE_START;

			List<Character> attackerList = owner.GetCharacters;
			int targetDamage = (int)(MaxHealth * percent);
			// float combinedDamage = 0;
			float progressToTarget = 0;
			for (int i = 0; i < attackerList.Count && damageOver == false; i++)
			{
				Character currentCharacter = attackerList[i];
				int ownerLevel = currentCharacter.GetRelevantLevel(this);

				if (ownerLevel > 220)
				{
					ownerLevel = 220;
				}

				if (ownerLevel > minAvailLevel && ownerLevel < maxAvailLevel)
				{
					AggroData dataForEntity = GetAggroForEntity(currentCharacter);
					if (dataForEntity != null)
					{
						//initially assume no 
						float reductionAmount = 0;
						float contributionRate = 1.0f;
						//if the players level is in the upper degradation range
						if (ownerLevel > upperDegradeLevel)
						{
							reductionAmount =
								//how far within the range is the player
								((ownerLevel - upperDegradeLevel) /
								//the upper degradation range
								(upperRange));
							contributionRate = 1 - reductionAmount;
							//how far within the range is the player
							/* ((ownerLevel - upperDegradeLevel) /
							 //the upper degradation range
							 (LOCK_UPPER_LVL_LIMIT - LOCK_UPPER_DEGRADE_START));*/
						}
						//if the players level is in the lower degradation range
						else if (ownerLevel < lowerDegradeLevel)
						{
							reductionAmount =
								//how far within the range is the player
								((lowerDegradeLevel - ownerLevel) /
								//the lower degradation range
								(lowerRange));
							contributionRate = 1 - reductionAmount;
							//how far within the range is the player
							/* ((lowerDegradeLevel-ownerLevel)/
							 //the lower degradation range
							 (LOCK_LOWER_LVL_LIMIT-LOCK_LOWER_DEGRADE_START));*/
						}
						if (reductionAmount > 1) { reductionAmount = 1; }
						else if (reductionAmount < 0) { reductionAmount = 0; }
						//clamp the values in case something has gone wrong
						if (contributionRate > 1) { contributionRate = 1; }
						else if (contributionRate < 0) { contributionRate = 0; }
						/*float currentDamage = contributionRate*dataForEntity.TotalDamage;
						combinedDamage += currentDamage;

						if (combinedDamage >= targetDamage)
						{
							damageOver = true;
						}*/
						//get the target health that this player would need to take off the mob to lock it
						float currentTarget = MaxHealth * ((reductionAmount * (1 - percent)) + percent);
						float currentContribution = dataForEntity.TotalDamage / currentTarget;
						progressToTarget += currentContribution;
						if (progressToTarget >= 1)
						{
							damageOver = true;
						}
					}
				}
			}


			return damageOver;
		}
		void ResetDamageByTargetOwner(ITargetOwner owner)
		{

			List<Character> attackerList = owner.GetCharacters;

			for (int i = 0; i < attackerList.Count; i++)
			{
				Character currentCharacter = attackerList[i];
				int ownerLevel = currentCharacter.GetRelevantLevel(this);

				AggroData dataForEntity = GetAggroForEntity(currentCharacter);
				if (dataForEntity != null)
				{
					dataForEntity.TotalDamage = 0;
				}
			}

		}

		override internal void ClearAggroForEntity(CombatEntity entityToChangeAggro)
		{
			AggroData targetAggro = GetAggroForEntity(entityToChangeAggro);
			if (targetAggro != null)
			{
				targetAggro.ClearAggro();
			}
		}
		override internal void EntityAidedByEntity(CombatEntity targetedEnt, CombatEntity assistingEnt, float AggroOfAssist)
		{
			// CombatEntity mainTarget = null;
			AggroData targetAggro = GetAggroForEntity(targetedEnt);

			/*if (AttackTarget != null)
			{
				mainTarget = AttackTarget;
			}*/

			//if (mainTarget!=null && targetedEnt == mainTarget)
			//do they have any hate for this target
			if (targetAggro != null)
			{
				AggroData theAggroData = null;
				for (int i = 0; i < m_aggroList.Count; i++)
				{
					AggroData currentAggroData = m_aggroList[i];
					if (currentAggroData != null && currentAggroData.LinkedCharacter == assistingEnt)
					{
						theAggroData = currentAggroData;
					}
				}



				if (theAggroData == null)
				//if you cant find the agressor add them to the list
				{

					theAggroData = new AggroData(assistingEnt);
					m_aggroList.Add(theAggroData);
				}
				if (AggroOfAssist > 0)
				{
					//add this aggro to the data
					double currentTime = Program.MainUpdateLoopStartTime();
					theAggroData.AddToAggro(AggroOfAssist, currentTime);
					AggroNeedsRechecked(currentTime);
					// theAggroData.AssistAggroRating += AggroOfAssist;

				}
			}
		}

		internal override StatusEffect InflictNewStatusEffect(EFFECT_ID newEffect, CombatEntity caster, int level, bool aggressive, bool PVP, float statModifier)
		{
			StatusEffect effectInflicted = base.InflictNewStatusEffect(newEffect, caster, level, aggressive, PVP, statModifier);
			//if ((effectInflicted != null) && ((newEffect == EFFECT_ID.GRASPING_ROOTS) || (newEffect == EFFECT_ID.MAX_ROOT)))
			//{
			//    //tell the player he's stuck
			//    JustStopped = true;
			//    m_DestinationUpdate = true;
			//    m_currentPathDestination.m_position = CurrentPosition.m_position;
			//    m_currentDestination.m_position = CurrentPosition.m_position;
			//    m_currentDestination.m_currentSpeed = 0;
			//}

			return effectInflicted;

		}

		internal void StopAtCurrentPosition()
		{
			SetDestination(CurrentPosition.m_position);
			JustStopped = true;
			m_DestinationUpdate = true;
			m_currentPathDestination.m_position = CurrentPosition.m_position;
			m_currentDestination.m_position = CurrentPosition.m_position;
			m_currentDestination.m_currentSpeed = 0;
		}
		internal void UseSkill(MobSkill theSkill, CombatEntity theTarget)
		{
			if (m_combatManager != null && theSkill != null && theSkill.TheSkill != null)
			{
				StopAtCurrentPosition();
				m_combatManager.UseSkillOnEntity(theSkill.TheSkill, this, theTarget);
			}
		}
		
		internal override void EndAttack()
		{
			m_combatAI.ActionComplete();

			//forget about the attack data, this attack is complete
			base.EndAttack();
		}
		public override void CarryOutSkill()
		{
			m_combatAI.ActionComplete();
			m_timeTillCanMove = Program.MainUpdateLoopStartTime() + SKILL_COOLDOWN_TIME;
		}
		public override void SkillCancelledNotification()
		{
			m_combatAI.ActionComplete();
		}
		public void ChangeAITemplate(CombatAITemplate newTemplate)
		{
			if (newTemplate != null && newTemplate != m_combatAI.AITemplate)
			{
				m_combatAI.SetNewAITemplate(newTemplate, this);
			}
		}
		/// <summary>
		/// called if a next skill is removed (or not set) due to an entity failing various conditions
		/// mainly used for combat ai
		/// </summary>
		public override void SkillFailedConditions()
		{
			m_combatAI.ActionComplete();
		}
		#endregion

		internal override int getAbilityLevel(ABILITY_TYPE ability_id)
		{
			/*CharacterAbility ability = getAbilityById(ability_id);
			if (ability == null)
				return 0;
			else
			{
				return ability.m_currentLevel;
			}*/
			int level = (int)m_compiledStats.GetAbilityValForId(ability_id);

			if (level < 0)
			{
				level = 0;
			}
			return level;
			// return (int)m_compiledStats.GetAbilityValForId(ability_id);
		}


		internal override CharacterAbility getAbilityById(ABILITY_TYPE ability_id)
		{

			for (int i = 0; i < Template.m_abilities.Count; i++)
			{
				if (Template.m_abilities[i].m_ability_id == ability_id)
				{
					return Template.m_abilities[i];
				}
			}
			return null;
		}
		internal override void AddAbiltiesToStats(CombatEntityStats statsToAddTo)
		{
			CES_AbilityHolder.AddCharacterAbilitiesToList(statsToAddTo.Abilities, Template.m_abilities);
		}

		#region rewards
		internal int getExperienceValue(int playerLevel)
		{
			int diff = playerLevel - Level;
			if (diff < 0)
				diff = 0;
			int xp_reward = (int)Math.Round(Math.Pow(Character.EXPERIENCE_ACCELLERATOR, diff) * m_template.XP);
			//  if(diff>10)
			// xp_reward=0;
			xp_reward = (int)(xp_reward * Program.processor.GlobalEXPMod);
			return xp_reward;
		}

        
		internal int getCoinsDropped()
		{
			if (m_template.m_maxCoins == 0)
				return 0;
			int diff = m_template.m_maxCoins - m_template.m_minCoins;
			int coinsWon = Program.getRandomNumber(diff + 1) + m_template.m_minCoins;
			coinsWon = (int)(coinsWon * Program.processor.GlobalGoldMod);
			return coinsWon;//Program.getRandomNumber(diff+1) + m_template.m_minCoins;
		}
		internal List<LootDetails> getLootDropped()
		{
			List<LootDetails> lootdetails = m_template.getLootDropped();

			return lootdetails;
		}
		/* internal List<LootDetails> getLootDropped()
		 {
			 List<LootDetails> lootdetails = new List<LootDetails>();
			 for (int i = 0; i < m_template.m_num_drops; i++)
			 {
				 LootDetails detail = m_template.getLootItem();

				 if (detail != null)
				 {
					 if (lootdetails.Count > 0)
					 {
						 ItemTemplate itemTemplate = ItemTemplateManager.GetItemForID(detail.m_templateID);
						 if (itemTemplate.m_stackable)
						 {
							 bool found = false;
							 for (int j = 0; j < lootdetails.Count; j++)
							 {
								 if (lootdetails[j].m_templateID == detail.m_templateID)
								 {
									 lootdetails[j].m_quantity += detail.m_quantity;
									 found = true;
									 break;
								 }
							 }
							 if (!found)
							 {
								 lootdetails.Add(detail);
							 }
						 }
						 else
						 {
							 lootdetails.Add(detail);
						 }
					 }
					 else
					 {
						 lootdetails.Add(detail);
					 }

				 }
			 }
			 return lootdetails;
		 }*/
		#endregion //rewards
		#region IEntitySocialStanding Members

		public int GetOpinionFor(IEntitySocialStanding theEntity)
		{
			int factionStanding = theEntity.GetFactionStanding(m_template.m_factionID);

			return m_template.m_opinionBase + factionStanding;
		}

        /// <summary>
        /// Note this is not the faction system ID (reavers, liches etc.). Use factionInfluences
        /// </summary>
        /// <returns></returns>
		public int GetFactionID()
		{
			return m_template.m_factionID;
		}

        /// <summary>
        /// wtf? not the faction system - don't use
        /// </summary>
        /// <param name="factionID"></param>
        /// <returns></returns>
		public int GetFactionStanding(int factionID)
		{
			return 0;
		}

		public bool WithinPartyWith(IEntitySocialStanding theEntity)
		{
			//entities are in their own parties
			if (theEntity == this)
			{
				return true;
			}
			return false;
		}

		#endregion


		override internal double TimeSinceSkillLastCast(SKILL_TYPE skillID)
		{

			MobSkill theSkill = m_skillTable.GetSkillForID(skillID);
			if (theSkill != null && theSkill.TheSkill != null)
			{
				return theSkill.TheSkill.TimeLastCast;
			}

			return 0;
		}
		internal override int GetOpinionOf(CombatEntity otherEntity)
		{
			if (otherEntity.Type == EntityType.Player)
			{
				return Template.m_opinionBase;
			}

			return 100;
		}
		internal override bool IsTargetting(CombatEntity otherEntity)
		{
			if (m_combatAI.MainTarget == otherEntity)
			{
				return true;
			}
			return base.IsTargetting(otherEntity);
		}

	    internal void ForceKill()
	    {
            //CurrentStatusEffects.Clear();
            ResetStatusEffects();
            CurrentSkillTarget = null;
            CurrentSkill = null;
            HostileEntities.Clear();
            InCombat = false;
            NextSkillTarget = null;
            if (AttackTarget != null)
            {
                m_combatManager.StopAttacking(this);
            }
            AttackTarget = null;
            //NextAttackTarget = null;
            NextSkill = null;
            ActionInProgress = false;
            ClearDownAggroLists();
            m_movementAI = m_defaultMovementAI;
            
            if(m_combatAI!=null)
            m_combatAI.ResetCombatAI(this);
	        
            m_combatAI = null;
            m_aiState = SCE_AI_STATE.IDLE;

            CurrentHealth = 0;
            //m_currentPosition.m_position = m_spawnPosition;
            
	    }
	}
}
