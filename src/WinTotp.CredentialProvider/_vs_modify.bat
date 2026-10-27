@echo off
"C:\Program Files (x86)\Microsoft Visual Studio\Installer\setup.exe" modify --installPath "D:\Program Files\Microsoft Visual Studio\18\Community" --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 --add Microsoft.VisualStudio.Component.VC.Redist.14.Latest --passive --norestart
echo EXIT=%ERRORLEVEL%
