package alphaTab.collections;

class FastDictionaryKeyIterator<TKey, TValue>
{
	private var _dict:Map<TKey, TValue> ;
	public function new( dict : Map<TKey, TValue>) 
	{
		_dict = dict;
	}
	public inline function iterator() : Iterator<TKey> return _dict.keys();
}