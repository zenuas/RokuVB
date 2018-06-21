@prompt $$$S
@echo off

set PATH=%PATH%;%PROGRAMFILES(X86)%\MSBuild\14.0\Bin;%PROGRAMFILES(X86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools

set ENABLE_DOT=0
set ENABLE_IL=0

for %%v in (%*) do (
	if "%%v" == "--dot" (
		set ENABLE_DOT=1
	)
	if "%%v" == "--il" (
		set ENABLE_IL=1
	)
)

mkdir tests\obj 2>NUL

for %%f in (tests\*.rk) do (
	call :TESTFILE %%f
)

call .\make.bat
cd tests

set BIN=..\bin\Trace\roku.exe

for %%f in (*.rk) do (
	call :COMPILE %%f
)

cd ..

exit /B 0

:TESTFILE
	set RK=%1
	set RKOUT=tests\obj\%~n1.exe
	
	if not exist %RKOUT%.testlib. (
		start /B cmd /d /c ".\build-tools\sed.bat -p s/^^^^\s*#=^^^>(.*)$/$1/  %RK% > %RKOUT%.testout "
		start /B cmd /d /c ".\build-tools\sed.bat -p s/^^^^\s*#=2^^^>(.*)$/$1/ %RK% > %RKOUT%.testerr "
		start /B cmd /d /c ".\build-tools\sed.bat -p s/^^^^\s*#^^^<=(.*)$/$1/  %RK% > %RKOUT%.testin "
		start /B cmd /d /c ".\build-tools\sed.bat -p s/^^^^\s*##\*(.*)$/$1/    %RK% > %RKOUT%.testargs "
		start /B cmd /d /c ".\build-tools\sed.bat -p s/^^^^\s*##\?(.*)$/$1/    %RK% | .\build-tools\xargs.bat -n 1 cmd /d /c "
	)
	
	exit /B 0
	
:COMPILE
	set RK=%1
	set RKOUT=obj\%RK:.rk=.exe%
	
	set LIB=
	if exist %RKOUT%.testlib. (
		for /f "DELIMS=" %%x in ('type %RKOUT%.testlib') do (
			set LIB=%%x
		)
	) else (
		type nul > %RKOUT%.testlib
		for /f "DELIMS=" %%x in ('..\build-tools\sed.bat -p "s/^\s*##!(.*)$/$1/" %RK%') do (
			set LIB=%%x
			echo %%x> %RKOUT%.testlib
		)
	)
	
	echo %BIN% %RK% -o %RKOUT% %LIB%
	set RKOPT=""
	set RKCMD=""
	if %ENABLE_DOT% equ 1 (
		set RKOPT="%RKOPT:"=% -N %RKOUT%.dot"
		set RKCMD="%RKCMD:"=% && dot -Tpng %RKOUT%.dot > %RKOUT%.png"
	)
	if %ENABLE_IL% equ 1 (
		set RKCMD="%RKCMD:"=% && ildasm %RKOUT% /out:%RKOUT%.fat.il /nobar && ..\build-tools\strip-il %RKOUT%.fat.il > %RKOUT%.il && del /F %RKOUT%.fat.il"
	)
	start /B cmd /c "%BIN% %RK% -o %RKOUT% %LIB% %RKOPT:"=% 2>%RKOUT%.stderr %RKCMD:"=%"
	exit /B 0
