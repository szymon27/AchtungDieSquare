namespace Models
{
    public class PlayerDTO : BaseModel
    {
        public ClientInfo ClientInfo { get; set; }
        public Color Color { get; set; }
    }
}
