#
# usage: make.bat
#

PATH:=$(PATH);$("PROGRAMFILES(X86)")\MSBuild\14.0\Bin;$("PROGRAMFILES(X86)")\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools
WORK=bin\netf4
OUT=$(WORK)\roku.exe
RELEASE=Release
VBFLAGS=/debug-

YANP=..\legacy\Yanp\bin\Debug\yanp.exe
YANP_OUT=Parser\MyParser.vb

RK=tests\test.rk
RKOUT=.\a.exe
RKIL=a.il

all: test

clean:
	rmdir /S /Q $(WORK)
	del $(RKIL)
	del $(RKOUT)
	del $(subst exe,pdb,$(RKOUT))

test: $(RKIL)
	-$(RKOUT)
	#-start $(RKIL)

tests: $(RKIL)
	

$(OUT): $(YANP_OUT)
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

yanp: $(YANP_OUT)
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

$(RKOUT): $(OUT)
	$(OUT) $(RK) -o $(RKOUT) -a CIL

$(RKIL): $(RKOUT)
	ildasm $(RKOUT) /out:$(RKIL) /nobar
