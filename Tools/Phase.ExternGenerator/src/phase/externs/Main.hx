package phase.externs;
import sys.FileSystem;

class Main 
{
	
	static function main() 
	{
		var help:Bool = false;
		
		var generator = new Generator();
		var parser = new Parser();
		
		var argHandler = hxargs.Args.generate([
			@doc("Set the output path for generated source files")
			["-o", "--output-path"] => function(v:String) generator.outputPath = v,

			@doc("Set the xml input path")
			["-i", "--input-path"] => function(v:String) parser.xmlPath = v,

			@doc("Sets the template path")
			["-t", "--template-path"] => function(path:String) generator.templatePath = path,
			
			@doc("Sets the platform for which the classes should be generated")
			["-p", "--platform"] => function(v:String) parser.platform = v,
			
			@doc("Set the package which serves for top-level types")
			["--toplevel-package"] => function(dotPath:String) generator.toplevelPackage = dotPath,
			
			@doc("Set the root namespace into which the classes are generated in package structure")
			["-ns", "--library-namespace"] => function(dotPath:String) generator.libraryNamespace = dotPath,
						
			@doc("Package Filter")
			["-tpkg"] => function(dotPath:String) generator.toplevelPackage = dotPath,

			@doc("Add a path include filter")
			["-in", "--include"] => function(regex:String) parser.addFilter(regex, true),

			@doc("Add a path exclude filter")
			["-ex", "--exclude"] => function(regex:String) parser.addFilter(regex, false),			

			@doc("Display this list of options")
			["-help", "--help"] => function() help = true,

			_ => function(arg:String) throw "Unknown command: " + arg
		]);
		argHandler.parse(Sys.args());
		
		if (!FileSystem.exists(parser.xmlPath))
		{
			Sys.println("Could not find input file: " + parser.xmlPath);
			help = true;
		}
		
		if (generator.outputPath == "" || generator.outputPath == null)
		{
			Sys.println("Output directory not specified");
			help = true;
		}
		
		if (parser.platform == "" || parser.platform == null)
		{
			Sys.println("Platform not specified");
			help = true;
		}
		
		if (help == true)
		{
			Sys.println("Phase Extern generator");
			Sys.println(argHandler.getDoc());
			Sys.exit(0);
		}

		try
		{
			parser.parse();
		}
		catch (e:Dynamic)
		{
			Sys.println("Failed to load input XML:");
			Sys.println(e);
			Sys.exit(1);
		}
		
		try
		{
			generator.generate(parser.root);
		}
		catch (e:Dynamic)
		{
			Sys.println("Failed to generate output files:");
			Sys.println(e);
			Sys.println(haxe.CallStack.toString(haxe.CallStack.exceptionStack()));
			Sys.exit(1);
		}
	}
}