using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class JoinRoomDTO : BaseModel
    {
        public RoomInfoDTO Room { get; set; }
        public string Password { get; set; }
    }
}
