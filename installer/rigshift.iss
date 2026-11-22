; Rigshift per-machine installer (Inno Setup 6.7.1).
; Build with installer\build.ps1, which publishes the self-contained win-x64 output first.

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

#ifndef SourceDir
  #define SourceDir "..\artifacts\publish"
#endif

[Setup]
; Fixed for the life of the product: upgrades and uninstall find the install by this id.
AppId={{365D421D-FC94-4CFB-9416-357C0F2C2BDC}
AppName=Rigshift
AppVersion={#AppVersion}
AppPublisher=Rigshift
; Per-machine only. There is deliberately no PrivilegesRequiredOverridesAllowed, so setup
; never offers a per-user install into a folder the user can write.
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Fixed folder: the tray relaunches its own exe as administrator for admin steps, so the
; install folder must stay read-only for Users. The [Code] check below also rejects /DIR=.
DefaultDirName={autopf}\Rigshift
DisableDirPage=yes
UsePreviousAppDir=no
DefaultGroupName=Rigshift
DisableProgramGroupPage=yes
; Contract with Rigshift/Program.cs: the tray holds this mutex while it runs. Setup and
; Uninstall ask the user to exit the tray instead of closing it through Restart Manager.
AppMutex=Rigshift.SingleInstance
CloseApplications=no
OutputDir=..\artifacts\installer
OutputBaseFilename=Rigshift-Setup-{#AppVersion}-x64
SetupIconFile=..\Rigshift\Assets\rigshift-app.ico
UninstallDisplayIcon={app}\Rigshift.exe
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
LicenseFile=..\LICENSE
WizardImageFile=assets\wizard-image-100.bmp,assets\wizard-image-125.bmp,assets\wizard-image-150.bmp,assets\wizard-image-175.bmp,assets\wizard-image-200.bmp,assets\wizard-image-225.bmp,assets\wizard-image-250.bmp
WizardSmallImageFile=assets\wizard-small-100.bmp,assets\wizard-small-125.bmp,assets\wizard-small-150.bmp,assets\wizard-small-175.bmp,assets\wizard-small-200.bmp,assets\wizard-small-225.bmp,assets\wizard-small-250.bmp
WizardImageBackColor=$0F1005
WizardSmallImageBackColor=$0F1005

[Tasks]
Name: "startup"; Description: "Start Rigshift when anyone signs in"

[InstallDelete]
; Clears the previous payload, including files from a manual copy and runtime files a newer
; publish no longer ships, so nothing outlives an upgrade or uninstall. Files are named one by
; one: a *.json wildcard would also match a portable rigshift.json, which must survive.
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.pdb"
Type: files; Name: "{app}\Rigshift.exe"
Type: files; Name: "{app}\createdump.exe"
Type: files; Name: "{app}\Rigshift.deps.json"
Type: files; Name: "{app}\Rigshift.runtimeconfig.json"
Type: files; Name: "{app}\rigshift.example.json"
Type: filesandordirs; Name: "{app}\Assets"
Type: filesandordirs; Name: "{app}\cs"
Type: filesandordirs; Name: "{app}\de"
Type: filesandordirs; Name: "{app}\es"
Type: filesandordirs; Name: "{app}\fr"
Type: filesandordirs; Name: "{app}\it"
Type: filesandordirs; Name: "{app}\ja"
Type: filesandordirs; Name: "{app}\ko"
Type: filesandordirs; Name: "{app}\pl"
Type: filesandordirs; Name: "{app}\pt-BR"
Type: filesandordirs; Name: "{app}\ru"
Type: filesandordirs; Name: "{app}\tr"
Type: filesandordirs; Name: "{app}\zh-Hans"
Type: filesandordirs; Name: "{app}\zh-Hant"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "rigshift.json,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Rigshift"; Filename: "{app}\Rigshift.exe"
Name: "{commonstartup}\Rigshift"; Filename: "{app}\Rigshift.exe"; Tasks: startup

[Run]
; runasoriginaluser: setup runs elevated, and an elevated tray would run per-user config steps
; as administrator. The tray must always start unelevated.
Filename: "{app}\Rigshift.exe"; Description: "Launch Rigshift"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
// The only code in this script. DisableDirPage hides the folder page, but Setup.exe /DIR=
// still sets {app}. Reject any other folder before [InstallDelete] or [Files] touch it.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Expected: String;
  Actual: String;
begin
  Result := '';
  Expected := RemoveBackslashUnlessRoot(ExpandConstant('{autopf}\Rigshift'));
  Actual := RemoveBackslashUnlessRoot(ExpandConstant('{app}'));
  if CompareText(Actual, Expected) <> 0 then
    Result := 'Rigshift installs only to ' + Expected + '.';
end;
