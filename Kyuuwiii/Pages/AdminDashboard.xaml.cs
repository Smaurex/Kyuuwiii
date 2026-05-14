using Kyuuwiii.Models;
using Kyuuwiii.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Kyuuwiii.Pages;

public class UserViewModel : User
{
    public string Initials => (firstName.Length > 0 ? firstName[0].ToString() : "?").ToUpper();
    public string RoleDisplay => user_role.ToUpper();
}

public partial class AdminDashboard : ContentPage
{
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
            int idx = i;
            string status = idx < statusLabels.Length ? statusLabels[idx] : "Active";
            string color = idx < statusColors.Length ? statusColors[idx] : "#22C55E";
            string avg = idx < avgTimes.Length ? avgTimes[idx] : "–";

            var card = new Border
            {
                Style = (Style)Application.Current!.Resources["CardStyle"],
                Padding = new Thickness(20, 16)
            };

            // Left accent bar
            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(4)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
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
            stats.Add(waitStack); stats.Add(avgStack);
            info.Add(stats);
            info.Add(new Label { Text = $"👤 Coordinator: {c.courseCardCoordinator}", FontSize = 12, TextColor = Color.FromArgb("#6B7280") });

            var badge = new Border
            {
                BackgroundColor = Color.FromArgb(color),
                StrokeThickness = 0,
                Padding = new Thickness(10, 4),
                VerticalOptions = LayoutOptions.Start,
                StrokeShape = new Rectangle { RadiusX = 20, RadiusY = 20 }
            };
            badge.StrokeShape = new Rectangle { RadiusX = 20, RadiusY = 20 };
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
        var users = await DatabaseService.Instance.GetAllUsersAsync();
        _allUsers = users.Select(u => new UserViewModel
        {
            userId = u.userId,
            firstName = u.firstName,
            lastName = u.lastName,
            studentId = u.studentId,
            email = u.email,
            password = u.password,
            course = u.course,
            user_role = u.user_role
        }).ToList();

        ApplyFilter(filter);
    }

    private void ApplyFilter(string filter)
    {
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allUsers
            : _allUsers.Where(u =>
                u.username.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                u.email.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                u.studentId.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        UserList.ItemsSource = filtered;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(e.NewTextValue ?? "");
    }

    private async void OnUserSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not UserViewModel user) return;
        ((CollectionView)sender).SelectedItem = null;

        string action = await DisplayActionSheet(
            $"Manage: {user.username}", "Cancel", "Delete User",
            "Set as Student", "Set as Manager", "Set as Admin");

        switch (action)
        {
            case "Set as Student": user.user_role = "user"; await DatabaseService.Instance.UpdateUserAsync(user); break;
            case "Set as Manager": user.user_role = "manager"; await DatabaseService.Instance.UpdateUserAsync(user); break;
            case "Set as Admin": user.user_role = "admin"; await DatabaseService.Instance.UpdateUserAsync(user); break;
            case "Delete User":
                bool confirm = await DisplayAlert("Delete", $"Delete {user.username}?", "Delete", "Cancel");
                if (confirm) await DatabaseService.Instance.DeleteUserAsync(user);
                break;
        }

        await LoadUsers(SearchEntry.Text ?? "");
    }

    private async void OnAddUserClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("RegisterPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        Session.Logout();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
