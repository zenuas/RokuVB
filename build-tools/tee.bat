@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

WScript.Quit(main(WScript.Arguments));

function main(args)
{
	var fs = WScript.CreateObject("Scripting.FileSystemObject");
	
	var opt_a = "";
	var files = [];
	
	for(var i = 0; i < args.Length; i++)
	{
		var arg = args(i);
		if(arg == "-a") {opt_a = true;}
		else
		{
			var f;
			if(opt_a && fs.FileExists(arg))
			{
				f = fs.GetFile(arg).OpenAsTextStream(8);
			}
			else
			{
				f = fs.CreateTextFile(args(i), true);
			}
			files.push(f);
		}
	}
	
	while(!WScript.StdIn.AtEndOfStream)
	{
		var line = WScript.StdIn.ReadLine();
		WScript.Echo(line);
		for(var i = 0; i < files.length; i++)
		{
			files[i].WriteLine(line);
		}
	}
	return(0);
}
