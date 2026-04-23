; installer/setup.iss
; DesktopTranslation Installer — Ramen Cat Studio
; Automatically downloads and installs .NET 8 Desktop Runtime if not present

#define MyAppName "DesktopTranslation"
#define MyAppVersion "1.2.13"
#define MyAppPublisher "Ramen Cat Studio"
#define MyAppURL "https://desktop-translation.pages.dev"
#define MyAppExeName "DesktopTranslation.exe"

; .NET 8 Desktop Runtime download URL (x64)
#define DotNetRuntimeURL "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
#define DotNetInstallerName "windowsdesktop-runtime-8.0-win-x64.exe"
#define DotNetMinVersion "8.0.0"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\dist
OutputBaseFilename=DesktopTranslation-v{#MyAppVersion}-Setup
Compression=lzma2
SolidCompression=yes
SetupIconFile=..\src\DesktopTranslation\Assets\app-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; Flags: uninsdeletevalue dontcreatekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "啟動 {#MyAppName}"; Flags: nowait postinstall

[Code]
// Check if .NET 8 Desktop Runtime is installed
function IsDotNet8Installed: Boolean;
var
  ResultCode: Integer;
begin
  // Try running dotnet --list-runtimes and check for Microsoft.WindowsDesktop.App 8.x
  Result := Exec('cmd.exe', '/c dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 8."', '',
                 SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := Result and (ResultCode = 0);

  // Fallback: check registry
  if not Result then
  begin
    Result := RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App');
  end;
end;

// Download and install .NET 8 Desktop Runtime
function DownloadAndInstallDotNet: Boolean;
var
  TempFile: String;
  ResultCode: Integer;
  DownloadPage: TDownloadWizardPage;
begin
  Result := True;

  // Use built-in downloader
  TempFile := ExpandConstant('{tmp}\{#DotNetInstallerName}');

  // Show progress
  DownloadPage := CreateDownloadPage('正在下載 .NET 8 Runtime', '正在下載必要的執行環境，請稍候...', nil);
  DownloadPage.Clear;
  DownloadPage.Add('{#DotNetRuntimeURL}', '{#DotNetInstallerName}', '');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
    except
      // Download failed — offer manual download
      if MsgBox('自動下載 .NET 8 Runtime 失敗。' + #13#10 + #13#10 +
                '請手動前往以下網址下載安裝：' + #13#10 +
                'https://dotnet.microsoft.com/download/dotnet/8.0' + #13#10 + #13#10 +
                '是��繼續安裝？（安裝後需手動安裝 .NET 8 才能執行程式）',
                mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
        Exit;
      end;
      Exit;
    end;
  finally
    DownloadPage.Hide;
  end;

  // Run the .NET installer silently
  if FileExists(TempFile) then
  begin
    MsgBox('即將安裝 .NET 8 Desktop Runtime。' + #13#10 +
           '安裝過程中可能需要管理員權限。', mbInformation, MB_OK);
    Exec(TempFile, '/install /quiet /norestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode);
    if ResultCode <> 0 then
    begin
      // Silent install failed, try interactive
      Exec(TempFile, '/install', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode);
    end;
  end;
end;

// Main setup initialization
function InitializeSetup: Boolean;
begin
  Result := True;

  if not IsDotNet8Installed then
  begin
    if MsgBox('{#MyAppName} 需��� .NET 8 Desktop Runtime 才能執行。' + #13#10 + #13#10 +
              '���否要自動下載並安裝？（約 55MB）',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      Result := DownloadAndInstallDotNet;
    end
    else
    begin
      MsgBox('請手動安裝 .NET 8 Desktop Runtime 後再執行本安裝程式。' + #13#10 +
             '下載網址：https://dotnet.microsoft.com/download/dotnet/8.0',
             mbInformation, MB_OK);
      Result := False;
    end;
  end;
end;

// Uninstall: ask about keeping settings
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    if MsgBox('是否要刪除所有設定檔？' + #13#10 +
              '（包含 API Key、視窗位置等偏好設定）',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      // User chose to delete settings
      DelTree(ExpandConstant('{userappdata}\DesktopTranslation'), True, True, True);
    end;
  end;
end;
