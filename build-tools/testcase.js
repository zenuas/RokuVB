/*
	usage: test.js foo.js
		#=>stdout
		#<=stdin
*/
WScript.Quit(main(WScript.Arguments));

function main(args)
{
	if(args.Length != 1) {return(1);}
	
	var stdin  = "";
	var stdout = "";
	
	open_read(args.Item(0), function (input)
		{
			while(!input.AtEndOfStream)
			{
				var line = input.ReadLine();
				var m;
				if     (m = /^\s*#=>(.*)/.exec(line)) {stdout += m[1] + "\n";}
				else if(m = /^\s*#<=(.*)/.exec(line)) {stdin  += m[1] + "\n";}
			}
		});
	print(stdout);
	print_err(stdin);
	
	return(0);
}

function print(s)
{
	WScript.StdOut.Write(s);
}

function print_err(s)
{
	WScript.StdErr.Write(s);
}

function open_read(name, f)
{
	var fs    = new ActiveXObject("Scripting.FileSystemObject");
	var input = fs.OpenTextFile(name, 1 /* ForReading */, false);
	try
	{
		f(input);
	}
	finally
	{
		input.Close();
	}
}
