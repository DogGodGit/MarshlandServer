using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MainServer.Factions
{

    public class FactionTemplate
    {               

        /// <summary>
        /// Helper, links a faction title to a level based on points
        /// </summary>
        public struct Level
        {
            public int level;
            public int points;
            public string title;

            public Level(int level, int points, string title)
            {
                this.level = level;
                this.points = points;
                this.title = title;                
            }
        }

        public string Name { get; private set; }
        public int Id { get; private set; }        
        public List<Level> Levels { get; private set; }

        //min & max faction points
        public int Max { get; private set; }
        public  int Min { get; private set; }

        public FactionTemplate(int id, string name)
        {
            this.Id = id;
            this.Name = name;
            Levels = new List<Level>();
        }

        internal void AddLevelInformation(int factionLevel, int factionPoints, string factionTitle)
        {
            //if there is no title, this will either be the min or max levels for this faction
            if (String.IsNullOrEmpty(factionTitle))
            {                
                if (factionPoints < 0)
                    Min = factionPoints;
                else
                    Max = factionPoints;
                
                return;
            }

            //normal faction level information
            Level level = new Level(factionLevel,factionPoints, factionTitle);
            Levels.Add(level);

            //keep them ordered as we add
            Levels = Levels.OrderBy(x => x.level).ToList();
        }


        public Level GetLevel(int factionPoints)
        {            
            // if negative...go from lowest to highest 
            // and look for first number we're lower than
            // e.g. -450  -300  -100  0
            //                 ^
            // value is      -122
            if (factionPoints < 0)
            {
                for(int i =0; i < Levels.Count; i++)
                {
                    if (Levels[i].points < 0 && factionPoints <= Levels[i].points)
                        return Levels[i];
                }
            }
            
            //if positive...go from highest to lowest
            if (factionPoints >= 0)
            {
                for (int i = Levels.Count-1; i > 0; i--)
                {
                    if (Levels[i].points >= 0 && factionPoints >= Levels[i].points)
                        return Levels[i];
                }
            }

            // we shouldn't get there
            // display an error

            return new Level();
        }
    }
}
