using SQLite;
using Kyuuwiii.Models;

namespace Kyuuwiii.Services
{
    public class DatabaseService
    {
        private static DatabaseService? _instance;
        public static DatabaseService Instance => _instance ??= new DatabaseService();

        private SQLiteAsyncConnection? _db;

        private async Task<SQLiteAsyncConnection> GetDb()
        {
            if (_db == null)
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "kyuuwiii.db3");
                _db = new SQLiteAsyncConnection(path,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            }
            return _db;
        }

        public async Task InitAsync()
        {
            var db = await GetDb();
            await db.CreateTableAsync<User>();
            await db.CreateTableAsync<Course>();
            await db.CreateTableAsync<QueueEntry>();
            await SeedCoursesAsync(db);
            await SeedAdminAsync(db);
        }

        private async Task SeedCoursesAsync(SQLiteAsyncConnection db)
        {
            var count = await db.Table<Course>().CountAsync();
            if (count > 0) return;

            var courses = new List<Course>
            {
                // replace them with dcism default 
                new() { courseName = "Computer Science",      queueManager = "CISCO OFFICER 1",   courseCardCoordinator = "Mr.Archival Sebial" },
                new() { courseName = "Information Technology",queueManager = "CISCO OFFICER 2",courseCardCoordinator = "Mr. Glenn Pepito" },
                new() { courseName = "Data Science",          queueManager = "CISCO OFFICER 3",  courseCardCoordinator = "Dr. Katrina Fuentes" },
                new() { courseName = "Information Systems",   queueManager = "CISCO OFFICER 4",        courseCardCoordinator = "Engr. Christian Maderazo" },
            };
            await db.InsertAllAsync(courses);
        }

        private async Task SeedAdminAsync(SQLiteAsyncConnection db)
        {
            var admin = await db.Table<User>().Where(u => u.user_role == "admin").FirstOrDefaultAsync();
            if (admin != null) return;
            // admin account
            await db.InsertAsync(new User
            {
                firstName = "Admin",
                lastName = "Kyuuwiii",
                studentId = "0000-00000",
                email = "admin@kyuuwiii.edu",
                password = "admin123",
                course = "Computer Science",
                user_role = "admin"
            });
        }

        // USER METHODS vvv 

        public async Task<bool> RegisterUserAsync(User user)
        {
            var db = await GetDb();
            var existing = await db.Table<User>()
                .Where(u => u.email == user.email || u.studentId == user.studentId)
                .FirstOrDefaultAsync();
            if (existing != null) return false;
            await db.InsertAsync(user);
            return true;
        }

