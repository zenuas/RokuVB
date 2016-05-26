@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

var fs = WScript.CreateObject("Scripting.FileSystemObject");
var sh = WScript.CreateObject("WScript.Shell");

var args = [];
for(var i = 0; i < WScript.Arguments.Length; i++)
{
	args.push(WScript.Arguments(i));
}
WScript.Quit(main(args));

function main(args)
{
	var coverage = {};
	var current  = sh.CurrentDirectory + "\\"
	
	var stdin = WScript.StdIn;
	while(!stdin.AtEndOfStream)
	{
		var xs = stdin.ReadLine().split(":");
		coverage[xs[0] + ":" + xs[1]] = 0;
	}
	
	var in_ = fs.GetFile(args[0]).OpenAsTextStream(1);
	while(!in_.AtEndOfStream)
	{
		var line = in_.ReadLine();
		var xs   = line.split(":");
		if(xs.length > 0 && xs[0] == args[1])
		{
			var path = xs[1] + ":" + xs[2];
			if(path.indexOf(current) == 0) {path = path.substring(current.length);}
			var c = path + ":" + xs[5];
			if(c in coverage) {coverage[c] += 1;}
		}
	}
	
	var ok_case = 0;
	var ng_case = 0;
	for(var c in coverage)
	{
		if(coverage[c] == 0)
		{
			WScript.Echo(c);
			ng_case++;
		}
		else
		{
			ok_case++;
		}
	}
	WScript.Echo("code coverage " + (ok_case == 0 ? "0.00" : (ok_case / (ng_case + ok_case) * 100)) + "%");
	
	return(0);
}
