
(function ()
{
	var fs = WScript.CreateObject("Scripting.FileSystemObject");
	
	var a = fs.GetFile("roku.y");
	var b = fs.GetFile("Compiler/MyParser.vb");
	
	function mv(from, to)
	{
		rm(to);
		fs.MoveFile(from, to);
	}
	
	function rm(f)
	{
		fs.FileExists(f) && fs.DeleteFile(f, true);
	}
	
	function start(cmd)
	{
		var sh = WScript.CreateObject("WScript.Shell");
		
		sh.Run(cmd, 0, true);
	}
	
	if(a.DateLastModified > b.DateLastModified)
	{
		start("yanp.bat");
	}
})();

