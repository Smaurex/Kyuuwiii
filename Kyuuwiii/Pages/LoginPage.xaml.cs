using Kyuuwiii.Models;
using Kyuuwiii.Services;

namespace Kyuuwiii.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var emailOrId = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(emailOrId) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please fill in all fields.");
            return;
        }

        var user = await DatabaseService.Instance.LoginAsync(emailOrId, password);
        if (user == null)
        {
            ShowError("Invalid credentials. Please try again.");
            return;
        }

        Session.Login(user);

        if (Session.IsAdmin)
        {
            await Shell.Current.GoToAsync("//AdminDashboard");
        }
        else if (Session.IsManager)
        {
            await Shell.Current.GoToAsync("//ManagerDashboard");
        }
        else
        {
            var active = await DatabaseService.Instance.GetActiveEntryAsync(user.userId);
            if (active != null)
                await Shell.Current.GoToAsync("//QueueStatusPage");
            else
                await Shell.Current.GoToAsync("//StudentDashboard");
        }
    }

    private async void OnSignUpTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("RegisterPage");
    }

    private void OnForgotTapped(object sender, EventArgs e)
    {
        DisplayAlert("Forgot Password", "Please contact your department administrator to reset your password.", "OK");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
