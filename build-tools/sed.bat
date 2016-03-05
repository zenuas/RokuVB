@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

WScript.Quit(main(WScript.Arguments));

function main(args)
{
	var opt   = {e : "", p : false};
	var files = [];
	
	for(var i = 0; i < args.Length; i++)
	{
		if(args(i) == "-e" && i + 1 < args.Length) {opt.e = args(i + 1); i++;}
		else if(args(i) == "-p") {opt.p = true;}
		else
		{
			files.push(args(i));
		}
	}
	if(opt.e == "" && files.length > 0) {opt.e = files.shift();}
	
	var xs   = opt.e.split("/"); // "s/reg/text/g" format
	var reg  = new RegExp(xs[1], xs[3]);
	var text = xs[2];
	
	if(files.length == 0)
	{
		replace(reg, text, WScript.StdIn, opt);
	}
	else
	{
		var fs = WScript.CreateObject("Scripting.FileSystemObject");
		for(var i = 0; i < files.length; i++)
		{
			replace(reg, text, fs.GetFile(files[i]).OpenAsTextStream(1), opt);
		}
	}
	return(0);
}

function replace(reg, text, in_, opt)
{
	while(!in_.AtEndOfStream)
	{
		var line = in_.ReadLine();
		if(opt.p && !line.match(reg)) {continue;}
		WScript.Echo(line.replace(reg, text));
	}
}
