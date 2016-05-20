@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

var fs  = WScript.CreateObject("Scripting.FileSystemObject");
var dom = WScript.CreateObject("Msxml2.DOMDocument");

var opt  = {xml : false};
var args = [];
for(var i = 0; i < WScript.Arguments.Length; i++)
{
	var arg = WScript.Arguments(i);
	if(arg == "-x") {opt.xml = true;}
	else
	{
		args.push(arg);
	}
}
WScript.Quit(main(args));

function main(args)
{
	dom.load(args[0]);
	var xs = dom.documentElement.selectNodes(args[1]);
	for(var i = 0; i < xs.length; i++)
	{
		WScript.Echo(opt.xml ? xs[i].xml : xs[i].text);
	}
	
	return(0);
}
