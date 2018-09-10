using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainServer
{
    enum PERMENENT_BUFF_ID
    {
        NONE =0,
        BACKPACK=13,
        HEALTH_REGEN_1=8,
        ENERGY_REGEN_1=9,
        EXTRA_HUD_SLOT=19,
        SOLO_BANK_EXPANSION=92,
        AUCTION_HOUSE_SLOT_EXPANSION=93
    }

    class PermanentBuff
    {
        PERMENENT_BUFF_ID m_buffID = 0;
        int m_buffQuantity=0;

        public PERMENENT_BUFF_ID BuffID
        {
            get { return m_buffID; }
        }
        public int BuffQuantity
        {
            set { m_buffQuantity = value; }
            get { return m_buffQuantity; }
        }

        internal PermanentBuff(PERMENENT_BUFF_ID buffID, int buffQuantity)
        {
            m_buffID = buffID;
            m_buffQuantity = buffQuantity;
        }
    }
}
