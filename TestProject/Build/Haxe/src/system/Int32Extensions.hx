package system;

class Int32Extensions 
{
	public static inline function ToString(i:Int32) : CsString return Std.string(i);
	public static inline function CompareTo_Int32(a:Int32, b:Int32) : Int32 {
		if (a < b) return -1;
		if (a > b) return 1;
		return 0;
	}
}