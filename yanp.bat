@echo off
prompt $$$S

mkdir Compiler 2> NUL

cscript //nologo ..\legacy\build-tools\make.vbs Compiler\MyParser.vb roku.y || call :yanp

if "%1" neq "--quit" pause
exit /b 0



:yanp
  ..\legacy\Yanp\bin\Debug\yanp.exe ^
    -i roku.y ^
    -v tests\\roku.txt ^
    -c tests\\roku.csv ^
    -p .\\Compiler\\ ^
    -b ..\\legacy\\Yanp ^
    -t vb
  
  find /n "/reduce" < tests\roku.txt
  
  del Compiler\ParserSample.vb8.sln
  del Compiler\ParserSample.vbproj
  del Compiler\Main.vb
  del Compiler\IToken.vb
  del Compiler\Token.vb
  exit /b 0

