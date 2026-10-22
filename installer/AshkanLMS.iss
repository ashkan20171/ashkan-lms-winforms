#define AppName "AshkanLMS"
#define AppVersion "1.0.66"
#define AppPublisher "AshkanLMS"
#define AppExeName "AshkanLMS.exe"

[Setup]
AppId={{7C7426E7-08D2-4934-BD4C-3A6B85E5F721}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\AshkanLMS
DefaultGroupName=AshkanLMS
OutputDir=..\dist\installer
OutputBaseFilename=AshkanLMS-Setup-Stage66
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x86 x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\deployment\FIRST-RUN-GUIDE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\AshkanLMS-Release\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "AshkanLMS.exe.config,*.pdb"
Source: "..\dist\AshkanLMS-Release\AshkanLMS.exe.config"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{group}\First-run guide"; Filename: "{app}\FIRST-RUN-GUIDE.txt"
Name: "{group}\AshkanLMS"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\AshkanLMS"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch AshkanLMS"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ReleaseValue: Cardinal;
begin
  Result := True;
  if not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', ReleaseValue) or (ReleaseValue < 528040) then
  begin
    MsgBox('AshkanLMS requires Microsoft .NET Framework 4.8 or later. Install it before running setup.', mbCriticalError, MB_OK);
    Result := False;
  end;
end;
