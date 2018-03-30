package phase.externs.templates;

class CodeWriter 
{
	private var _output:StringBuf;
	private var _level:Int;

	public var isNewLine:Bool;

	public function new() 
	{
		_output = new StringBuf();
		_level = 0;
		isNewLine = true;
	}
	
	public function indent()
	{
		_level++;
	}

	public function outdent()
	{
		if (_level > 0)
		{
			_level--;
		}
	}

	public function writeIndent()
	{
		if (!isNewLine)
		{
			return;
		}

		for( i in 0 ... _level)
		{
			_output.add("    ");
		}

		isNewLine = false;
	}

	public function writeNewLine()
	{
		_output.add("\r\n");
		isNewLine = true;
	}

	public function beginBlock()
	{
		write("{");
		writeNewLine();
		indent();
	}

	public function endBlock(newline:Bool = true)
	{
		outdent();
		write("}");
		if (newline)
		{
			writeNewLine();
		}
	}

	public function write<T>(value:T)
	{
		writeIndent();
		_output.add(value);
	}

	public function writeLine<T>(value:T)
	{
		writeIndent();
		_output.add(value);
		writeNewLine();
	}
	
	public function writeIterable<T>(iterable:Iterable<T>, writeItem:T->Void, separator:String = ", ")
	{
		var i = 0; 
		for (item in iterable)
		{
			if (i > 0) write(separator);
			writeItem(item);
			i++;
		}
	}
	
	public function toString() : String
	{
		return _output.toString();
	}
}