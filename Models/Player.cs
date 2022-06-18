namespace Models
{
    public class Player : BaseModel
    {
        public Client Client { get; set; }
        public Color Color { get; set; }
    }
}
