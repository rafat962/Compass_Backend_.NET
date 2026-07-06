namespace CompassAI.Models.DTO
{
    public class EmailToOwnerDto
    {
        public string Email { get; set; }
        public string Subject { get; set; } = string.Empty;
         public string Message { get; set; } = string.Empty;
    }
}
