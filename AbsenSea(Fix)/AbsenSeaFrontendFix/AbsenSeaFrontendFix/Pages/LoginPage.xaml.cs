using AbsenSeaFrontendFix.Pages.Database;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AbsenSeaFrontendFix.Pages
{
    public partial class LoginPage : Page
    {
        private Backend _backend;
        private bool _isInitialized = false;

        public LoginPage()
        {
            InitializeComponent();
            this.Loaded += LoginPage_Loaded;
        }

        private async void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    Debug.WriteLine("Initializing Backend...");
                    _backend = await Backend.CreateAsync();
                    _isInitialized = true;
                    Debug.WriteLine("Backend initialized successfully!");

                    // Test database connection
                    bool connectionTest = await _backend.TestConnection();
                    if (connectionTest)
                    {
                        Debug.WriteLine("✓ Database connection test passed!");
                        
                        // Count records to verify access
                        var crewCount = await _backend.GetAllCrewMembers();
                        Debug.WriteLine($"✓ Found {crewCount.Count} crew members in database");
                    }
                    else
                    {
                        Debug.WriteLine("✗ Database connection test failed");
                        MessageBox.Show("Database connected but may have access issues. Check RLS policies.", 
                            "Connection Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during initialization: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Failed to connect to database:\n\n{ex.Message}\n\nCheck your .env file and Supabase credentials.", 
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(ShipIdTextBox.Text))
            {
                MessageBox.Show("Please enter your ID", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ShipIdTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Please enter your password", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            try
            {
                var loginButton = sender as Button;
                if (loginButton != null)
                {
                    loginButton.IsEnabled = false;
                    loginButton.Content = "Signing In...";
                }

                Debug.WriteLine("Attempting login...");

                if (_backend == null || !_isInitialized)
                {
                    _backend = await Backend.CreateAsync();
                    _isInitialized = true;
                }

                var userId = long.Parse(ShipIdTextBox.Text);
                var password = PasswordBox.Password;

                // Try to login as Ship first
                var isShipLogin = await TryShipLogin(userId, password);

                if (!isShipLogin)
                {
                    // If not a ship, try crew member login
                    var isCrewLogin = await TryCrewLogin(userId, password);

                    if (!isCrewLogin)
                    {
                        MessageBox.Show("Invalid ID or password", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        PasswordBox.Clear();
                        PasswordBox.Focus();
                    }
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("ID must be a valid number", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                ShipIdTextBox.SelectAll();
                ShipIdTextBox.Focus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login error: {ex.Message}");
                MessageBox.Show($"Login failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                var loginButton = sender as Button;
                if (loginButton != null)
                {
                    loginButton.IsEnabled = true;
                    loginButton.Content = "Sign In";
                }
            }
        }

        private async Task<bool> TryShipLogin(long shipId, string password)
        {
            try
            {
                var result = await _backend.SupabaseClient
                    .From<ShipAccount>()
                    .Select("*")
                    .Where(x => x.SHIP_ID == shipId)
                    .Single();

                if (result != null && result.SHIP_NAME == password) // Replace with actual password check
                {
                    Debug.WriteLine($"Ship login successful: {result.SHIP_NAME}");

                    // Store ship info
                    Application.Current.Properties["UserType"] = "Ship";
                    Application.Current.Properties["CurrentShipId"] = result.SHIP_ID;
                    Application.Current.Properties["CurrentShipName"] = result.SHIP_NAME;
                    Application.Current.Properties["CurrentUserId"] = result.user_id;
                    Application.Current.Properties["Backend"] = _backend;

                    MessageBox.Show($"Welcome, {result.SHIP_NAME}!", "Login Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Navigate to Ship Dashboard
                    var dashboardPage = new DashboardPage();
                    NavigationService?.Navigate(dashboardPage);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ship login attempt failed: {ex.Message}");
            }

            return false;
        }

        private async Task<bool> TryCrewLogin(long crewId, string password)
        {
            try
            {
                var result = await _backend.SupabaseClient
                    .From<CrewMember>()
                    .Select("*, SHIP_ACCOUNT(*)")
                    .Where(x => x.CREW_ID == crewId)
                    .Single();

                if (result != null && result.CREW_NAME == password) // Replace with actual password check
                {
                    Debug.WriteLine($"Crew login successful: {result.CREW_NAME}");

                    // Store crew info
                    Application.Current.Properties["UserType"] = "Crew";
                    Application.Current.Properties["CurrentCrewId"] = result.CREW_ID;
                    Application.Current.Properties["CurrentCrewName"] = result.CREW_NAME;
                    Application.Current.Properties["CurrentShipId"] = result.ship_id;
                    Application.Current.Properties["CurrentUserId"] = result.user_id;
                    Application.Current.Properties["Backend"] = _backend;

                    MessageBox.Show($"Welcome, {result.CREW_NAME}!", "Login Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Navigate to Crew Dashboard (or different page)
                    var dashboardPage = new DashboardPage();
                    NavigationService?.Navigate(dashboardPage);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Crew login attempt failed: {ex.Message}");
            }

            return false;
        }

        private void RegisterLink_Click(object sender, MouseButtonEventArgs e)
        {
            var registerPage = new RegisterPage();
            NavigationService?.Navigate(registerPage);
        }
    }
}