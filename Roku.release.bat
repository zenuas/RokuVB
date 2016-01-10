@echo off
setlocal ENABLEDELAYEDEXPANSION

set    PATH=%PATH%;%PROGRAMFILES(X86)%\MSBuild\14.0\Bin;%PROGRAMFILES(X86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools
set    WORK=bin\netf4
set     OUT=%WORK%\roku.exe
set RELEASE=Release
set VBFLAGS=/debug-
REM set RELEASE=Debug
REM set VBFLAGS=/debug+ /define:"Debug"

if not exist %WORK% mkdir %WORK%

call yanp.bat --quit
vbc ^
	/nologo ^
	/out:%OUT% ^
	/t:exe ^
	/noconfig ^
	/nostdlib ^
	/novbruntimeref ^
	/vbruntime* ^
	/optimize+ ^
	/optionstrict+ ^
	/optioninfer+ ^
	/filealign:512 ^
	/rootnamespace:Roku ^
	/define:"CONFIG=\"%RELEASE%\",TRACE=-1,_MyType=\"Empty\",PLATFORM=\"AnyCPU\"" ^
	/r:System.dll ^
	/warnaserror+ ^
	%VBFLAGS% ^
	^
	/recurse:*.vb

if errorlevel 1 (
  pause
  exit /b 1
)

ildasm %OUT% /out:%OUT%.il /nobar
REM del %OUT%.res
REM perl -ne "if(m/VisualBasic/) {print \"line $.: $_\"; exit 1}" %OUT%.il
REM if errorlevel 1 (
REM   echo %OUT%.il
REM   pause
REM   exit /b 1
REM )

REM perl ../legacy/Yanp/yanp.codesize-compact.pl Roku.Parser.MyParser < %OUT%.il > %OUT%.compact.il
REM ilasm %OUT%.compact.il /quit /out:%OUT% /res=%OUT%.res
REM 
REM if "%1" neq "--quit" (
REM   pause
REM )
