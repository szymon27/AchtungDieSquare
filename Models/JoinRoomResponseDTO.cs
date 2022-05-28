using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class JoinRoomResponseDTO : BaseModel
    {
        public bool Success { get; set; }
        public string Content { get; set; }
    }
}
