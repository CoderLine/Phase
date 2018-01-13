package system;

private typedef ArrayData<T> = Array<T>;

/**
 * ...
 * @author Danielku15
 */
abstract FixedArray<T>(ArrayData<T>) 
{
	public function new(length:Int) this = untyped __new__(Array, length);
	
	@:from public static inline function fromArray<T>(a:Array<T>):FixedArray<T> return cast a;
	
	public var Length(get, never):Int;
	public inline function get_Length() return this.length;
	
	@:op([]) public inline function get(index:Int):T return this[index];
	@:op([]) public inline function set(index:Int, val:T):T return this[index] = val;
	public inline function GetEnumerator() : Iterable<T> return this;

	public static inline function empty<T>(size:Int) return new FixedArray<T>(size);
}