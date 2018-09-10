using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    interface ITargetOwner
    {
        
        //resign ownership
        bool ResignOwnership(CombatEntity theEntity);
        //take ownership
        bool TakeOwnership(CombatEntity theEntity);
        //is the owner of an entity
        bool HasOwnership(CombatEntity theEntity);
        /// <summary>
        /// Send Any client notification required when a lock is made
        /// </summary>
        /// <param name="theEntity">the entity that this has just become the owner for</param>
        void NotifyOwnershipTaken(CombatEntity theEntity);
        //get a list of the characters 
        List<Character> GetCharacters
        {
            get;
        }
        List<CombatEntity> GetCurrentLocks
        {
            get;
        }
    }
}
