; installer/setup.iss
[Setup]
AppName=DesktopTranslation
AppVersion=1.0.0
DefaultDirName={autopf}\DesktopTranslation
DefaultGroupName=DesktopTranslation
OutputDir=..\dist
OutputBaseFilename=DesktopTranslation-Setup
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\DesktopTranslation.exe
PrivilegesRequired=lowest

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\DesktopTranslation"; Filename: "{app}\DesktopTranslation.exe"
Name: "{group}\Uninstall DesktopTranslation"; Filename: "{uninstallexe}"
Name: "{autodesktop}\DesktopTranslation"; Filename: "{app}\DesktopTranslation.exe"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DesktopTranslation"; Flags: uninsdeletevalue dontcreatekey

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\DesktopTranslation"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    if MsgBox('要保留設定檔嗎？', mbConfirmation, MB_YESNO) = IDYES then
    begin
      // User wants to keep settings, remove the UninstallDelete directive effect
      // Settings are in %AppData%\DesktopTranslation
    end;
  end;
end;
