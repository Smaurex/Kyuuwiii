using SQLite;


namespace Kyuuwiii.Models
{
    [Table("Users")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int userId { get; set; }

        public string firstName { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public string studentId { get; set; } = string.Empty;

        [Unique]
        public string email { get; set; } = string.Empty;

        public string password { get; set; } = string.Empty;
        public string course { get; set; } = string.Empty;
        public string user_role { get; set; } = "user"; // user | manager | admin

        [Ignore]
        public string username => $"{firstName} {lastName}";

        [Ignore]
        public bool IsStudent => user_role == "user";

        [Ignore]
        public bool IsManager => user_role == "manager";

        [Ignore]
        public bool IsAdmin => user_role == "admin";
    }
}
