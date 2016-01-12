@prompt $$$S
@echo off
setlocal ENABLEDELAYEDEXPANSION

set PATH=%PATH%;%PROGRAMFILES(X86)%\MSBuild\14.0\Bin;%PROGRAMFILES(X86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools
set  BAT=Roku.release.bat ildecomp.bat
set WORK=bin\netf4
set  OUT=%WORK%\roku.exe
set   RK=tests/test.rk

call :filelist SRCS *.vb

call :make Compiler\MyParser.vb roku.y %BAT% || call :echoexec call yanp.bat --quit
call :make %OUT% %SRCS%                %BAT% || call :echoexec call Roku.release.bat --quit
call :make a.exe %OUT% %RK%            %BAT% || call :echoexec %OUT% %RK% -o a.exe -a CIL
call :make a.il  a.exe                 %BAT% || call :echoexec ildasm a.exe /out:a.il /nobar

call :echoexec .\a.exe

start a.il

pause
exit /b 0


:echoexec
	echo $ %*
	%*
	echo.
	exit /b %ERRORLEVEL%

:make
	REM echo WScript.Echo CreateObject("Scripting.FileSystemObject").GetFile(WScript.Arguments(0)).DateLastModified > @cscript_oneliner.vbs
	REM call :setcmd target "cscript @cscript_oneliner.vbs //nologo %~s1"
	cscript //nologo ..\legacy\build-tools\make.vbs %*
	exit /b %ERRORLEVEL%

:filelist
	set %1=
	for /r %%a in (%2) do set %1=!%1! "%%a"
	exit /b 0

:setcmd
	set _P=
	for /f "delims=" %%v IN ('%2') do (
		set _P=!_P!%%v^


	)
	set %1=!_P!
	exit /b 0

