@prompt $$$S
@echo off

set PATH=%PATH%;%PROGRAMFILES(X86)%\MSBuild\14.0\Bin;%PROGRAMFILES(X86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools

set COMPILE_RK=
set ENABLE_DOT=0
set ENABLE_IL=0
set ENABLE_RUN=1
set ENABLE_TEST=0

for %%v in (%*) do (
	if "%%v" == "--dot" (
		set ENABLE_DOT=1
	) else if "%%v" == "--il" (
		set ENABLE_IL=1
	) else if "%%v" == "--norun" (
		set ENABLE_RUN=0
	) else if "%%v" == "--test" (
		set ENABLE_TEST=1
	) else (
		echo %%v | findstr ".*\.rk" 1>nul
		if not ERRORLEVEL 1 (
			set COMPILE_RK=%%v
		)
	)
)
if not "%COMPILE_RK%"=="" (
	call :COMPILE %COMPILE_RK%
	exit /B 0
)
if %ENABLE_TEST% equ 1 (
	for %%f in (tests\*.rk) do (
		call :TEST %%f
	)
	exit /B 0
)

mkdir tests\obj 2>NUL

call .\make.bat

for %%f in (tests\*.rk) do (
	start /B cmd /d /c %0 %%f %*
)

exit /B 0
	
:COMPILE
	set RK=%1
	set RKOUT=%RK:.rk=.exe%
	set RKOUT=%RKOUT:tests\=tests\obj\%
	
	set LIB=
	set ARGS=
	if exist %RKOUT%.testlib. (
		for /f "DELIMS=" %%x in ('type %RKOUT%.testlib') do (
			set LIB=%%x
		)
	) else (
		call .\build-tools\sed.bat -p "s/^\s*#=>(.*)$/$1/"  %RK% > %RKOUT%.testout
		call .\build-tools\sed.bat -p "s/^\s*#=2>(.*)$/$1/" %RK% > %RKOUT%.testerr
		call .\build-tools\sed.bat -p "s/^\s*#<=(.*)$/$1/"  %RK% > %RKOUT%.testin
		call .\build-tools\sed.bat -p "s/^\s*##\*(.*)$/$1/" %RK% | .\build-tools\xargs -Q echo. > %RKOUT%.testargs
		call .\build-tools\sed.bat -p "s/^\s*##\?(.*)$/$1/" %RK% | .\build-tools\xargs.bat -n 1 cmd /d /c
		type nul > %RKOUT%.testlib
		for /f "DELIMS=" %%x in ('.\build-tools\sed.bat -p "s/^\s*##!(.*)$/$1/" %RK%') do (
			set LIB=%%x
			echo %%x> %RKOUT%.testlib
		)
	)
	for /f "DELIMS=" %%x in ('type %RKOUT%.testargs') do (
		set ARGS=%%x
	)
	
	set RK2=%RK:tests\=%
	set RKOUT2=%RKOUT:tests\=%
	set BIN=..\bin\Trace\roku.exe
	
	echo %BIN% %RK% -o %RKOUT% %LIB%
	set RKOPT=""
	if %ENABLE_DOT% equ 1 (
		set RKOPT="%RKOPT:"=% -N %RKOUT2%.dot"
	)
	set RKOPT=%RKOPT:"=%
	
	cmd /d /c "cd tests && %BIN% %RK2% -o %RKOUT2% %LIB% %RKOPT% 2>%RKOUT2%.stderr"
	
	if not ERRORLEVEL 1 (
		if %ENABLE_DOT% equ 1 (
			dot -Tpng %RKOUT%.dot > %RKOUT%.png
		)
		if %ENABLE_IL% equ 1 (
			ildasm %RKOUT% /out:%RKOUT%.fat.il /nobar
			.\build-tools\strip-il %RKOUT%.fat.il > %RKOUT%.il
			del /F %RKOUT%.fat.il
		)
		if %ENABLE_RUN% equ 1 (
			%RKOUT% %ARGS% <%RKOUT%.testin >%RKOUT%.stdout
		)
	)
	
	exit /B 0

:TEST
	set RK=%1
	set RKTEST=%RK:.rk=%
	set RKOUT=%RK:.rk=.exe%
	set RKOUT=%RKOUT:tests\=tests\obj\%
	
	echo %RKTEST%
	fc %RKOUT%.testerr %RKOUT%.stderr >nul || type %RKOUT%.stderr
	if exist %RKOUT%. (fc %RKOUT%.testout %RKOUT%.stdout >%RKOUT%.diff || type %RKOUT%.diff) || echo failed!
	exit /B 0
