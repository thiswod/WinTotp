@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvars64.bat" || exit /b 1
cd /d "%~dp0"
if not exist "..\..\publish\credential-provider" mkdir "..\..\publish\credential-provider"
if not exist "obj" mkdir "obj"
REM /MT = static CRT so LogonUI can load without VC++ redistributable on the VM.
cl /nologo /O2 /W3 /EHsc /std:c++17 /DUNICODE /D_UNICODE /DWIN32 /D_WINDOWS /D_USRDLL /DNDEBUG /MT ^
  dllmain.cpp TotpProvider.cpp TotpCredential.cpp helpers.cpp config.cpp totp.cpp diag.cpp ^
  /Foobj\ /link /nologo /DLL /DEF:WinTotp.CredentialProvider.def ^
  /OUT:"..\..\publish\credential-provider\WinTotp.CredentialProvider.dll" ^
  credui.lib bcrypt.lib crypt32.lib advapi32.lib ole32.lib oleaut32.lib shlwapi.lib secur32.lib gdi32.lib user32.lib
echo ERRORLEVEL=%ERRORLEVEL%
exit /b %ERRORLEVEL%
