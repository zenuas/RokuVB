@if (0)==(0) echo off
cscript.exe //nologo //E:JScript "%~f0" %*
exit /B %ERRORLEVEL%
@end

var fs = WScript.CreateObject("Scripting.FileSystemObject");
var sh = WScript.CreateObject("WScript.Shell");
var eu = sh.Environment("Process");

String.prototype.trim       = function ()    {return this.replace(/^\s+|\s+$/g, "");};
String.prototype.trimEnd    = function ()    {return this.replace(/\s+$/g,      "");};
String.prototype.replaceAll = function (a,b) {return this.replace(new RegExp(escape(a), "g"), b);};
Array.prototype.indexOf     = function (v)   {for(var i = 0; i < this.length; i++) {if(this[i] == v) {return(i);}} return(-1);};
Array.prototype.map         = function (f)   {var xs = []; for(var i = 0; i < this.length; i++) {xs.push(f(this[i]));} return(xs);};

var opt  = {print : false, just_print : false, always_make : false, cmd_opt : {}};
var args = [];
for(var i = 0; i < WScript.Arguments.Length; i++)
{
	var arg = WScript.Arguments(i);
	if     (arg == "-p") {opt.print       = true;}
	else if(arg == "-n") {opt.just_print  = true;}
	else if(arg == "-B") {opt.always_make = true;}
	else if(arg.indexOf("=") >= 0)
	{
		var eq = arg.indexOf("=");
		opt.cmd_opt[arg.substring(0, eq)] = arg.substring(eq + 1);
	}
	else
	{
		args.push(arg);
	}
}

var make = parse("Makefile");
if(args.length == 0) {args.push(make.$START);}
for(var i = 0; i < args.length; i++)
{
	run(make, args[i]);
}
WScript.Quit(0);

function parse(makefile, env)
{
	if(!env)
	{
		env = this;
		
		env.$ENV     = {MAKE : WScript.ScriptFullName};
		env.$START   = "";
		env.$TARGET  = {};
		env.$CACHE   = {};
		env.$PHONY   = [];
		env.set_val = function (key, s)
			{
				env.$ENV[key] = s;
				eu.Item(key) = s;
			};
		env.get_val = function (s)
			{
				if(s in opt.cmd_opt) {return(opt.cmd_opt[s]);}
				if(s in env.$ENV)    {return(env.$ENV[s]);}
				
				var key   = "%" + s + "%";
				var value = sh.ExpandEnvironmentStrings(key);
				return(value == key && sh.Environment("SYSTEM")(s) == "" ? "" : value);
			};
	}
	
	
	var f = fs.OpenTextFile(makefile);
	try
	{
		/*
			ifeq "$(xxx)" "xxx"
				command
			else
				command
			endif
		*/
		
		var target  = "";
		var linenum = 0;
		var parse_line = function (line)
			{
				line = remove_comment(line);
				if(line.length == 0) {target = ""; return;}
				
				if(line.substring(0, 1) == "\t")
				{
					if(target == "") {throw new Error("parse error, none target(" + linenum + ")");}
					line = line.replace(/^\t+/, "");
					if(line != "") {env.$TARGET[target].commands.push(line);}
				}
				else
				{
					target = "";
					var set_index    = line.indexOf(":=");
					var expand_index = line.indexOf("=");
					var target_index = line.indexOf(":");
					
					if(set_index >= 0 && (set_index <= expand_index && set_index <= target_index))
					{
						var key   = line.substring(0, set_index).trim();
						var value = line.substring(set_index + 2).trim();
						
						env.set_val(key, expand(env, value).value);
					}
					else if(expand_index >= 0 && (target_index < 0 || expand_index < target_index))
					{
						var key   = line.substring(0, expand_index).trim();
						var value = line.substring(expand_index + 1).trim();
						
						env.set_val(key, value);
					}
					else if(target_index >= 0 && (expand_index < 0 || expand_index > target_index))
					{
						target = line.substring(0, target_index).trim();
						
						if(target == ".PHONY")
						{
							array_add(env.$PHONY, command_split(expand(env, line.substring(target_index + 1).trim()).value, " "));
						}
						else
						{
							var depends = command_split(line.substring(target_index + 1).trim(), " ");
							if(env.$START == "") {env.$START = target;}
							env.$TARGET[target] = {
									depends  : depends,
									commands : []
								};
						}
					}
					else
					{
						var commands = line.split(/\s+/);
						if     (commands[0] == "include")  {parse(commands[1], env);}
						else if(commands[0] == "-include") {parse(commands[1], env);}
						else
						{
							throw new Error("missing command " + commands[0] + "(" + linenum + ")");
						}
					}
				}
			};
		
		var line = "";
		while(!f.AtEndOfStream)
		{
			linenum += 1;
			var s = f.ReadLine();
			if(line != "") {s = s.replace(/^\s+/, " ");}
			if(s.length > 0 && s.substring(s.length - 1, s.length) == "\\")
			{
				line += s.substring(0, s.length - 1);
			}
			else
			{
				line += s;
				parse_line(line);
				line = "";
			}
		}
		if(line != "") {parse_line();}
	}
	finally
	{
		f.Close();
	}
	
	return(env);
}

