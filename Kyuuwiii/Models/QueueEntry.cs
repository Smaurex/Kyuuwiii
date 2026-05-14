using SQLite;


namespace Kyuuwiii.Models
{
    [Table("QueueEntries")]
    public class QueueEntry
    {
        [PrimaryKey, AutoIncrement]
        public int entryId { get; set; }

        public int userId { get; set; }
        public int courseId { get; set; }
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public int queuePosition { get; set; }
        public string status { get; set; } = "waiting"; // waiting | serving | done | cancelled
        public DateTime joinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? servedAt { get; set; }

        [Ignore]
        public bool IsActive => status == "waiting" || status == "serving";

        [Ignore]
        public string StatusDisplay => status switch
        {
            "waiting" => "Waiting",
            "serving" => "Now Serving",
            "done" => "Done",
            "cancelled" => "Cancelled",
            _ => status
        };
    }
}
