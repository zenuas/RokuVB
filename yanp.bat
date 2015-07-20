@prompt $$$S

mkdir Compiler

..\legacy\Yanp\bin\Debug\yanp.exe ^
  -i roku.y ^
  -v tests\\roku.txt ^
  -c tests\\roku.csv ^
  -p .\\Compiler\\ ^
  -b ..\\legacy\\Yanp ^
  -t vb

find /n "/reduce" tests\roku.txt

del Compiler\ParserSample.vb8.sln
del Compiler\ParserSample.vbproj
del Compiler\Main.vb
del Compiler\IToken.vb
del Compiler\Token.vb

REM @pause
