@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
  echo vswhere.exe not found.
  exit /b 1
)

for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VS=%%i"
if not defined VS (
  for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -property installationPath`) do set "VS=%%i"
)
if not defined VS (
  echo Visual Studio with VC tools not found. Install Desktop C++ or Build Tools VCTools workload.
  exit /b 1
)

set "VCVARS=%VS%\VC\Auxiliary\Build\vcvars64.bat"
if not exist "%VCVARS%" set "VCVARS=%VS%\VC\Auxiliary\Build\vcvarsall.bat"
if not exist "%VCVARS%" (
  echo vcvars not found under %VS%
  exit /b 1
)

echo Using: %VS%
call "%VCVARS%" x64 >nul 2>&1
if errorlevel 1 call "%VCVARS%" >nul

set OUTDIR=%~dp0..\..\publish\credential-provider
if not exist "%OUTDIR%" mkdir "%OUTDIR%"
if not exist "%~dp0obj" mkdir "%~dp0obj"

REM /MT so LogonUI does not need VC++ redistributable.
set CLFLAGS=/nologo /O2 /W3 /EHsc /std:c++17 /DUNICODE /D_UNICODE /DWIN32 /D_WINDOWS /D_USRDLL /DNDEBUG /MT
set SOURCES=dllmain.cpp TotpProvider.cpp TotpCredential.cpp helpers.cpp config.cpp totp.cpp diag.cpp

pushd "%~dp0"
cl %CLFLAGS% %SOURCES% /Foobj\ /link /nologo /DLL /DEF:WinTotp.CredentialProvider.def /OUT:"%OUTDIR%\WinTotp.CredentialProvider.dll" credui.lib bcrypt.lib crypt32.lib advapi32.lib ole32.lib oleaut32.lib shlwapi.lib secur32.lib
set ERR=%ERRORLEVEL%
popd

if not %ERR%==0 (
  echo Build failed: %ERR%
  exit /b %ERR%
)

echo Built: %OUTDIR%\WinTotp.CredentialProvider.dll
exit /b 0
