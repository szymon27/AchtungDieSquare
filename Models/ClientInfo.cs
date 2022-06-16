namespace Models
{
    public class ClientInfo : BaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? RoomId { get; set; }
    }
}
