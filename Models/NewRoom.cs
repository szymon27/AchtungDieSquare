namespace Models
{
    public class NewRoom : BaseModel
    {
        public string Name { get; set; }
        public bool Private { get; set; }
        public string Password { get; set; }
        public int Games { get; set; }
        public int MaxPlayers { get; set; }
        public int Owner { get; set; }
    }
}
