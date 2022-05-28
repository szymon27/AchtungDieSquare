namespace Models
{
    public class RoomInfoDTO : BaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Private { get; set; }
        public int Games { get; set; }
        public int MaxPlayers { get; set; }
        public int Players { get; set; }
    }
}
