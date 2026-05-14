using Kyuuwiii.Models;
using Kyuuwiii.Services;

namespace Kyuuwiii.Pages;

public partial class StudentDashboard : ContentPage
{
    public StudentDashboard()
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
            ? Session.CurrentUser.firstName[0].ToString().ToUpper()
            : "?";

        await LoadOfficeStatus();
    }

    private async Task LoadOfficeStatus()
    {
        var courses = await DatabaseService.Instance.GetAllCoursesAsync();
        int total = courses.Sum(c => c.queueLength);
        TotalWaitingLabel.Text = $"{total} People";
        AvgTimeLabel.Text = "6 Minutes"; // static estimate; replace with real calc if desired
    }

    private async void OnJoinQueueClicked(object sender, EventArgs e)
    {
        var title = TitleEntry.Text?.Trim() ?? "";
        var description = DescriptionEditor.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowError("Please enter a queue title.");
            return;
        }

        // Resolve courseId from user's enrolled course
        var courses = await DatabaseService.Instance.GetAllCoursesAsync();
        var userCourse = courses.FirstOrDefault(c => c.courseName == Session.CurrentUser!.course);

        if (userCourse == null)
        {
            ShowError("Your enrolled course was not found. Contact admin.");
            return;
        }

        var (success, message) = await DatabaseService.Instance.JoinQueueAsync(
            Session.CurrentUser!.userId, userCourse.courseId, title, description);

        if (!success)
        {
            ShowError(message);
            return;
        }

        await Shell.Current.GoToAsync("//QueueStatusPage");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
