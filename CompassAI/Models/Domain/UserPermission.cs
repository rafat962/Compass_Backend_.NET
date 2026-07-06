namespace CompassAI.Models.Domain
{
    public class UserPermission
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // مثلاً: Asset, Users, Maps
        public string Resource { get; set; } = null!;

        // المسار في الـ React مثلاً: /assets/list
        public string Route { get; set; } = null!;

        // المسار البرمجي (اختياري للـ Breadcrumbs)
        public string RouteName { get; set; } = null!;

        // الأفعال مخزنة كـ String مفصول بفاصلة: "ADD,DELETE,VIEW"
        public string Actions { get; set; } = null!;

        // Navigation property
        public User User { get; set; } = null!;
    }
}