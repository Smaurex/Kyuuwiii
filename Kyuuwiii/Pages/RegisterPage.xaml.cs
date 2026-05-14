using Kyuuwiii.Models;
using Kyuuwiii.Services;

namespace Kyuuwiii.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        var fullName = FullNameEntry.Text?.Trim() ?? "";
        var studentId = StudentIdEntry.Text?.Trim() ?? "";
        var email = EmailEntry.Text?.Trim() ?? "";
        var course = CoursePicker.SelectedItem?.ToString() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(studentId) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(course) ||
            string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please fill in all fields.");
            return;
        }

        var parts = fullName.Split(' ', 2);
        var firstName = parts[0];
        var lastName = parts.Length > 1 ? parts[1] : "";

        var user = new User
        {
            firstName = firstName,
            lastName = lastName,
            studentId = studentId,
            email = email,
            course = course,
            password = password,
            user_role = "user"
        };

        var success = await DatabaseService.Instance.RegisterUserAsync(user);
        if (!success)
        {
            ShowError("Email or Student ID already exists.");
            return;
        }

        await DisplayAlert("Success", "Account created! You can now log in.", "Continue");
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
