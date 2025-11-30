# AbsenSea - Ship Crew Attendance System

A WPF desktop application for monitoring and recording ship crew attendance with AI-powered safety equipment detection.

## Overview

AbsenSea is designed for ship owners to monitor crew member attendance and ensure compliance with safety equipment requirements. The system uses AI to detect whether crew members are wearing required safety helmets and vests during check-in.

## Features

### 1. **Login Page**
- Ship account authentication
- Modern, professional UI with branding
- "Remember me" functionality
- Password recovery option
- Link to registration page

### 2. **Registration Page**
- New ship account creation
- Fields: Ship Name, Ship ID, Email, Password
- Terms of service acceptance
- Validation for all inputs

### 3. **Dashboard**
- **Key Statistics:**
  - Total crew members
  - Daily check-ins
  - Compliance rate
  - Non-compliant crew count
  
- **Visual Analytics:**
  - 7-day attendance trend chart
  - Equipment compliance breakdown (Helmet, Vest, Both)
  - Real-time activity feed
  
- **Quick Insights:**
  - Percentage comparisons
  - Alert notifications for non-compliance

### 4. **Attendance Page**
- **Two-Step Process:**
  1. Crew ID input and verification
  2. Camera-based equipment detection
  
- **Features:**
  - Live camera feed preview
  - AI detection status overlay
  - Real-time equipment verification (Helmet & Vest)
  - Instant feedback on compliance
  - Recent check-ins sidebar
  - Daily summary statistics

### 5. **Attendance Report Page**
- **Comprehensive Reporting:**
  - Detailed attendance records table
  - Date range filtering
  - Compliance status filtering
  - Department filtering
  - Export to PDF functionality
  
- **Data Display:**
  - Crew ID, Name, Position
  - Check-in time
  - Individual equipment status (color-coded)
  - Overall compliance status
  - Quick action buttons

## User Flow

```
Login → Dashboard → Attendance Check → Report Review
  ↓
Register (for new ships)
```

## Technical Details

### Technology Stack
- **Framework:** .NET 8.0 with WPF
- **UI/UX:** Modern Material Design-inspired interface
- **Language:** C# and XAML

### Project Structure
```
AbsenSeaFrontendFix/
├── Pages/
│   ├── LoginPage.xaml / .cs
│   ├── RegisterPage.xaml / .cs
│   ├── DashboardPage.xaml / .cs
│   ├── AttendancePage.xaml / .cs
│   └── AttendanceReportPage.xaml / .cs
├── MainWindow.xaml / .cs
├── App.xaml / .cs
└── README.md
```

### Design System

**Color Palette:**
- Primary Blue: `#2563EB`
- Navy Blue: `#1E3A8A`
- Success Green: `#10B981`
- Danger Red: `#EF4444`
- Gray Scale: From `#F9FAFB` to `#111827`

**Key UI Components:**
- Modern rounded corners (8-12px border radius)
- Soft shadows for depth
- Responsive buttons with hover effects
- Clean typography with proper hierarchy
- Color-coded status indicators

## Features to Implement (Backend Integration)

The current implementation provides a complete UI/UX foundation. To make it fully functional, implement:

1. **Authentication System**
   - User login validation
   - Session management
   - Password encryption

2. **Database Integration**
   - Crew member database
   - Attendance records storage
   - Ship account management

3. **AI Integration**
   - Camera feed capture
   - Safety equipment detection (Helmet & Vest)
   - Real-time image processing

4. **Reporting System**
   - PDF generation
   - Data export (Excel, CSV)
   - Advanced filtering and search

5. **Real-time Updates**
   - Live dashboard statistics
   - Automatic data refresh
   - Push notifications for non-compliance

## How to Run

1. **Prerequisites:**
   - Visual Studio 2022 or later
   - .NET 8.0 SDK

2. **Build and Run:**
   ```powershell
   # Navigate to project directory
   cd AbsenSeaFrontendFix
   
   # Restore dependencies
   dotnet restore
   
   # Build the project
   dotnet build
   
   # Run the application
   dotnet run
   ```

3. **Alternatively:**
   - Open `AbsenSeaFrontendFix.sln` in Visual Studio
   - Press F5 to build and run

## Navigation

- **Login Page:** Entry point - authenticate or navigate to registration
- **Dashboard:** Overview of all statistics and recent activity
- **Attendance:** Real-time crew check-in and equipment verification
- **Reports:** Detailed attendance records with filtering options
- **Settings:** Configuration and preferences (UI placeholder)

## UI/UX Highlights

✅ **Responsive Design** - Adapts to different screen sizes
✅ **Intuitive Navigation** - Persistent header with clear page indicators
✅ **Visual Feedback** - Color-coded status for instant recognition
✅ **Professional Branding** - Maritime-themed design elements
✅ **Accessibility** - High contrast, readable fonts, clear hierarchy
✅ **Modern Aesthetics** - Clean, minimal interface with purposeful design

## Future Enhancements

- Multi-language support
- Mobile companion app
- Advanced analytics and trends
- Custom compliance rules per ship
- Integration with maritime safety standards
- Crew member self-service portal
- Email/SMS notifications
- Biometric authentication

## License

This project is created for ship owner use. All rights reserved.

## Support

For questions or support, please contact your system administrator.

---

**Built with ❤️ for maritime safety and crew accountability**
