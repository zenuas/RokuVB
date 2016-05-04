#
# usage: make.bat
#

PATH:=build-tools;$(PATH);$("PROGRAMFILES(X86)")\MSBuild\14.0\Bin;$("PROGRAMFILES(X86)")\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools
WORK=bin\netf4
OUT=$(WORK)\roku.exe
RELEASE=Release
VBFLAGS=/debug-

YANP=..\Yanp\bin\Debug\yanp.exe
YANP_OUT=Parser\MyParser.vb

RK=tests\test.rk
RKOBJ=tests\obj\test.exe

SRCS:=$(wildcard %.vb)
RKTEST:=$(subst .rk,,$(wildcard tests\*.rk))
RKOUT:=$(subst tests\,tests\obj\,$(patsubst %.rk,%.exe,$(wildcard tests\*.rk)))

.PHONY: all clean parser parserd node

all: test

clean:
	rmdir /S /Q $(WORK) 2>NUL || exit /B 0
	rmdir /S /Q tests\obj 2>NUL || exit /B 0
	rmdir /S /Q tests\parser 2>NUL || exit /B 0

distclean: clean
	del /F $(YANP_OUT) 2>NUL
	rmdir /S /Q tests\\parser || exit /B 0

test: $(RKOBJ)
	$<

tests: $(RKTEST)

$(RKTEST): $(subst tests\,tests\obj\,$@).exe
	@build-tools\sed -p "s/^\s*\#=>(.*)$/$1/" $@.rk > tests\obj\.stdout
	@build-tools\sed -p "s/^\s*\#<=(.*)$/$1/" $@.rk > tests\obj\.stdin
	@ildasm $< /out:$<.il /nobar
	@echo $<
	-@$< < tests\obj\.stdin > $<.stdout
	@diff tests\obj\.stdout $<.stdout | build-tools\tee $<.diff
	@del tests\obj\.stdout 2>NUL
	@del tests\obj\.stdin  2>NUL

$(RKOUT): $(subst tests\obj\,tests\,$(patsubst %.exe,%.rk,$@)) $(OUT)
	mkdir tests\obj 2>NUL || exit /B 0
	cd tests && ..\$(OUT) $(subst tests\,,$<) -o $(subst tests\,,$@)
	ildasm $@ /out:$@.il /nobar

node: $(OUT)
	cd tests && ..\$(OUT) $(subst tests\,,$(RK)) -o $(subst tests\,,$(RKOBJ)) -N - | dot -Tpng > obj\node.png
	start tests\obj\node.png

$(OUT): $(YANP_OUT) $(SRCS)
	mkdir $(WORK) 2>NUL || exit /B 0
	vbc \
		/nologo \
		/out:$(OUT) \
		/t:exe \
		/noconfig \
		/nostdlib \
		/novbruntimeref \
		/vbruntime* \
		/optimize+ \
		/optionstrict+ \
		/optioninfer+ \
		/filealign:512 \
		/rootnamespace:Roku \
		/define:"CONFIG=\"$(RELEASE)\",TRACE=-1,_MyType=\"Empty\",PLATFORM=\"AnyCPU\"" \
		/r:System.dll \
		/warnaserror+ \
		$(VBFLAGS) \
		\
		/recurse:*.vb
	ildasm $(OUT) /out:$(OUT).il /nobar

parser: $(YANP_OUT)
$(YANP_OUT): roku.y
	@$(MAKE) parser

parserd:
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

