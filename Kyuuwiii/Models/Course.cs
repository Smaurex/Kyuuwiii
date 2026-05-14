using SQLite;


namespace Kyuuwiii.Models
{
    [Table("Courses")]
    public class Course
    {
        [PrimaryKey, AutoIncrement]
        public int courseId { get; set; }

        public string courseName { get; set; } = string.Empty;
        public int servingNumber { get; set; } = 0;
        public int queueLength { get; set; } = 0;
        public string queueManager { get; set; } = string.Empty;
        public string courseCardCoordinator { get; set; } = string.Empty;

        [Ignore]
        public string ShortName => courseName switch
        {
            "Computer Science" => "CS",
            "Information Technology" => "IT",
            "Data Science" => "DS",
            "Information Systems" => "IS",
            _ => courseName[..2].ToUpper()
        };
    }
}
