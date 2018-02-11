package system;

class UInt16ArrayIterator
{
	private var _array:js.html.Uint16Array;
	private var _i:Int;
		
	public function new(array:js.html.Uint16Array) 
	{
		_array = array;
		_i = 0;
	}

	public function hasNext() return _i < _array.length - 1;
	public function next() 
	{
		return _array[_i++];
	}
}