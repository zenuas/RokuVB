@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

WScript.Quit(main(WScript.Arguments));

function main(args)
{
	var sh    = WScript.CreateObject("WScript.Shell");
	var start = new Date();
	
	var arg   = "";
	for(var i = 0; i < WScript.Arguments.Length; i++)
	{
		if(i > 0) {arg += " ";}
		arg += escape(WScript.Arguments(i));
	}
	
	var exec = sh.Exec("cmd /d /c " + arg + " 2>&1")
	while(!exec.StdOut.AtEndOfStream)
	{
		WScript.Echo(exec.StdOut.ReadLine());
	}
	
	var end = new Date();
	WScript.Echo("real " + ((end - start) / 1000));
}

function escape(s)
{
	if(s.indexOf(" ") >= 0)
	{
		return("\"" + s + "\"");
	}
	else
	{
		return(s);
	}
}
