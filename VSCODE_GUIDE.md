# VSCode Setup and Usage Guide

This guide explains how to use the Yahoo Finance Downloader project in Visual Studio Code.

## Prerequisites

1. **Visual Studio Code** - Download from https://code.visualstudio.com/
2. **.NET 8.0 SDK** - Download from https://dotnet.microsoft.com/download
3. **C# Extension** - Will be prompted to install when opening the project

## Opening the Project

1. Open VSCode
2. File → Open Folder
3. Navigate to `C:\Users\savov\source\yfin-csharp`
4. Click "Select Folder"

VSCode will prompt you to install recommended extensions:
- **C# Dev Kit** (ms-dotnettools.csdevkit)
- **C#** (ms-dotnettools.csharp)

Click **Install** for both.

## First Time Setup

### 1. Restore NuGet Packages

Open the integrated terminal (`` Ctrl+` `` or View → Terminal) and run:

```bash
dotnet restore
```

This downloads the HtmlAgilityPack dependency.

### 2. Build the Project

Press `Ctrl+Shift+B` or run in terminal:

```bash
dotnet build
```

You should see:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Running the Application

### Method 1: Using Debug Configurations (Recommended)

1. Press `F5` or click the "Run and Debug" icon in the sidebar
2. Select one of the pre-configured launch configurations:
   - **".NET Core Launch (AAPL - 1 year)"** - Downloads 1 year of AAPL data
   - **".NET Core Launch (MSFT - Custom Range)"** - Downloads MSFT data for 2024
   - **".NET Core Launch (GOOGL - Hourly)"** - Downloads hourly GOOGL data

3. Click the green play button or press `F5`

The application will run in the integrated terminal with the selected arguments.

### Method 2: Using Terminal

Open the integrated terminal and run:

```bash
# Basic usage
dotnet run -- --ticker AAPL

# With custom arguments
dotnet run -- --ticker MSFT --start 2024-01-01 --end 2024-12-31

# Hourly data
dotnet run -- --ticker GOOGL --start 2024-12-01 --interval 1h --output google.csv
```

**Note:** The `--` is required to separate dotnet arguments from application arguments.

### Method 3: Run Compiled Binary

After building, you can run the compiled executable directly:

```bash
# Windows
.\bin\Debug\net8.0\YahooFinanceDownloader.exe --ticker AAPL

# Linux/Mac
./bin/Debug/net8.0/YahooFinanceDownloader --ticker AAPL
```

## Debugging

### Setting Breakpoints

1. Click in the left margin next to any line number to set a breakpoint (red dot appears)
2. Press `F5` to start debugging
3. Execution will pause at breakpoints
4. Use the debug toolbar to:
   - **Continue** (F5)
   - **Step Over** (F10)
   - **Step Into** (F11)
   - **Step Out** (Shift+F11)

### Debugging with Custom Arguments

1. Open `.vscode/launch.json`
2. Modify the `args` array in any configuration:

```json
{
    "name": "My Custom Config",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build",
    "program": "${workspaceFolder}/bin/Debug/net8.0/YahooFinanceDownloader.dll",
    "args": [
        "--ticker",
        "TSLA",
        "--start",
        "2024-01-01",
        "--output",
        "tesla.csv"
    ],
    "cwd": "${workspaceFolder}",
    "console": "integratedTerminal"
}
```

3. Select your configuration from the debug dropdown
4. Press `F5`

### Viewing Variables

While debugging:
- **Variables pane** (left sidebar) shows all local variables
- **Watch pane** - Add expressions to monitor
- **Hover over variables** in code to see their values
- **Debug Console** - Evaluate expressions during debugging

## Project Structure in VSCode

```
yfin-csharp/
├── .vscode/                    # VSCode configuration
│   ├── launch.json            # Debug configurations
│   ├── tasks.json             # Build tasks
│   ├── settings.json          # Workspace settings
│   └── extensions.json        # Recommended extensions
├── bin/                        # Compiled binaries (ignored in git)
├── obj/                        # Build artifacts (ignored in git)
├── Program.cs                  # Main entry point
├── YahooFinanceClient.cs       # API client
├── PriceBar.cs                 # Data model
├── RateLimitException.cs       # Custom exception
├── YahooFinanceDownloader.csproj  # Project file
├── README.md                   # Main documentation
├── VSCODE_GUIDE.md            # This file
└── .gitignore                  # Git ignore rules
```

## Useful VSCode Commands

### Build Commands

- **Build** - `Ctrl+Shift+B` (or Tasks: Run Build Task)
- **Clean** - Command Palette → Tasks: Run Task → clean
- **Restore** - Command Palette → Tasks: Run Task → restore

### Navigation

- **Go to Definition** - `F12` (or right-click → Go to Definition)
- **Find All References** - `Shift+F12`
- **Go to Symbol** - `Ctrl+Shift+O` (list all methods/properties in file)
- **Go to File** - `Ctrl+P`
- **Search in Files** - `Ctrl+Shift+F`

### Editing

- **Format Document** - `Shift+Alt+F`
- **Organize Imports** - Right-click → Organize Imports
- **Rename Symbol** - `F2` (renames across entire project)
- **Quick Fix** - `Ctrl+.` (shows suggested fixes)

### IntelliSense

- **Trigger IntelliSense** - `Ctrl+Space`
- **Parameter Info** - `Ctrl+Shift+Space` (shows method parameters)

## Available Tasks

Press `Ctrl+Shift+P` and type "Tasks: Run Task" to see:

- **build** - Compile the project
- **clean** - Remove build artifacts
- **restore** - Restore NuGet packages
- **publish** - Publish for deployment
- **watch** - Auto-rebuild on file changes

## Testing the Application

### Quick Test

1. Press `F5` (runs default configuration: AAPL)
2. Wait for authentication and download
3. Check the output in the terminal
4. Look for the generated CSV file in the project folder

### Test Different Scenarios

Modify `.vscode/launch.json` to test:

```json
// Test with invalid ticker
"args": ["--ticker", "INVALID"]

// Test with date in the future (should fail)
"args": ["--ticker", "AAPL", "--start", "2025-01-01"]

// Test with very old data
"args": ["--ticker", "AAPL", "--start", "2000-01-01", "--end", "2001-01-01"]

// Test intraday data
"args": ["--ticker", "AAPL", "--start", "2024-12-28", "--interval", "1m"]
```

## Troubleshooting

### "OmniSharp server crashed"

1. Press `Ctrl+Shift+P`
2. Type "OmniSharp: Restart OmniSharp"
3. Wait for server to restart

### ".NET SDK not found"

1. Verify .NET SDK is installed:
   ```bash
   dotnet --version
   ```
2. If not installed, download from https://dotnet.microsoft.com/download
3. Restart VSCode after installing

### "Cannot find build task"

1. Open `.vscode/tasks.json`
2. Verify the file exists and is valid JSON
3. Press `Ctrl+Shift+B` to trigger build

### Build Errors

1. Check the "Problems" tab (View → Problems)
2. Click on any error to jump to the line
3. Use `Ctrl+.` for quick fixes

### Package Restore Issues

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Rebuild
dotnet build
```

## Output Files

Generated CSV files will appear in the project root folder:
- `AAPL_20240101_20241231.csv`
- `MSFT_20240101_20241231.csv`
- etc.

These are ignored by git (see `.gitignore`).

## Publishing for Distribution

To create a standalone executable:

```bash
# Windows executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux executable
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS executable
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be in `bin/Release/net8.0/{runtime}/publish/`

## Tips for Development

1. **Auto-format on save** is enabled (see `.vscode/settings.json`)
2. **Use IntelliSense** - Type a few letters and `Ctrl+Space` for suggestions
3. **Use snippets** - Type `cw` then `Tab` for `Console.WriteLine()`
4. **Explore with debugger** - Set breakpoints and step through code
5. **Check Problems tab** - Shows all errors and warnings in real-time

## Next Steps

- Modify `Program.cs` to add new command-line options
- Extend `YahooFinanceClient.cs` to fetch other data (quotes, fundamentals)
- Add new export formats (JSON, Excel, database)
- Implement caching to avoid re-fetching data
- Add unit tests

## Resources

- [VSCode C# Documentation](https://code.visualstudio.com/docs/languages/csharp)
- [.NET CLI Reference](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [C# Language Reference](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [Yahoo Finance Methods Guide](../yfinance-methods.md)

## Keyboard Shortcuts Cheat Sheet

| Action | Shortcut |
|--------|----------|
| Start Debugging | `F5` |
| Run without Debugging | `Ctrl+F5` |
| Build | `Ctrl+Shift+B` |
| Toggle Terminal | `` Ctrl+` `` |
| Command Palette | `Ctrl+Shift+P` |
| Quick Open File | `Ctrl+P` |
| Go to Definition | `F12` |
| Find References | `Shift+F12` |
| Rename Symbol | `F2` |
| Format Document | `Shift+Alt+F` |
| Toggle Breakpoint | `F9` |
| Step Over | `F10` |
| Step Into | `F11` |
| Continue | `F5` |
