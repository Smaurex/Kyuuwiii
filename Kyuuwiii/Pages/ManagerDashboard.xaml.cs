using Kyuuwiii.Models;
using Kyuuwiii.Services;
using System.Collections.ObjectModel;

namespace Kyuuwiii.Pages;

public class QueueItemViewModel
{
    public string DisplayPosition { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string Reason { get; set; } = "";
    public int EntryId { get; set; }
}

public partial class ManagerDashboard : ContentPage
{
    public ObservableCollection<QueueItemViewModel> QueueItems { get; } = new();

    private List<Course> _courses = new();
    private int _selectedCourseIndex = 0;
    private QueueEntry? _servingEntry;
    private System.Timers.Timer? _timer;

    public ManagerDashboard()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Session.IsLoggedIn) { await Shell.Current.GoToAsync("//LoginPage"); return; }

        AvatarLabel.Text = Session.CurrentUser!.firstName.Length > 0
            ? Session.CurrentUser.firstName[0].ToString().ToUpper() : "M";

        _courses = await DatabaseService.Instance.GetAllCoursesAsync();

        if (Session.CurrentUser.course is string mc)
        {
            var idx = _courses.FindIndex(c => c.courseName == mc);
            if (idx >= 0) _selectedCourseIndex = idx;
        }

        UpdateTabHighlight();
        await LoadQueue();
        StartTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop(); _timer?.Dispose(); _timer = null;
    }

    private void StartTimer()
    {
        _timer = new System.Timers.Timer(8_000);
        _timer.Elapsed += async (s, e) => await LoadQueue();
        _timer.Start();
    }

    private async Task LoadQueue()
    {
        if (_courses.Count == 0) return;
        var course = _courses[_selectedCourseIndex];

        var entries = await DatabaseService.Instance.GetQueueForCourseAsync(course.courseId);
        _servingEntry = entries.FirstOrDefault(e => e.status == "serving");
        var waiting = entries.Where(e => e.status == "waiting").OrderBy(e => e.queuePosition).ToList();

        // fetch all users
        var allUsers = await DatabaseService.Instance.GetAllUsersAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            QueueTitleLabel.Text = $"{course.ShortName} Enrollment Queue";
            QueueLengthLabel.Text = $"{waiting.Count}";

            // serving entries
            if (_servingEntry != null)
            {
                var u = allUsers.FirstOrDefault(x => x.userId == _servingEntry.userId);
                ServingNameLabel.Text = u?.username ?? "Unknown";
                ServingIdLabel.Text = $"ID: {u?.studentId ?? "–"}";
                ServingInitials.Text = u?.firstName.Length > 0 ? u.firstName[0].ToString().ToUpper() : "?";
                ServingTicketLabel.Text = $"ST-{_servingEntry.queuePosition:D3}";
                var elapsed = (DateTime.UtcNow - _servingEntry.joinedAt).TotalMinutes;
                ServingTimeLabel.Text = $"{(int)elapsed} minutes";
            }
            else
            {
                ServingNameLabel.Text = "No one serving";
                ServingIdLabel.Text = "–";
                ServingInitials.Text = "–";
                ServingTicketLabel.Text = "–";
                ServingTimeLabel.Text = "–";
            }

            QueueItems.Clear();
            int displayPos = 2;
            foreach (var entry in waiting)
            {
                var u = allUsers.FirstOrDefault(x => x.userId == entry.userId);
                QueueItems.Add(new QueueItemViewModel
                {
                    DisplayPosition = displayPos.ToString(),
                    StudentName = u?.username ?? "Unknown",
                    StudentId = u?.studentId ?? "–",
                    Reason = $"{u?.course ?? "–"} • {entry.title}",
                    EntryId = entry.entryId
                });
                displayPos++;
            }
        });
    }

    private async void OnCallNextClicked(object sender, EventArgs e)
    {
        if (_courses.Count == 0) return;
        var course = _courses[_selectedCourseIndex];
        bool advanced = await DatabaseService.Instance.CallNextAsync(course.courseId);
        if (!advanced)
            await DisplayAlertAsync("Queue Empty", "No more students waiting in this queue.", "OK");
        await LoadQueue();
    }

    private async void OnMarkDoneClicked(object sender, EventArgs e)
    {
        if (_servingEntry == null) { await DisplayAlertAsync("No Active", "No student is currently being served.", "OK"); return; }
        await DatabaseService.Instance.MarkDoneAsync(_servingEntry.entryId);
        await LoadQueue();
    }

    private async void OnTabTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string param && int.TryParse(param, out int idx))
        {
            _selectedCourseIndex = idx;
            UpdateTabHighlight();
            await LoadQueue();
        }
    }

    // istitik purposes
    private void UpdateTabHighlight()
    {
        
        var tabs = new[] { TabCS, TabIT, TabIS, TabDS };

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isSelected = i == _selectedCourseIndex;
            tabs[i].BackgroundColor = isSelected
                ? Color.FromArgb("#0D1B5E")   
                : Colors.Transparent;

            if (tabs[i].Content is Label lbl)
            {
                lbl.TextColor = isSelected
                    ? Colors.White
                    : Color.FromArgb("#6B7280"); 
            }
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        Session.Logout();
        AppShell.NavigateToLogin();
    }
}