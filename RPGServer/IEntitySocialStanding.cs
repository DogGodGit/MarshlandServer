using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    interface IEntitySocialStanding
    {

        int GetOpinionFor(IEntitySocialStanding theEntity);

        int GetFactionID();

        int GetFactionStanding(int factionID);

        bool WithinPartyWith(IEntitySocialStanding theEntity);

       

       
        

    }
}
