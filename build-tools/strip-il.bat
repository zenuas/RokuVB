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
		var in_ = fs.GetFile(args(i)).OpenAsTextStream(1);
		
		var buffer   = [];
		var ismethod = false;
		while(!in_.AtEndOfStream)
		{
			var line = in_.ReadLine();
			if(!ismethod)
			{
				if(/^\{/.test(line)) {ismethod = true;}
				WScript.Echo(line);
			}
			else
			{
				if(!/^\}/.test(line)) {buffer.push(line);}
				else
				{
					var jumptbl = {};
					var jumpcnt = 0;
					for(var i = 0; i < buffer.length; i++)
					{
						var jump = buffer[i].match(/:.+ (IL_[0-9a-f]+)/);
						if(jump) {jumptbl[jump[1]] = jumpcnt++;}
					}
					for(var i = 0; i < buffer.length; i++)
					{
						WScript.Echo(buffer[i].replace(/^( *)(IL_[0-9a-f]+):/, function (all, g1, g2) {
								return(g1 + (g2 in jumptbl ? g2 + ":" : "        "));
							}));
					}
					WScript.Echo(line);
					ismethod = false;
					buffer = [];
				}
			}
		}
	}
}
