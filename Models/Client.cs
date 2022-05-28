using System.Net.Sockets;

namespace Models
{
    public class Client : BaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? RoomId { get; set; }
        public TcpClient Connection { get; set; }
    }
}
