namespace CompassAI.Models.DTO
{
    public class UpdateUserDto
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Role { get; set; } = "user";

        public string CurrentPlan { get; set; } = "Free";
        public bool? Active { get; set; }
    }
}
