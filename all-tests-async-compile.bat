@prompt $$$S
@echo off

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
	start /B cmd /c "%BIN% %RK% -o %RKOUT% %LIB% -N %RKOUT%.dot 2>%RKOUT%.stderr && dot -Tpng %RKOUT%.dot > %RKOUT%.png"
	exit /B 0

