# Music Vault - Setup & Build Guide

## Prerequisites

Before you start, ensure you have:

1. **.NET 6.0 SDK** or later
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

2. **Visual Studio 2022** (optional, but recommended)
   - Community Edition is free: https://visualstudio.microsoft.com/
   - Or use any .NET-compatible IDE/editor

3. **Music files** (obviously!)
   - MP3, WAV, FLAC, AAC, OGG, or M4A format
   - Properly tagged with ID3 metadata recommended

## Installation Steps

### Option 1: Command Line (Fastest)

```bash
# 1. Navigate to project directory
cd path/to/MusicVault

# 2. Restore NuGet packages
dotnet restore

# 3. Build the application
dotnet build -c Release

# 4. Run the application
dotnet run

# Or run directly from build output
./bin/Release/net6.0-windows/MusicVault.exe
```

### Option 2: Visual Studio

1. Open `MusicVault.csproj` in Visual Studio 2022
2. Visual Studio automatically restores packages
3. Press `F5` to build and run
4. Or right-click project → "Build Solution" → "Start Without Debugging"

### Option 3: Visual Studio Code

```bash
# 1. Open folder in VS Code
code .

# 2. Install C# extension (if not already)
# Cmd+Shift+P → "Extensions: Install Extensions" → Search "C#"

# 3. Open integrated terminal
# Ctrl+` 

# 4. Run build and execute
dotnet build
dotnet run
```

## First Run

When you launch the application for the first time:

1. A new `MusicVault.db` database file will be created automatically
2. The interface will appear with empty library
3. Click **"📁 Add Folder"** to scan your music directory
4. Wait for the scan to complete
5. Your library will populate with songs

## Project Structure

```
MusicVault/
├── MusicVault.csproj      ← Project file (start here)
├── Theme.cs               ← Design tokens
├── Song.cs                ← Data model
├── DatabaseService.cs     ← Database layer
├── LibraryService.cs      ← File system layer
├── NeonButton.cs          ← Custom control
├── NeonSeekBar.cs         ← Custom control
├── MiniPlayerPanel.cs     ← Custom control
├── MainForm.cs            ← Main window
├── Program.cs             ← Entry point
├── README.md              ← Documentation
└── MusicVault.db          ← Created on first run
```

## Verifying the Build

### Check NuGet Packages Are Installed

```bash
dotnet list package
```

You should see:
- System.Data.SQLite 1.0.118
- TagLibSharp 2.3.0

### Clean Build (If Issues Occur)

```bash
# Clean build artifacts
dotnet clean

# Remove NuGet cache
rm -r bin obj ~/.nuget/packages

# Restore and rebuild
dotnet restore
dotnet build -c Release
```

### Run Tests/Verify Compilation

```bash
# Just compile without running
dotnet build

# Compile with verbose output
dotnet build --verbosity detailed
```

## Configuration

### Default Behavior

- Database stored at: `./MusicVault.db` (same directory as executable)
- Supported formats: MP3, WAV, FLAC, AAC, OGG, M4A
- Theme colors defined in `Theme.cs`

### Customize Database Location

Edit line in `DatabaseService.cs`:

```csharp
private const string DbPath = "MusicVault.db";
// Change to:
private const string DbPath = @"C:\Users\YourName\Music\.musicvault\db.db";
```

### Add Supported Formats

In `LibraryService.cs`:

```csharp
private readonly string[] _supportedExtensions = { 
    ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a"
    // Add more: ".wma", ".opus", etc.
};
```

## Troubleshooting Build Issues

### Error: "Project file not found"

```bash
# Make sure you're in the project directory
cd path/to/MusicVault
ls *.csproj  # Should show MusicVault.csproj
```

### Error: ".NET 6.0 is not installed"

```bash
# Check installed runtimes
dotnet --list-runtimes

# Download .NET 6.0
# https://dotnet.microsoft.com/download/dotnet/6.0

# Or use latest version (change TargetFramework in .csproj)
```

### Error: "System.Data.SQLite not found"

```bash
# Restore packages again
dotnet restore

# Or clear package cache
rm -r ~/.nuget/packages/system.data.sqlite*
dotnet restore
```

### Error: "TagLibSharp not found"

```bash
# Same as above
dotnet restore --no-cache
```

### Application Won't Start

1. Check .NET version: `dotnet --version` (must be 6.0+)
2. Check Windows version: Windows 7 or later required
3. Rebuild: `dotnet clean && dotnet build`
4. Check for crash logs in output

## Publishing

### Create Standalone Executable

```bash
# Publish for your OS
dotnet publish -c Release -o ./publish

# Windows single-file executable
dotnet publish -c Release -r win-x64 -o ./publish \
  --self-contained -p:PublishSingleFile=true
```

### Share Application

The published folder contains everything needed - no .NET installation required!

## Performance Tips

1. **Organize Music**: Use folder structure like `Genre/Artist/Album`
2. **Optimize Database**: Periodically reset library for fresh scan
3. **Keep Metadata**: Properly tag MP3 files for best results
4. **Regular Backups**: Copy `MusicVault.db` periodically

## Development

### Add New Feature

1. Create new class in appropriate folder
2. Follow existing patterns and naming conventions
3. Use `ThemeColors` for all colors
4. Use `Sizes` and `Spacing` for layout
5. Add null-checks and try-catch for robustness

### Build Custom Control

```csharp
public class MyCustomControl : Control
{
    public MyCustomControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.UserPaint | 
                ControlStyles.DoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Custom drawing code
    }
}
```

## IDE Recommendations

### Visual Studio 2022 Community (Free)
- Full debugger support
- Excellent IntelliSense
- Built-in designer (not used here but helpful)

### Visual Studio Code (Free)
- Lightweight and fast
- C# extension handles most tasks
- Great for quick edits

### JetBrains Rider (Paid, 30-day trial)
- Powerful refactoring tools
- Best IDE for C# development
- Great for large projects

## System Requirements

| Requirement | Minimum | Recommended |
|-----------|---------|-------------|
| OS | Windows 7 | Windows 10+ |
| RAM | 512 MB | 2 GB+ |
| Disk | 100 MB | 500 MB+ |
| .NET | 6.0 | 7.0+ |
| Music Files | 100 songs | 1000+ |

## FAQ

**Q: Can I modify the theme colors?**
A: Yes! Edit `Theme.cs` - all colors are in one place.

**Q: Will adding 10,000 songs slow it down?**
A: No, the SQLite database is indexed and handles large libraries well.

**Q: How do I uninstall?**
A: Delete the entire MusicVault folder. No registry entries or system files are modified.

**Q: Can I use this on Mac/Linux?**
A: With modifications. WinForms is Windows-only, but you could port to WPF or use Avalonia for cross-platform.

**Q: Where is my data stored?**
A: Everything is in `MusicVault.db` (SQLite) in the application directory. Your music files are never touched.

## Next Steps

1. **Launch the app** and add your music folder
2. **Explore the UI** - try searching, marking favorites
3. **Customize colors** in `Theme.cs` to match your style
4. **Review the code** - it's well-commented and beginner-friendly

Enjoy your music! 🎵
