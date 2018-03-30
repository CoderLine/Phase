package phase;

class Filter 
{
	public var isIncludeFilter:Bool;
	private var _regex:EReg;
	
	public function new(pattern:String, isIncludeFilter:Bool) 
	{
		this.isIncludeFilter = isIncludeFilter;
		_regex = new EReg(pattern, "");	
	}
	
	public function match(path:String) return _regex.match(path);
}