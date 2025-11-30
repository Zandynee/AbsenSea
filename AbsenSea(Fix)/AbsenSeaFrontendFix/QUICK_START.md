# AbsenSea - Quick Start Guide

## 🚀 Getting Started

### Running the Application

1. **Open the project in Visual Studio:**
   - Double-click `AbsenSeaFrontendFix.sln`
   - Or from command line: `start AbsenSeaFrontendFix.sln`

2. **Build and Run:**
   - Press `F5` in Visual Studio
   - Or from command line: `dotnet run`

3. **Navigate through the app:**
   - Start at **Login Page**
   - Click "Register" to see the registration form
   - Login (any credentials work for demo)
   - Explore **Dashboard**, **Attendance**, and **Reports**

## 📋 Page Overview

### 1. Login Page
- **Purpose:** Ship account authentication
- **Demo:** Click "Sign In" with any input to proceed
- **Navigation:** Click "Register" link to go to registration

### 2. Register Page  
- **Purpose:** Create new ship account
- **Fields:** Ship Name, Ship ID, Email, Password, Confirm Password
- **Demo:** Fill in fields and click "Create Account"
- **Navigation:** Click "Sign in" to return to login

### 3. Dashboard
- **Features:**
  - 4 stat cards: Total Crew, Checked In Today, Compliant, Non-Compliant
  - 7-day attendance trend bar chart
  - Equipment compliance breakdown with progress bars
  - Recent activity feed (last 4 check-ins)
- **Navigation:** Use top menu buttons to switch pages

### 4. Attendance Page
- **Two-Step Process:**
  1. **Enter Crew ID:** Type any ID → Click "Verify ID"
  2. **Camera Detection:** Click "Start Camera" → Click "Capture & Analyze"
- **Features:**
  - Shows crew info after ID verification
  - Simulated camera feed placeholder
  - Detection status overlay
  - Today's summary sidebar
  - Recent check-ins list
- **Demo:** 
  - Type "CR-001" → Verify ID
  - Click "Capture & Analyze" to simulate attendance recording

### 5. Attendance Report Page
- **Features:**
  - Filterable data grid with attendance records
  - 10 sample crew records with equipment status
  - Color-coded compliance indicators (Green = Compliant, Red = Non-Compliant)
  - Date range, status, and department filters
  - Export to PDF button
- **Data Columns:**
  - Crew ID, Name, Position
  - Check-in Time
  - Helmet Status (✓/✗)
  - Vest Status (✓/✗)
  - Overall Status
  - View Details action

## 🎨 Design Features

### Color Coding
- **Green (#10B981):** Compliant, Success
- **Red (#EF4444):** Non-compliant, Warning
- **Blue (#2563EB):** Primary actions, Active state
- **Gray (#6B7280):** Secondary text, Neutral info

### UI Components
- **Modern Cards:** White backgrounds with subtle shadows
- **Rounded Corners:** 8-12px for friendly appearance
- **Hover Effects:** All buttons respond to mouse hover
- **Status Badges:** Color-coded pills for quick recognition
- **Progress Bars:** Visual equipment compliance metrics

## 🔧 Customization Points

### To Connect Backend:

1. **Authentication (LoginPage.xaml.cs):**
   ```csharp
   private void LoginButton_Click(object sender, RoutedEventArgs e)
   {
       // TODO: Replace with actual authentication
       string shipId = ShipIdTextBox.Text;
       string password = PasswordBox.Password;
       // Call your API here
   }
   ```

2. **Crew Verification (AttendancePage.xaml.cs):**
   ```csharp
   private void VerifyId_Click(object sender, RoutedEventArgs e)
   {
       // TODO: Query database for crew member
       string crewId = CrewIdTextBox.Text;
       // Fetch crew details from API
   }
   ```

3. **Camera Integration (AttendancePage.xaml.cs):**
   ```csharp
   private void StartCamera_Click(object sender, RoutedEventArgs e)
   {
       // TODO: Initialize webcam
       // Use libraries like OpenCVSharp or AForge.NET
   }
   ```

4. **AI Detection (AttendancePage.xaml.cs):**
   ```csharp
   private void CaptureAnalyze_Click(object sender, RoutedEventArgs e)
   {
       // TODO: Send image to AI service
       // Process helmet and vest detection
       // Save attendance record
   }
   ```

5. **Report Data (AttendanceReportPage.xaml.cs):**
   ```csharp
   private void LoadSampleData()
   {
       // TODO: Replace with database query
       // Fetch from your attendance table
       var records = attendanceService.GetRecords(dateRange, filters);
       AttendanceDataGrid.ItemsSource = records;
   }
   ```

## 📊 Sample Data

The app includes mock data for demonstration:
- **Dashboard:** 142 total crew, 128 checked in, 115 compliant
- **Reports:** 10 sample crew records with varying compliance
- **Recent Activity:** 4 recent check-ins with timestamps

## 🎯 Next Steps for Production

1. **Database Setup:**
   - Ship accounts table
   - Crew members table
   - Attendance records table
   - Equipment detection logs

2. **API Integration:**
   - RESTful API or GraphQL
   - Authentication endpoints
   - CRUD operations for attendance

3. **AI Service:**
   - Helmet detection model
   - Vest detection model
   - Image preprocessing pipeline

4. **Camera Integration:**
   - Webcam access
   - Image capture
   - Real-time preview

5. **Reports & Export:**
   - PDF generation library
   - Excel export
   - Email notifications

## 💡 Tips

- **Navigation:** All pages have consistent top navigation bar
- **Status Colors:** Green = Good, Red = Alert, Blue = Active
- **Logout:** Available on all main pages (Dashboard, Attendance, Reports)
- **Responsive:** UI adapts to window resizing

## 🐛 Troubleshooting

**Build Errors:**
```powershell
dotnet clean
dotnet build
```

**Navigation Issues:**
- All pages use NavigationService for page transitions
- MainWindow hosts a Frame that loads pages

**Styling Issues:**
- Common styles defined in Page.Resources
- App.xaml contains global color palette

## 📝 Notes

- This is a **UI/UX prototype** - backend integration required
- All button clicks show MessageBox dialogs for demonstration
- Sample data is hardcoded for visual design purposes
- Camera feed is a placeholder - integrate actual camera library

---

**Built for maritime safety and crew accountability** ⚓
