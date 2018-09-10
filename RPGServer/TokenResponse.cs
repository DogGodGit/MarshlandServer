using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainServer
{
    class TokenResponse
    {
        public TokenResponse()
        {
            access_token = "";
            token_type = "";
            expires_in = 0;
            refresh_token = "";
        }
        public string access_token;
        public string token_type;
        public int expires_in;
        public string refresh_token;
    }
}
