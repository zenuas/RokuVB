#
# usage: make.bat
#

PATH:=build-tools;$(PATH);$("PROGRAMFILES(X86)")\MSBuild\14.0\Bin;$("PROGRAMFILES(X86)")\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools
WORK=bin\netf4
OUT=$(WORK)\roku.exe
RELEASE=Release
VBFLAGS=/debug-

YANP=..\legacy\Yanp\bin\Debug\yanp.exe
YANP_OUT=Parser\MyParser.vb

RK=tests\test.rk
RKOUT=tests\obj\test.exe

SRCS=$(wildcard %.vb)
RKTEST:=$(subst tests\,tests\obj\,$(patsubst %.rk,%.exe,$(wildcard tests\*.rk)))

.PHONY: all clean parser node $(RKTEST)

all: test

clean:
	rmdir /S /Q $(WORK) || exit /B 0
	del /F $(RKOUT) 2>NUL
	del /F $(subst exe,pdb,$(RKOUT)) 2>NUL
	del /F tests\obj\.stdin tests\obj\.stdout 2>NUL
	del /F tests\obj\node.png 2>NUL
	del /F $(RKTEST) 2>NUL
	del /F $(patsubst %.exe,%.pdb,$(RKTEST)) 2>NUL
	del /F $(patsubst %.exe,%.exe.il,$(RKTEST)) 2>NUL
	del /F $(patsubst %.exe,%.exe.stdout,$(RKTEST)) 2>NUL
	del /F $(patsubst %.exe,%.exe.diff,$(RKTEST)) 2>NUL

distclean: clean
	del /F $(YANP_OUT) 2>NUL
	rmdir /S /Q tests\\parser || exit /B 0

test: $(RKOUT)

tests: $(RKTEST)

$(RKTEST): $(subst tests\obj\,tests\,$(patsubst %.exe,%.rk,$@)) $(OUT)
	@mkdir tests\obj 2>NUL || exit /B 0
	@build-tools\sed -p "s/^\s*\#=>(.*)$/$1/" $< > tests\obj\.stdout
	@build-tools\sed -p "s/^\s*\#<=(.*)$/$1/" $< > tests\obj\.stdin
	@cd tests && ..\$(OUT) $(subst tests\,,$<) -o $(subst tests\,,$@) -a CIL
	@ildasm $@ /out:$@.il /nobar
	@echo $@
	-@$@ < tests\obj\.stdin > $@.stdout
	@diff tests\obj\.stdout $@.stdout | build-tools\tee $@.diff
	@del tests\obj\.stdout 2>NUL
	@del tests\obj\.stdin  2>NUL

node: $(RKOUT)
	cd tests && ..\$(OUT) $(subst tests\,,$(RK)) -o ..\$(RKOUT) -N - -a CIL | dot -Tpng > obj\node.png
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
	mkdir tests\parser 2>NUL || exit /B 0
	$(YANP) \
		-i roku.y \
		-v tests\\parser\\roku.txt \
		-c tests\\parser\\roku.csv \
		-p .\\Parser\\ \
		-b ..\\legacy\\Yanp \
		-t vb
	
	find /n "/reduce" < tests\parser\roku.txt || exit /B 0
	
	del Parser\ParserSample.vb8.sln 2>NUL
	del Parser\ParserSample.vbproj  2>NUL
	del Parser\Main.vb   2>NUL
	del Parser\IToken.vb 2>NUL
	del Parser\Token.vb  2>NUL

$(RKOUT): $(OUT) $(RK)
	cd tests && ..\$(OUT) $(subst tests\,,$(RK)) -o ..\$(RKOUT) -a CIL

$(RKIL): $(RKOUT)
	ildasm $(RKOUT) /out:$(RKIL) /nobar
