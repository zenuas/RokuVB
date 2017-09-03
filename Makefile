#
# usage: make.bat
#

PATH:=build-tools;$(PATH);$("PROGRAMFILES(X86)")\MSBuild\14.0\Bin;$("PROGRAMFILES(X86)")\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools
RELEASE=Trace
OUT=bin\$(RELEASE)\roku.exe
VBFLAGS=/debug-

YANP=..\Yanp\bin\Debug\yanp.exe
YANP_OUT=Parser\MyParser.vb

RK=tests\test.rk
RKOBJ=tests\obj\test.exe

SRCS:=$(wildcard %.vb)
RKSRCS:=$(wildcard tests\*.rk)
RKTEST:=$(subst .rk,,$(RKSRCS))
RKOUT:=$(subst tests\,tests\obj\,$(patsubst %.rk,%.exe,$(RKSRCS)))

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
	git archive HEAD --output=Roku_$(subst /,.,$(shell cmd /c date /T)).zip
	zip Roku_$(subst /,.,$(shell cmd /c date /T)).zip bin\Release\roku.exe

test: clean $(OUT)
	del /F bin\$(RELEASE)\coverage.txt 2>NUL
	build-tools\time $(MAKE) tests
	@build-tools\xpath.bat Roku.vbproj /Project/ItemGroup/Compile[@Include]/@Include | build-tools\xargs grep.bat -n -e "^ *Coverage.Case" | build-tools\coverage bin\$(RELEASE)\coverage.txt Coverage.Case

tests: $(OUT) $(RKTEST)

$(RKTEST): $(subst tests\,tests\obj\,$@).exe
	-@if exist $<. $< $(shell cmd /d /c build-tools\sed -p "s/^\s*\#\#\*(.*)$/$1/" $@.rk | build-tools\xargs -Q echo.) < $<.testin > $<.stdout && (fc $<.testout $<.stdout >$<.diff || type $<.diff)

$(RKOUT): $(subst tests\obj\,tests\,$(patsubst %.exe,%.rk,$@)) $(OUT)
	@mkdir tests\obj 2>NUL || exit /B 0
	-@del /F $@ 2>NUL
	-@cd tests && ..\$(OUT) $(subst tests\,,$<) -o $(subst tests\,,$@) $(shell cmd /d /c build-tools\sed -p "s/^\s*\#\#!(.*)$/$1/" $< | build-tools\xargs -Q echo.)
	-@if exist $@. ildasm $@ /out:$@.il /nobar
	@build-tools\sed -p "s/^\s*\#=>(.*)$/$1/"   $< > $@.testout
	@build-tools\sed -p "s/^\s*\#<=(.*)$/$1/"   $< > $@.testin
	@build-tools\sed -p "s/^\s*\#\#\?(.*)$/$1/" $< | build-tools\xargs -n 1 cmd /d /c >NUL 2>NUL

$(RKSRCS):
	@echo $@

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

