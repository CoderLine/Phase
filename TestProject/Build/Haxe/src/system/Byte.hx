package system;


abstract Byte(Int) from Int to Int
{
	public inline function new(i:Int) this = system.Convert.toUInt8(i);
	
	public inline function ToHaxeInt(): Int return this;
	
	@:op(-A) public inline function neg() : Int return -this;

    @:op(~A)public inline function not() : Int return ~this;

    @:op(A++)public inline function postinc() : system.Byte return this++;
    @:op(++A)public inline function preinc() : system.Byte return ++this;

    @:op(A--)public inline function postdec() : system.Byte return this--;
    @:op(--A)public inline function predec() : system.Byte return --this;

	@:op(A * B) public static function mul0(lhs : system.Byte, rhs : system.Byte) : Int;
	@:op(A * B) public static function mul1(lhs : system.Byte, rhs : Int) : Int;
    @:op(A * B) public static function mul2(lhs : Int, rhs : system.Byte) : Int;
    @:op(A * B) public static function mul3(lhs : system.Byte, rhs : Float) : Float;
    @:op(A * B) public static function mul4(lhs : Float, rhs : system.Byte) : Float;

    @:op(A / B) public static function div0(lhs : system.Byte, rhs : system.Byte) : Int return Std.int(lhs.ToHaxeInt() / rhs.ToHaxeInt());
    @:op(A / B) public static function div1(lhs : system.Byte, rhs : Int) : Int return Std.int(lhs.ToHaxeInt() / rhs);
    @:op(A / B) public static function div2(lhs : Int, rhs : system.Byte) : Int return Std.int(lhs / rhs.ToHaxeInt());
    @:op(A / B) public static function div3(lhs : system.Byte, rhs : Float) : Float;
    @:op(A / B) public static function div4(lhs : Float, rhs : system.Byte) : Float;

    @:op(A % B) public static function mod0(lhs : system.Byte, rhs : system.Byte) : Int;
    @:op(A % B) public static function mod1(lhs : system.Byte, rhs : Int) : Int;
    @:op(A % B) public static function mod2(lhs : Int, rhs : system.Byte) : Int;
    @:op(A % B) public static function mod3(lhs : system.Byte, rhs : Float) : Float;
    @:op(A % B) public static function mod4(lhs : Float, rhs : system.Byte) : Float;
                              
    @:op(A + B) public static function add0(lhs : system.Byte, rhs : system.Byte) : Int;
    @:op(A + B) public static function add1(lhs : system.Byte, rhs : Int) : Int;
    @:op(A + B) public static function add2(lhs : Int, rhs : system.Byte) : Int;
    @:op(A + B) public static function add3(lhs : system.Byte, rhs : Float) : Float;
    @:op(A + B) public static function add4(lhs : Float, rhs : system.Byte) : Float;
                              
    @:op(A - B) public static function sub0(lhs : system.Byte, rhs : system.Byte) : Int;
    @:op(A - B) public static function sub1(lhs : system.Byte, rhs : Int) : Int;
    @:op(A - B) public static function sub2(lhs : Int, rhs : system.Byte) : Int;
    @:op(A - B) public static function sub3(lhs : system.Byte, rhs : Float) : Float;
    @:op(A - B) public static function sub4(lhs : Float, rhs : system.Byte) : Float;

    @:op(A << B) public static function shl0(lhs : system.Byte, rhs : system.Byte) : Int;
    @:op(A << B) public static function shl1(lhs : system.Byte, rhs : Int) : Int;
    @:op(A << B) public static function shl2(lhs : Int, rhs : system.Byte) : Int;

    @:op(A > B) public static function gt0(lhs : system.Byte, rhs : Int) : Bool;
    @:op(A > B) public static function gt1(lhs : system.Byte, rhs : Int) : Bool;
    @:op(A > B) public static function gt2(lhs : Int, rhs : system.Byte) : Bool;
    @:op(A > B) public static function gt3(lhs : system.Byte, rhs : Float) : Bool;
    @:op(A > B) public static function gt4(lhs : Float, rhs : system.Byte) : Bool;

    @:op(A < B) public static function lt0(lhs : system.Byte, rhs : system.Byte) : Bool;
    @:op(A < B) public static function lt1(lhs : system.Byte, rhs : Int) : Bool;
	@:op(A < B) public static function lt2(lhs : Int, rhs : system.Byte) : Bool;
    @:op(A < B) public static function lt3(lhs : system.Byte, rhs : Float) : Bool;
    @:op(A < B) public static function lt4(lhs : Float, rhs : system.Byte) : Bool;

    @:op(A >= B) public static function gte0(lhs : system.Byte, rhs : system.Byte) : Bool;
    @:op(A >= B) public static function gte1(lhs : system.Byte, rhs : Int) : Bool;
    @:op(A >= B) public static function gte2(lhs : Int, rhs : system.Byte) : Bool;
    @:op(A >= B) public static function gte3(lhs : system.Byte, rhs : Float) : Bool;
    @:op(A >= B) public static function gte4(lhs : Float, rhs : system.Byte) : Bool;

    @:op(A <= B) public static function lte0(lhs : system.Byte, rhs : system.Byte) : Bool;
    @:op(A <= B) public static function lte1(lhs : system.Byte, rhs : Int) : Bool;
    @:op(A <= B) public static function lte2(lhs : Int, rhs : system.Byte) : Bool;
    @:op(A <= B) public static function lte3(lhs : system.Byte, rhs : Float) : Bool;
    @:op(A <= B) public static function lte4(lhs : Float, rhs : system.Byte) : Bool;

    @:op(A == B) public static function eq0(lhs : system.Byte, rhs : system.Byte) : Bool;
    @:op(A == B) public static function eq1(lhs : system.Byte, rhs : Int) : Bool;
    @:op(A == B) public static function eq2(lhs : Int, rhs : system.Byte) : Bool;
    @:op(A == B) public static function eq3(lhs : system.Byte, rhs : Float) : Bool;
    @:op(A == B) public static function eq4(lhs : Float, rhs : system.Byte) : Bool;

    @:op(A != B) public static function neq0(lhs : system.Byte, rhs : system.Byte) : Bool;
    @:op(A != B) public static function neq1(lhs : system.Byte, rhs : Int) : Bool;
    @:op(A != B) public static function neq2(lhs : Int, rhs : system.Byte) : Bool;
    @:op(A != B) public static function neq3(lhs : system.Byte, rhs : Float) : Bool;
    @:op(A != B) public static function neq4(lhs : Float, rhs : system.Byte) : Bool;

    @:op(A & B) public static function and0(lhs : system.Byte, rhs : system.Byte) : Int;
    @:op(A & B) public static function and1(lhs : system.Byte, rhs : Int) : Int;
    @:op(A & B) public static function and2(lhs : Int, rhs : system.Byte) : Int;
                              
    @:op(A | B) public static function or0(lhs : system.Byte, rhs : system.Byte) : Int;
    @:op(A | B) public static function or1(lhs : system.Byte, rhs : Int) : Int;
    @:op(A | B) public static function or2(lhs : Int, rhs : system.Byte) : Int;
                              
    @:op(A ^ B) public static function xor1(lhs : system.Byte, rhs : Int) : Int;
    @:op(A ^ B) public static function xor2(lhs : Int, rhs : system.Byte) : Int;
}