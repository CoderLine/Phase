package phase.externs;
import haxe.rtti.CType.TypeRoot;
import haxe.rtti.CType.TypeTree;

using Lambda;

class Parser 
{
	public var xmlPath(default, default) :String;
	public var platform(default, default) :String;
	public var root(default,null) :TypeRoot;
	
	private var _pathFilters:Array<Filter>;
	
	public function new() 
	{
		_pathFilters = new Array<Filter>();		
	}
	
	public function parse()
	{
		Sys.println('Parsing $xmlPath');
		var parser = new haxe.rtti.XmlParser();
		var data = sys.io.File.getContent(xmlPath);
		var xml = Xml.parse(data).firstElement();
		parser.process(xml, platform);
		Sys.println('Parsing done, processing types');

		process(parser.root);
		
		Sys.println('Processing done');
	}
	
	public function addFilter(pattern:String, isIncludeFilter:Bool)
	{
		_pathFilters.push(new Filter(pattern, isIncludeFilter));
	}
	
	private function process(r:TypeRoot)
	{
		root = new TypeRoot();
		for (x in r)
		{
			processTree(x);
		}
	}
	
	private function processTree(tree:TypeTree)
	{
		switch(tree)
		{
			case TPackage(name, full, subs):
				if (name.charAt(0) == '_' || isPathFiltered(full)) return;
				subs.iter(processTree);
			case TClassdecl(c):
				if (isPathFiltered(c.path)) return;
				root.push(tree);
			case TEnumdecl(e):
				if (isPathFiltered(e.path)) return;
				root.push(tree);
			case TTypedecl(t):
				if (isPathFiltered(t.path)) return;
				root.push(tree);
			case TAbstractdecl(a):
				if (isPathFiltered(a.path)) return;
				root.push(tree);
			default:
		}
	}
	private function isPathFiltered(path:String):Bool
	{
		if (path == "Int")
		{
			return true;
		}
		if (path == "Float")
		{
			return true;
		}
		if (path == "Bool")
		{
			return true;
		}
		if (path == "Array")
		{
			return true;
		}
		if (path == "String")
		{
			return true;
		}
		var hasInclusionFilter = false;
		for (filter in _pathFilters) 
		{
			if (filter.isIncludeFilter) hasInclusionFilter = true;
			if (filter.match(path)) return !filter.isIncludeFilter;
		}
		return hasInclusionFilter;
	}
}