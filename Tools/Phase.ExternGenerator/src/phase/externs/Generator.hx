package phase.externs;
import haxe.io.Path;
import haxe.rtti.CType.TypeRoot;
import haxe.rtti.CType.TypeTree;
import phase.externs.templates.AbstractTemplate;
import phase.externs.templates.ClassTemplate;
import phase.externs.templates.EnumTemplate;
import phase.externs.templates.TemplateBase;
import phase.externs.templates.TypeDefTemplate;
import sys.FileSystem;
import sys.io.File;

using Lambda;


class Generator 
{
	public var outputPath(default, default):String;
	public var templatePath(default, default):String;
	public var toplevelPackage(default, default):String;
	public var libraryNamespace(default, default):String;
	
	public function new() 
	{
	}
	public function generate(root:TypeRoot)
	{
		Sys.println('Cleaning ouput');
		deleteDirectory(outputPath);	
		
		Sys.println('Start generation');
		for (tree in root)
		{
			generateTree(tree);
		}
	}
	
	private function deleteDirectory(path:String)
	{
		if (FileSystem.exists(path) && FileSystem.isDirectory(path))
		{
			var entries = FileSystem.readDirectory(path);
			for (entry in entries) 
			{
				var subPath = Path.join([path, entry]);
				if (FileSystem.isDirectory(subPath))
				{
					deleteDirectory(subPath);
				}
				else
				{
					FileSystem.deleteFile(path + '/' + entry);
				}
			}
			FileSystem.deleteDirectory(path);
		}
	}
	
	private function generateTree(tree:TypeTree)
	{
		switch(tree)
		{
			case TPackage(name, full, subs):
				if (name.charAt(0) == '_') return;
				subs.iter(generateTree);
			case TClassdecl(c):
				render(new ClassTemplate(this, c));
			case TEnumdecl(e):
				render(new EnumTemplate(this, e));
			case TTypedecl(t):
				render(new TypeDefTemplate(this, t));
			case TAbstractdecl(a):
				render(new AbstractTemplate(this, a));
			default:
		}
	}
	
	private function render(template:TemplateBase)
	{
		Sys.println('  Generating type "'+template.haxePath+'"');
		
		var content = template.generate();
		
		var directory = Path.join([outputPath].concat(template.namespaceParts));
		var filePath = Path.join([directory, template.name + ".cs"]);
		
		FileSystem.createDirectory(directory);		
		File.saveContent(filePath, content);
	}
}