function remove_comment(line)
{
	return(line.replace(/^((\\#|[^#])*)(#.*)?$/, function (match, s1) {return(s1.replace(/\\#/g, "#"));}));
}

function run(env, target)
{
	var p;
	if(!(p = env.$TARGET[target]))
	{
		var ext = get_exten(target);
		
		// "$(TARGET)" expand
		for(var c in env.$TARGET)
		{
			var t = expand(env, c).value;
			if(t == target) {p = env.$TARGET[c]; break;}
			else if(t.substring(0, 1) == ".")
			{
				// ".c.o" suffix rule
				var r = t.match(/^\.(\w+)\.(\w+)$/);
				if(r && r[2] == ext)
				{
					p = {depends : [target.substring(0, target.length - ext.length) + r[1]], commands : env.$TARGET[c].commands};
					for(var i = 0; i < env.$TARGET[c].depends.length; i++)
					{
						p.depends.push(env.$TARGET[c].depends[i]);
					}
				}
			}
			else if(command_split(t, " ").indexOf(target) >= 0) {p = env.$TARGET[c]; break;}
		}
	}
	
	var t = fs.FileExists(target) ? fs.GetFile(target).DateLastModified : 0;
	if(p)
	{
		var need = opt.always_make || env.$PHONY.indexOf(target) >= 0;
		if(!need && target in env.$CACHE) {return(env.$CACHE[target]);}
		
		var xs = command_expand(env, p.depends, function (x) {return(x.replace(/\$@/g, target));});
		if(xs.length == 0 || t == 0) {need = true;}
		for(var i = 0; i < xs.length; i++)
		{
			if(opt.print) {WScript.Echo(target + " : " + xs[i]);}
			var d = run(env, xs[i]);
			if(t == 0 || t < d) {need = true;}
		}
		if(need)
		{
			if(opt.print) {WScript.Echo(target + " : ");}
			for(var i = 0; i < p.commands.length; i++)
			{
				var cmd = p.commands[i];
				cmd = cmd.replace(/\$[@%<]/g,
					function (v)
					{
						if     (v == "$@") {return(target);}
						else if(v == "$%") {return(xs.join(" "));}
						else if(v == "$<") {return(xs[0]);}
						else
						{
							throw new Error("");
						}
					});
				
				exec(expand(env, cmd).value);
			}
			t = fs.FileExists(target) ? fs.GetFile(target).DateLastModified : 0;
		}
	}
	env.$CACHE[target] = t;
	return(t);
}

function get_exten(name)
{
	var dir_sep = name.lastIndexOf("/");
	dir_sep = dir_sep >= 0 ? dir_sep : name.lastIndexOf("\\");
	
	var ext_sep = name.lastIndexOf(".");
	return(dir_sep > ext_sep ? "" : name.substring(ext_sep + 1));
}

function expand(env, s, i, quote)
{
	i     = i || 0;
	quote = quote || "";
	
	// $(OUT)
	// $(DEPEND.INC)
	// $(SRCS:%.c=$(WORK)%.obj)
	// $(shell cygpath '$(WINDIR)')
	var r = {value : "", length : 0};
	
	for(; i < s.length; i++)
	{
		var c = s.substring(i, i + 1);
		if(quote != "" && c == quote)
		{
			return(r);
		}
		else if(c == "\"" || c == "'")
		{
			var p = expand(env, s, i + 1, c);
			r.value  += c + p.value + c;
			r.length += p.length + 2;
			i        += p.length + 1;
		}
		else if(c == "$" && s.substring(i + 1, i + 2) == "(")
		{
			var p = expand(env, s, i + 2, ")");
			var re;
			if(re = p.value.match(/^(\w+):/))
			{
				// $(SRCS:%.c=$(WORK)%.obj)
				r.value += "replace";
				throw new Error("unknown command [" + p.value + "]");
			}
			else
			{
				var xs = command_split(p.value, " ", 1);
				if(xs.length == 1)
				{
					// $(OUT)
					// $(DEPEND.INC)
					// $("ProgramFiles(x86)")
					r.value += expand(env, env.get_val(p.value.replace(/^(['"])(.*)\1$/, "$2"))).value; // '
				}
				else if(xs[0] == "shell")
				{
					// $(shell cygpath '$(WINDIR)')
					r.value += exec(xs[1], true);
				}
				else if(xs[0] == "subst")
				{
					// $(subst from,to,text)
					var param = command_split(xs[1], ",", 2);
					r.value += expand(env, param[2] || "").value.replaceAll(expand(env, param[0] || "").value, expand(env, param[1] || "").value);
				}
				else if(xs[0] == "patsubst")
				{
					// $(patsubst %.c,%.o,foo.c)
					var param = command_split(xs[1], ",", 2);
					var xxs   = command_split(expand(env, param[2] || "").value, " ");
					var reg   = new RegExp(escape(expand(env, (param[0] || "").substring(1)).value), "");
					for(var j = 0; j < xxs.length; j++)
					{
						xxs[j] = xxs[j].replace(reg, expand(env, (param[1] || "").substring(1)).value);
					}
					r.value += xxs.join(" ");
				}
				else if(xs[0] == "wildcard")
				{
					// $(wildcard *.c)
					// $(wildcard */*.c *.txt)
					var param = command_split(xs[1], " ");
					var xxs   = [];
					for(var j = 0; j < param.length; j++)
					{
						array_add(xxs, wildcard(param[j]));
					}
					r.value += xxs.join(" ");
				}
				else
				{
					throw new Error("unknown command [" + s + "]");
				}
			}
			r.length += p.length + 3;
			i        += p.length + 2;
		}
		else
		{
			r.value  += c;
			r.length += 1;
		}
	}
	if(quote != "") {throw new Error("bad quote string [" + s + "]");}
	
	return(r);
}

function command_split(s, splitter, maxsplit)
{
	splitter = splitter || " ";
	maxsplit = maxsplit || 0;
	
	var quote   = [];
	var xs      = [];
	var command = "";
	
	for(var i = 0; i < s.length; i++)
	{
		var c = s.substring(i, i + 1);
		if(quote.length == 0 && c == splitter)
		{
			if(splitter != " " || command != "")
			{
				xs.push(command);
				if(maxsplit > 0 && maxsplit == xs.length)
				{
					if(i + 1 < s.length) {xs.push(s.substring(i + 1));}
					return(xs);
				}
			}
			command = "";
		}
		else if(quote.length > 0 && c == quote[quote.length - 1])
		{
			command += c;
			quote.pop();
		}
		else
		{
			if(c == "\"" || c == "'")
			{
				quote.push(c);
			}
			else if(c == "(")
			{
				quote.push(")");
			}
			command += c;
		}
	}
	if(quote.length > 0) {throw new Error("quote error [" + s + "]");}
	
	if(command != "") {xs.push(command);}
	
	return(xs);
}

function escape(s)
{
	return(s.replace(/[.*+?^$\[\](){}\\]/g, "\\$&"));
}

function meta_match(target, meta)
{
	function match(ti, mi)
	{
		if(target.length <= ti && meta.length <= mi) {return(true);}
		if(target.length <= ti || meta.length <= mi) {return(false);}
		
		var tc = target.substring(ti, ti + 1);
		var mc = meta.substring(mi, mi + 1);
		
		if(mc == '%')
		{
			return(match(ti + 1, mi) ? true : match(ti + 1, mi + 1));
		}
		else if(tc != '/' && tc != '\\')
		{
			if(mc == '*')
			{
				return(match(ti + 1, mi) ? true : match(ti + 1, mi + 1));
			}
			else if(mc == '?')
			{
				return(match(ti + 1, mi + 1));
			}
		}
		
		return(tc == mc ? match(ti + 1, mi + 1) : false);
	}
	
	return(match(0, 0));
}

var dir_cache = null;
function wildcard(meta)
{
	var current = fs.GetFolder(".").Path;
	var files   = (dir_cache ? dir_cache : dir_cache = exec("cmd /d /c dir /B /S", true).split("\r\n"));
	var xs      = [];
	for(var i = 0; i < files.length; i++)
	{
		var p = files[i].substring(current.length + 1);
		meta_match(p, meta) && xs.push(p);
	}
	return(xs);
}

function array_add(xs, p)
{
	if(p instanceof Array)
	{
		for(var i = 0; i < p.length; i++) {array_add(xs, p[i]);}
	}
	else
	{
		xs.push(p);
	}
}

function command_expand(env, c, f)
{
	if(c instanceof Array)
	{
		var xs = [];
		for(var i = 0; i < c.length; i++)
		{
			array_add(xs, command_expand(env, c[i], f));
		}
		return(xs);
	}
	else
	{
		var xs = command_split(expand(env, f(c)).value);
		if(xs.length == 1 && xs[0] == c) {return(xs);}
		return(command_expand(env, xs, f));
	}
}

function exec(s, subshell)
{
	subshell = subshell || false;
	
	if(s instanceof Array)
	{
		s = s.join(" ");
	}
	
	// -@command
	var no_error = false;
	var no_echo  = false;
	if(s.substring(0, 1) == "-") {s = s.substring(1); no_error = true;}
	if(s.substring(0, 1) == "@") {s = s.substring(1); no_echo  = true;}
	
	if(subshell)
	{
		return(sh.Exec(s).StdOut.ReadAll().trimEnd());
	}
	else
	{
		if(!no_echo || opt.print) {WScript.Echo(s);}
		if(!opt.just_print)
		{
			var p = sh.Exec("cmd /d /c " + s);
			while(!p.StdOut.AtEndOfStream)
			{
				WScript.Echo(p.StdOut.ReadLine());
			}
			while(!p.StdErr.AtEndOfStream)
			{
				WScript.Echo(p.StdErr.ReadLine());
			}
			if(!no_error && p.ExitCode != 0)
			{
				WScript.Echo("Error " + p.ExitCode);
				WScript.Quit(p.ExitCode);
			}
		}
	}
}