        public async Task<User?> LoginAsync(string emailOrId, string password)
        {
            var db = await GetDb();
            return await db.Table<User>()
                .Where(u => (u.email == emailOrId || u.studentId == emailOrId) && u.password == password)
                .FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var db = await GetDb();
            return await db.Table<User>().ToListAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            var db = await GetDb();
            await db.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(User user)
        {
            var db = await GetDb();
            await db.DeleteAsync(user);
        }

        // METHODS FOR THE COURSES vvv

        public async Task<List<Course>> GetAllCoursesAsync()
        {
            var db = await GetDb();
            return await db.Table<Course>().ToListAsync();
        }

        public async Task<Course?> GetCourseAsync(int courseId)
        {
            var db = await GetDb();
            return await db.Table<Course>().Where(c => c.courseId == courseId).FirstOrDefaultAsync();
        }

        public async Task UpdateCourseAsync(Course course)
        {
            var db = await GetDb();
            await db.UpdateAsync(course);
        }

        // METHODS FOR THE QUEUES vvv

        public async Task<(bool success, string message)> JoinQueueAsync(int userId, int courseId, string title, string description)
        {
            var db = await GetDb();

            // code to avoid joining queue multiple times
            var active = await GetActiveEntryAsync(userId);
            if (active != null) return (false, "You already have an active queue entry.");

            // count waivers 
            var waiters = await db.Table<QueueEntry>()
                .Where(e => e.courseId == courseId && (e.status == "waiting" || e.status == "serving"))
                .CountAsync();

            var entry = new QueueEntry
            {
                userId = userId,
                courseId = courseId,
                title = title,
                description = description,
                queuePosition = waiters + 1,
                status = "waiting",
                joinedAt = DateTime.UtcNow
            };
            await db.InsertAsync(entry);

            // iupdate ang length ng queue sa course
            var course = await GetCourseAsync(courseId);
            if (course != null)
            {
                course.queueLength++;
                await db.UpdateAsync(course);
            }

            return (true, "Successfully joined the queue.");
        }

        public async Task<QueueEntry?> GetActiveEntryAsync(int userId)
        {
            var db = await GetDb();
            return await db.Table<QueueEntry>()
                .Where(e => e.userId == userId && (e.status == "waiting" || e.status == "serving"))
                .FirstOrDefaultAsync();
        }

        public async Task<List<QueueEntry>> GetQueueForCourseAsync(int courseId)
        {
            var db = await GetDb();
            return await db.Table<QueueEntry>()
                .Where(e => e.courseId == courseId && (e.status == "waiting" || e.status == "serving"))
                .OrderBy(e => e.queuePosition)
                .ToListAsync();
        }

        public async Task<int> GetPositionAheadAsync(int userId, int courseId, int myPosition)
        {
            var db = await GetDb();
            return await db.Table<QueueEntry>()
                .Where(e => e.courseId == courseId && e.status == "waiting" && e.queuePosition < myPosition)
                .CountAsync();
        }

        public async Task<bool> CallNextAsync(int courseId)
        {
            var db = await GetDb();
            // reccom na code daw for serving done first
            var currentServing = await db.Table<QueueEntry>()
                .Where(e => e.courseId == courseId && e.status == "serving")
                .FirstOrDefaultAsync();

            if (currentServing != null)
            {
                currentServing.status = "done";
                currentServing.servedAt = DateTime.UtcNow;
                await db.UpdateAsync(currentServing);

                var c = await GetCourseAsync(courseId);
                if (c != null) { c.queueLength = Math.Max(0, c.queueLength - 1); await db.UpdateAsync(c); }
            }

            // Promote next waiter
            var next = await db.Table<QueueEntry>()
                .Where(e => e.courseId == courseId && e.status == "waiting")
                .OrderBy(e => e.queuePosition)
                .FirstOrDefaultAsync();

            if (next == null) return false;

            next.status = "serving";
            await db.UpdateAsync(next);

            var course = await GetCourseAsync(courseId);
            if (course != null)
            {
                course.servingNumber = next.queuePosition;
                await db.UpdateAsync(course);
            }
            return true;
        }

        public async Task MarkDoneAsync(int entryId)
        {
            var db = await GetDb();
            var entry = await db.Table<QueueEntry>().Where(e => e.entryId == entryId).FirstOrDefaultAsync();
            if (entry == null) return;

            entry.status = "done";
            entry.servedAt = DateTime.UtcNow;
            await db.UpdateAsync(entry);

            var course = await GetCourseAsync(entry.courseId);
            if (course != null)
            {
                course.queueLength = Math.Max(0, course.queueLength - 1);
                await db.UpdateAsync(course);
            }
        }

        public async Task CancelEntryAsync(int entryId)
        {
            var db = await GetDb();
            var entry = await db.Table<QueueEntry>().Where(e => e.entryId == entryId).FirstOrDefaultAsync();
            if (entry == null) return;

            entry.status = "cancelled";
            await db.UpdateAsync(entry);

            var course = await GetCourseAsync(entry.courseId);
            if (course != null)
            {
                course.queueLength = Math.Max(0, course.queueLength - 1);
                await db.UpdateAsync(course);
            }
        }

        public async Task<List<QueueEntry>> GetAllEntriesAsync()
        {
            var db = await GetDb();
            return await db.Table<QueueEntry>().OrderByDescending(e => e.joinedAt).ToListAsync();
        }
    }
}
