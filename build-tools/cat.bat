@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

WScript.Quit(main(WScript.Arguments));

function main(args)
{
	var fs = WScript.CreateObject("Scripting.FileSystemObject");
	
	for(var i = 0; args.Length == 0 || i < args.Length; i++)
	{
		if(args.Length == 0 || args(i) == "-")
		{
			while(!WScript.StdIn.AtEndOfStream)
			{
				WScript.Echo(WScript.StdIn.ReadLine());
			}
			if(args.Length == 0) {break;}
		}
		else
		{
			var in_ = fs.GetFile(args(i)).OpenAsTextStream(1);
			while(!in_.AtEndOfStream)
			{
				WScript.Echo(in_.ReadLine());
			}
		}
	}
}

function replace(reg, text, in_, opt)
{
}
