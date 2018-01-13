package system;

abstract Char(Int) from Int to Int
{
	public function new(v:Int) this = v;
	@:from public static inline function fromCode(i:Int) return new Char(i);
}