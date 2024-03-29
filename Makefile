#
# usage: make.bat
#

PATH:=build-tools;$(PATH);$("PROGRAMFILES(X86)")\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin;$("PROGRAMFILES(X86)")\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools
RELEASE=Release
OUT=bin\$(RELEASE)\roku.exe
VBFLAGS=/debug-

YANP=..\Yanp\bin\Debug\yanp.exe
YANP_OUT=Parser\MyParser.vb

RK=tests\test.rk
RKOBJ=tests\obj\test.exe

SRCS:=$(shell cmd /d /c build-tools\xpath Roku.vbproj /Project/ItemGroup/Compile[@Include]/@Include | build-tools\xargs echo.) sys.rk
RKSRCS:=$(wildcard tests\*.rk)
RKTEST:=$(patsubst %.rk,,$(RKSRCS))
RKOUT:=$(subst tests\,tests\obj\,$(patsubst %.rk,%.exe,$(RKSRCS)))
RKSTDOUT:=$(subst .exe,.exe.stdout,$(RKOUT))

.PHONY: all clean distclean release test tests parser parserd node

all: $(OUT)

clean:
	msbuild Roku.sln /t:Clean /p:Configuration=$(RELEASE) /v:q /nologo
	rmdir /S /Q tests\obj 2>NUL || exit /B 0
	rmdir /S /Q tests\parser 2>NUL || exit /B 0

distclean: clean
	#del /F $(YANP_OUT) 2>NUL
	rmdir /S /Q bin 2>NUL || exit /B 0
	rmdir /S /Q obj 2>NUL || exit /B 0

release:
	$(MAKE) RELEASE=Release all
	git archive HEAD --output=Roku-$(subst /,.,$(shell cmd /c date /T)).zip
	powershell -NoProfile Compress-Archive -Force -Path bin\Release\roku.exe, README.md, LICENSE -DestinationPath Roku-bin-$(subst /,.,$(shell cmd /c date /T)).zip

test: clean $(OUT)
	-del /F bin\Trace\coverage.txt 2>NUL
	build-tools\time $(MAKE) tests RELEASE=Trace
	@build-tools\xpath.bat Roku.vbproj /Project/ItemGroup/Compile[@Include]/@Include | build-tools\xargs grep.bat -n -e "^ *Coverage.Case" | build-tools\coverage bin\Trace\coverage.txt Coverage.Case

tests: $(OUT) $(RKTEST)

$(RKTEST): $(subst tests\,tests\obj\,$@).exe.stdout
	@echo   $@
	@fc $(patsubst %.stdout,,$<).testerr $(patsubst %.stdout,,$<).stderr >nul || type $(patsubst %.stdout,,$<).stderr
	@if exist $(patsubst %.stdout,,$<). (fc $(patsubst %.stdout,,$<).testout $(patsubst %.stdout,,$<).stdout >$(patsubst %.stdout,,$<).diff || type $(patsubst %.stdout,,$<).diff) || echo failed!

$(RKSTDOUT): $(subst .exe.stdout,.exe,$@)
	-@if exist $<. $< $(shell cmd /d /c type $<.testargs) < $<.testin > $<.stdout

$(RKOUT): $(subst tests\obj\,tests\,$(patsubst %.exe,%.rk,$@)) $(OUT)
	@mkdir tests\obj 2>NUL || exit /B 0
	-@del /F $@ 2>NUL
	-@cd tests && ..\$(OUT) $(subst tests\,,$<) -o $(subst tests\,,$@) $(shell cmd /d /c build-tools\sed -p "s/^\s*\#\#!(.*)$/$1/" $< | build-tools\xargs -Q echo.) 2> $(subst tests\,,$@).stderr
	-@if exist $@. ildasm $@ /out:$@.fat.il /nobar && build-tools\strip-il $@.fat.il > $@.il && del /F $@.fat.il
	@build-tools\sed -p "s/^\s*\#=>(.*)$/$1/"   $< > $@.testout
	@build-tools\sed -p "s/^\s*\#=2>(.*)$/$1/"  $< > $@.testerr
	@build-tools\sed -p "s/^\s*\#<=(.*)$/$1/"   $< > $@.testin
	@build-tools\sed -p "s/^\s*\#\#\*(.*)$/$1/" $< | build-tools\xargs -Q echo. > $@.testargs
	@build-tools\sed -p "s/^\s*\#\#\?(.*)$/$1/" $< | build-tools\xargs -n 1 cmd /d /c >NUL 2>NUL

node: $(OUT)
	cd tests && ..\$(OUT) $(subst tests\,,$(RK)) -o $(subst tests\,,$(RKOBJ)) -N - | dot -Tpng > obj\node.png
	start tests\obj\node.png

$(OUT): $(YANP_OUT) $(SRCS)
	msbuild Roku.sln /t:Build /p:Configuration=$(RELEASE) /v:q /nologo
	ildasm $(OUT) /out:$(OUT).il /nobar

parser: $(YANP_OUT)
$(YANP_OUT): roku.y
	@$(MAKE) parserd

parserd:
	cd ..\Yanp && msbuild Yanp.sln /nologo /v:q /t:build /p:Configuration=Debug
	mkdir tests\parser 2>NUL || exit /B 0
	$(YANP) \
		-i roku.y \
		-v tests\\parser\\roku.txt \
		-c tests\\parser\\roku.csv \
		-p .\\Parser\\ \
		-b ..\\Yanp \
		-t vb
	
	find /n "/reduce" < tests\parser\roku.txt || exit /B 0
	
	del Parser\ParserGenerator.sln 2>NUL
	del Parser\ParserSample.vbproj 2>NUL
	del Parser\Main.vb   2>NUL
	del Parser\IToken.vb 2>NUL
	del Parser\Token.vb  2>NUL

