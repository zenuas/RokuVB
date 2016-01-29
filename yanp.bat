@echo off
prompt $$$S

set MAKE=0
set QUIT=0
for %%v in (%*) do (
  if "%%v" equ "--make" set MAKE=1
  if "%%v" equ "--quit" set QUIT=1
)

mkdir Compiler 2> NUL

if "%MAKE%" equ "1" (
  cscript //nologo ..\legacy\build-tools\make.vbs Parser\MyParser.vb roku.y || call :yanp
) else (
  call :yanp
)

if "%QUIT%" equ "0" pause
exit /b 0



:yanp
  ..\legacy\Yanp\bin\Debug\yanp.exe ^
    -i roku.y ^
    -v tests\\roku.txt ^
    -c tests\\roku.csv ^
    -p .\\Parser\\ ^
    -b ..\\legacy\\Yanp ^
    -t vb
  
  find /n "/reduce" < tests\roku.txt
  
  del Parser\ParserSample.vb8.sln
  del Parser\ParserSample.vbproj
  del Parser\Main.vb
  del Parser\IToken.vb
  del Parser\Token.vb
  exit /b 0

