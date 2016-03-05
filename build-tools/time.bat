@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

WScript.Quit(main(WScript.Arguments));

function main(args)
{
	var sh    = WScript.CreateObject("WScript.Shell");
	var start = new Date();
	if(args.Length == 1)
	{
		sh.Exec("cmd /d /c " + args(0)).StdOut.ReadAll();
		var end = new Date();
		WScript.Echo("real " + ((end - start) / 1000));
	}
	else
	{
		for(var i = 0; i < args.length; i++)
		{
			var pstart = new Date();
			sh.Exec("cmd /d /c " + args(i)).StdOut.ReadAll();
			var pend = new Date();
			WScript.Echo(args(i));
			WScript.Echo("real " + ((pend - pstart) / 1000));
		}
		var end = new Date();
		WScript.Echo("total");
		WScript.Echo("real " + ((end - start) / 1000));
	}
}
