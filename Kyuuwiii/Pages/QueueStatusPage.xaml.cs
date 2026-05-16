using Kyuuwiii.Models;
using Kyuuwiii.Services;

namespace Kyuuwiii.Pages;

public partial class QueueStatusPage : ContentPage
{
    private System.Timers.Timer? _timer;
    private QueueEntry? _currentEntry;

    public QueueStatusPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Session.IsLoggedIn)
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        AvatarLabel.Text = Session.CurrentUser!.firstName.Length > 0
            ? Session.CurrentUser.firstName[0].ToString().ToUpper() : "?";

        await RefreshStatus();
        StartTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTimer();
    }

    private void StartTimer()
    {
        _timer = new System.Timers.Timer(10_000);
        _timer.Elapsed += async (s, e) =>
        {
            await RefreshStatus();
        };
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private async Task RefreshStatus()
    {
        if (!Session.IsLoggedIn) return;

        _currentEntry = await DatabaseService.Instance.GetActiveEntryAsync(Session.CurrentUser!.userId);

        if (_currentEntry == null)
        {
            // Done — navigate back to dashboard
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlertAsync("Queue Complete", "You have been served. Thank you!", "OK");
                await Shell.Current.GoToAsync("//StudentDashboard");
            });
            return;
        }

        var queue = await DatabaseService.Instance.GetQueueForCourseAsync(_currentEntry.courseId);
        int ahead = queue.Count(e => e.queuePosition < _currentEntry.queuePosition && e.status == "waiting");
        int totalWaiting = queue.Count;
        int estWaitMinutes = ahead * 6;

        var course = await DatabaseService.Instance.GetCourseAsync(_currentEntry.courseId);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TicketLabel.Text = $"A-{_currentEntry.queuePosition:D2}";
            OfficeName.Text = $"{course?.courseName ?? ""} — Enrollment Office";

            if (_currentEntry.status == "serving")
            {
                PositionLabel.Text = "NOW SERVING";
                WaitTimeLabel.Text = "Your turn!";
                StatusProgress.Progress = 1.0;
                ProgressLabel.Text = "100% Complete";
                StatusLabel.Text = "You are being served now!";
                CheerLabel.Text = "🎉";
            }
            else
            {
                PositionLabel.Text = ahead == 0 ? "Next in line!" : $"{ahead + 1} in line";
                WaitTimeLabel.Text = estWaitMinutes == 0 ? "< 1 min" : $"~{estWaitMinutes} min";

                double progress = totalWaiting > 0
                    ? 1.0 - ((double)ahead / totalWaiting)
                    : 0.5;
                StatusProgress.Progress = Math.Clamp(progress, 0.05, 0.95);

                int pct = (int)(StatusProgress.Progress * 100);
                ProgressLabel.Text = $"{pct}% Complete";
                StatusLabel.Text = "Processing documents…";
                CheerLabel.Text = pct >= 70 ? "Almost there!" : "";
            }

            QueueLengthLabel.Text = $"{totalWaiting} People";
        });
    }

    private async void OnLeaveQueueClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Leave Queue",
            "Are you sure you want to leave the queue? You will lose your spot.", "Yes, Leave", "Cancel");
        if (!confirm) return;

        if (_currentEntry != null)
        {
            await DatabaseService.Instance.CancelEntryAsync(_currentEntry.entryId);
        }

        StopTimer();
        await Shell.Current.GoToAsync("//StudentDashboard");
    }

    private async void OnAvatarTapped(object sender, TappedEventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Log Out", "Are you sure you want to log out?", "Log Out", "Cancel");
        if (!confirm) return;
        Session.Logout();
        AppShell.NavigateToLogin();
    }
}
