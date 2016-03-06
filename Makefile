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
RKOUT=a.exe
RKIL=a.il

SRCS=$(wildcard %.vb)
RKTEST:=$(patsubst %.rk,%.exe,$(wildcard tests\*.rk))

.PHONY: all clean parser node $(RKTEST)

all: test

clean:
	rmdir /S /Q $(WORK)
	del /F $(RKIL)
	del /F $(RKOUT)
	del /F $(subst exe,pdb,$(RKOUT))
	del /F a.dot a.png
	del /F $(RKTEST)
	del /F $(patsubst %.exe,%.pdb,$(RKTEST))
	del /F $(patsubst %.exe,%.exe.stdout,$(RKTEST))
	del /F $(patsubst %.exe,%.exe.diff,$(RKTEST))

test: $(RKIL)
	-.\$(RKOUT)
	#-start $(RKIL)

tests: $(RKTEST)

.rk.exe: $(OUT)
	@build-tools\sed -p "s/^\s*\#=>(.*)$/$1/" $< > .stdout
	@build-tools\sed -p "s/^\s*\#<=(.*)$/$1/" $< > .stdin
	@$(OUT) $< -o $@ -a CIL
	@echo $@
	-@$@ < .stdin > $@.stdout
	@diff .stdout $@.stdout | build-tools\tee $@.diff
	@del .stdout
	@del .stdin

node: $(OUT)
	$(OUT) $(RK) -o $(RKOUT) -N - -a CIL | dot -Tpng > a.png
	start a.png

$(OUT): $(YANP_OUT) $(SRCS)
	mkdir $(WORK)
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
	$(YANP) \
		-i roku.y \
		-v tests\\roku.txt \
		-c tests\\roku.csv \
		-p .\\Parser\\ \
		-b ..\\legacy\\Yanp \
		-t vb
	
	find /n "/reduce" < tests\roku.txt
	
	del Parser\ParserSample.vb8.sln
	del Parser\ParserSample.vbproj
	del Parser\Main.vb
	del Parser\IToken.vb
	del Parser\Token.vb

$(RKOUT): $(OUT) $(RK)
	$(OUT) $(RK) -o $(RKOUT) -a CIL

$(RKIL): $(RKOUT)
	ildasm $(RKOUT) /out:$(RKIL) /nobar
