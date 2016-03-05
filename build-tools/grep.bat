@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

WScript.Quit(main(WScript.Arguments));

function main(args)
{
	var opt   = {pattern : "", i : false, v : false, e : false, n : false};
	var files = [];
	
	for(var i = 0; i < args.Length; i++)
	{
		if     (args(i) == "-i") {opt.i = true;}
		else if(args(i) == "-v") {opt.v = true;}
		else if(args(i) == "-e") {opt.e = true;}
		else if(args(i) == "-n") {opt.n = true;}
		else
		{
			files.push(args(i));
		}
	}
	opt.pattern = files.shift();
	
	var reg = new RegExp((opt.e ? opt.pattern : escape(opt.pattern)), (opt.i ? "i" : ""));
	
	if(files.length == 0)
	{
		search(reg, "", WScript.StdIn, opt);
	}
	else
	{
		var fs = WScript.CreateObject("Scripting.FileSystemObject");
		for(var i = 0; i < files.length; i++)
		{
			search(reg, files[i] + ":", fs.GetFile(files[i]).OpenAsTextStream(1), opt);
		}
	}
	return(0);
}

function search(reg, name, in_, opt)
{
	var line_count = 0;
	while(!in_.AtEndOfStream)
	{
		var line = in_.ReadLine();
		line_count++;
		if(opt.v != !line.match(reg)) {continue;}
		WScript.Echo(name + (opt.n ? line_count + ":" : "") + line);
	}
}

function escape(s)
{
	return(s.replace(/[.*+?^$\[\](){}\\]/g, "\\$&"));
}
