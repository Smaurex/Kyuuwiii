using Kyuuwiii.Models;
using Kyuuwiii.Services;

namespace Kyuuwiii.Pages;

// ── Fully separate ViewModel — does NOT inherit User so SQLite never touches it ──
public class UserViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string Course { get; set; } = "";
    public string Role { get; set; } = "";

    // Display helpers
    public string Initials => FullName.Length > 0 ? FullName[0].ToString().ToUpper() : "?";
    public string RoleDisplay => Role.ToUpper();

    // Convert back to a plain User for DB operations
    public User ToUser(string firstName, string lastName, string password) => new()
    {
        userId = UserId,
        firstName = firstName,
        lastName = lastName,
        email = Email,
        studentId = StudentId,
        course = Course,
        password = password,
        user_role = Role
    };
}

public partial class AdminDashboard : ContentPage
{
    // Keep the original User objects around so we can pass them to the DB without losing fields
    private List<User> _rawUsers = new();
    private List<UserViewModel> _allUsers = new();

    public AdminDashboard()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Session.IsLoggedIn) { await Shell.Current.GoToAsync("//LoginPage"); return; }
        await LoadCourses();
        await LoadUsers();
    }

    private async Task LoadCourses()
    {
        var courses = await DatabaseService.Instance.GetAllCoursesAsync();
        CourseCardsContainer.Children.Clear();

        string[] statusLabels = { "Active", "Peak", "Steady", "Fast" };
        string[] statusColors = { "#22C55E", "#F59E0B", "#3B82F6", "#10B981" };
        string[] avgTimes = { "12m", "24m", "8m", "5m" };

        for (int i = 0; i < courses.Count; i++)
        {
            var c = courses[i];
            string status = i < statusLabels.Length ? statusLabels[i] : "Active";
            string color = i < statusColors.Length ? statusColors[i] : "#22C55E";
            string avg = i < avgTimes.Length ? avgTimes[i] : "–";

            var card = new Border
            {
                Style = (Style)Application.Current!.Resources["CardStyle"],
                Padding = new Thickness(20, 16)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(4)),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };

            var bar = new BoxView { Color = Color.FromArgb(color), WidthRequest = 4, CornerRadius = 2 };
            Grid.SetColumn(bar, 0);

            var info = new VerticalStackLayout { Margin = new Thickness(12, 0, 0, 0) };
            info.Add(new Label { Text = c.courseName.ToUpper(), FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 0.5, TextColor = Color.FromArgb(color) });
            info.Add(new Label { Text = $"{c.ShortName} Queue", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D1B5E") });

            var stats = new HorizontalStackLayout { Spacing = 24, Margin = new Thickness(0, 8, 0, 8) };
            var waitStack = new VerticalStackLayout();
            waitStack.Add(new Label { Text = "WAITING", FontSize = 10, TextColor = Color.FromArgb("#6B7280") });
            waitStack.Add(new Label { Text = c.queueLength.ToString(), FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D1B5E") });
            var avgStack = new VerticalStackLayout();
            avgStack.Add(new Label { Text = "AVG. TIME", FontSize = 10, TextColor = Color.FromArgb("#6B7280") });
            avgStack.Add(new Label { Text = avg, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1A7A5C") });
            stats.Add(waitStack);
            stats.Add(avgStack);
            info.Add(stats);
            info.Add(new Label { Text = $"👤 Coordinator: {c.courseCardCoordinator}", FontSize = 12, TextColor = Color.FromArgb("#6B7280") });

            var badge = new Border
            {
                BackgroundColor = Color.FromArgb(color),
                StrokeThickness = 0,
                Padding = new Thickness(10, 4),
                VerticalOptions = LayoutOptions.Start
            };
            badge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(20) };
            badge.Content = new Label { Text = status, TextColor = Colors.White, FontSize = 11, FontAttributes = FontAttributes.Bold };

            Grid.SetColumn(info, 1);
            Grid.SetColumn(badge, 2);
            grid.Add(bar); grid.Add(info); grid.Add(badge);
            card.Content = grid;
            CourseCardsContainer.Children.Add(card);
        }
    }

    private async Task LoadUsers(string filter = "")
    {
        // Always re-fetch raw User objects from DB
        _rawUsers = await DatabaseService.Instance.GetAllUsersAsync();

        _allUsers = _rawUsers.Select(u => new UserViewModel
        {
            UserId = u.userId,
            FullName = u.username,
            Email = u.email,
            StudentId = u.studentId,
            Course = u.course,
            Role = u.user_role
        }).ToList();

        ApplyFilter(filter);
    }

    private void ApplyFilter(string filter)
    {
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allUsers
            : _allUsers.Where(u =>
                u.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                u.StudentId.Contains(filter, StringComparison.OrdinalIgnoreCase)
              ).ToList();

        // Null then reassign forces CollectionView to fully re-render
        UserList.ItemsSource = null;
        UserList.ItemsSource = filtered;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(e.NewTextValue ?? "");
    }

    private async void OnUserSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not UserViewModel vm) return;
        ((CollectionView)sender).SelectedItem = null;

        // Find the original plain User object — never pass UserViewModel to SQLite
        var original = _rawUsers.FirstOrDefault(u => u.userId == vm.UserId);
        if (original == null) return;

        string action = await DisplayActionSheetAsync(
            $"Manage: {vm.FullName}", "Cancel", "Delete User",
            "Set as Student", "Set as Manager", "Set as Admin");

        switch (action)
        {
            case "Set as Student":
                original.user_role = "user";
                await DatabaseService.Instance.UpdateUserAsync(original);
                break;

            case "Set as Manager":
                original.user_role = "manager";
                await DatabaseService.Instance.UpdateUserAsync(original);
                break;

            case "Set as Admin":
                original.user_role = "admin";
                await DatabaseService.Instance.UpdateUserAsync(original);
                break;

            case "Delete User":
                bool confirm = await DisplayAlertAsync("Delete", $"Delete {vm.FullName}?", "Delete", "Cancel");
                if (!confirm) return;
                await DatabaseService.Instance.DeleteUserAsync(original);
                break;

            default:
                return; // Cancel or dismissed — don't reload
        }

        // Reload from DB to reflect real saved state
        await LoadUsers(SearchEntry.Text ?? "");
    }

    private async void OnAddUserClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("RegisterPage");
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Session.Logout();
        AppShell.NavigateToLogin();
    }
}
