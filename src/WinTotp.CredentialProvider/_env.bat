@echo off
call "D:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
echo VCToolsVersion=%VCToolsVersion%
echo VSCMD_ARG_TGT_ARCH=%VSCMD_ARG_TGT_ARCH%
where msbuild
msbuild -version -nologo
