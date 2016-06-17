@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

var sh = WScript.CreateObject("WScript.Shell");

Array.prototype.map = function (f) {var xs = []; for(var i = 0; i < this.length; i++) {xs.push(f(this[i]));} return(xs);};

var opt  = {num : -1, quot : false};
var args = [];
for(var i = 0; i < WScript.Arguments.Length; i++)
{
	args.push(WScript.Arguments(i));
}
while(args.length > 0)
{
	if(args.length >= 2 && args[0] == "-n") {opt.num = args[1] - 0; args.shift(); args.shift();}
	else if(args[0] == "-q") {opt.quot = true; args.shift();}
	{
		break;
	}
}
WScript.Quit(main(args));

function main(args)
{
	var stdin = WScript.StdIn;
	var xs    = [];
	if(stdin.AtEndOfStream)
	{
		exec(args);
	}
	else
	{
		while(!stdin.AtEndOfStream)
		{
			xs.push(stdin.ReadLine());
			if(opt.num == xs.length)
			{
				exec(args.concat(xs));
				xs = [];
			}
		}
		if(xs.length > 0) {exec(args.concat(xs));}
	}
	return(0);
}

function exec(args)
{
	WScript.StdOut.Write(sh.Exec(args.map(function(x) {return(escape(x));}).join(" ")).StdOut.ReadAll());
}

function escape(s)
{
	if(s.indexOf(" ") >= 0 || opt.quot)
	{
		return("\"" + s + "\"");
	}
	else
	{
		return(s);
	}
}
