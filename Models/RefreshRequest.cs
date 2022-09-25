using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI_JWT_Refresh_Token.Models
    {
    public class RefreshRequest
        {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        }
    }
