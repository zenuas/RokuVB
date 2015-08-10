@echo off
prompt $$$S

mkdir Compiler 2> NUL

cscript //nologo ..\legacy\build-tools\make.vbs Parser\MyParser.vb roku.y || call :yanp

if "%1" neq "--quit" pause
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

