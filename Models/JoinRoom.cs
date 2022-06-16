namespace Models
{
    public class JoinRoom : BaseModel
    {
        public RoomInfo Room { get; set; }
        public string Password { get; set; }
    }
}
