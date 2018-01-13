package alphaTab.collections;
import system.FixedArray;
abstract FastList<T>(Array<T>) from Array<T> to Array<T>
{
	public function new() this = new Array<T>();
	
	public var Count(get, never):Int;
	public inline function get_Count() return this.length;	
	public inline function get_Item(index:Int) return this[index];	
	public inline function set_Item(index:Int, value:T) return this[index] = value;
	public inline function Add(t:T) this.push(t);	
	public inline function Clone() return this.slice(0);
	public inline function RemoveAt(index:Int) : Void if (index != -1) this.splice(index, 1);
	public inline function Remove(val:T) RemoveAt(IndexOf(val));
	public inline function GetEnumerator() : Iterable<T> return this;
	public inline function IndexOf(t:T) return this.indexOf(t);
	public inline function ToArray() return FixedArray.fromArray(this);
	public inline function Sort(f:T->T->Int) this.sort(f);
	public inline function Reverse() this.reverse();
}
