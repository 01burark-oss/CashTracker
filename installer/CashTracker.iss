#define MyAppName "CashTracker"
#define MyAppVersion GetEnv("CASHTRACKER_APP_VERSION")
#define MyPublishDir GetEnv("CASHTRACKER_PUBLISH_DIR")
#define MyOutputDir GetEnv("CASHTRACKER_OUTPUT_DIR")
#define MySetupBaseName GetEnv("CASHTRACKER_SETUP_BASENAME")
#define MyIconPath GetEnv("CASHTRACKER_ICON_PATH")

[Setup]
AppId={{6A136296-EB7A-43B3-A6D4-0F8E813B8EE9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppName}
DefaultDirName={localappdata}\Programs\CashTracker
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\CashTracker.exe
Compression=lzma
SolidCompression=yes
OutputDir={#MyOutputDir}
OutputBaseFilename={#MySetupBaseName}
SetupIconFile={#MyIconPath}
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=CashTracker.exe
RestartApplications=no

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: files; Name: "{autodesktop}\CashTracker.lnk"
Type: files; Name: "{autoprograms}\CashTracker.lnk"

[Icons]
Name: "{autoprograms}\Cashtracker Fabesco"; Filename: "{app}\CashTracker.exe"
Name: "{autodesktop}\Cashtracker Fabesco"; Filename: "{app}\CashTracker.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CashTracker.exe"; Description: "CashTracker'i baslat"; Flags: nowait postinstall skipifsilent
