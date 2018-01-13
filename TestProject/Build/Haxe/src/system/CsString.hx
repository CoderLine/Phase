package system;

abstract CsString(String) from String to String
{
	public inline function new(s:String) this = s;	
	
	public inline function ToHaxeString(): String return this;
	
	public var Length(get, never):Int32;
	public inline function get_Length() :Int32 return this.length;
	public inline function get_Chars(i:Int32) : Char return Char.fromCode(this.charCodeAt(i));
	
	public inline function Substring_Int32(start:Int32) : CsString return this.substr(start);
	public inline function Substring_Int32_Int32(start:Int32, length:Int32) : CsString return this.substr(start, length);
	public inline function Replace_CsString_CsString(from:CsString, with :CsString) : CsString return StringTools.replace(this, from, with );
	public inline function IndexOf_Char(ch:Int32) : Int32 return this.indexOf(String.fromCharCode(ch));
	public inline function LastIndexOf_Char(ch:Char) : Int32 return this.lastIndexOf(String.fromCharCode(ch));
	public inline function EndsWith_CsString(end:String) : Boolean return StringTools.endsWith(this, end);
	public inline function Contains(s:String) : Boolean return this.indexOf(s) != -1;
	public inline function Trim() : CsString return StringTools.trim(this);
	
	public function Split_CharArray(chars:Array<Int32>) : Array<CsString>
	{
		var strings = new Array<CsString>();
		var startPos = 0;
		for (i in 0 ... this.length)
		{
			var cc = this.charCodeAt(i);
			if (chars.indexOf(cc) >= 0)
			{
				var endPos = i - 1;
				if (endPos < startPos)
				{
					strings.push("");
				}
				else
				{
					strings.push(this.substring(startPos, endPos));
				}
				startPos = i + 1;
			}
		}
		return strings;
	}
	
	public static inline function IsNullOrEmpty(s:CsString) : Boolean return (s == null || s.Length == 0);
	public static inline function IsNullOrWhiteSpace(s:CsString) : Boolean return (s == null || s.Trim().Length == 0);
	public static inline function FromCharCode(s:Int32) : CsString return String.fromCharCode(s);

	public inline function StartsWith_CsString(s:CsString) : Boolean return StringTools.startsWith(this, s); 
	
	public inline function ToLower() :CsString return this.toLowerCase();
	public inline function ToUpper() :CsString return this.toUpperCase();
	
	@:op(A + B) public static inline function add0(lhs : system.CsString, rhs : system.CsString) : system.CsString return lhs.ToHaxeString() + rhs.ToHaxeString();
	@:op(A + B) public static inline function add1(lhs : system.CsString, rhs : String) : system.CsString return lhs.ToHaxeString() + rhs;
	@:op(A + B) public static inline function add2(lhs : String, rhs : system.CsString) : system.CsString return lhs + rhs.ToHaxeString();
	@:op(A + B) public static inline function add3(lhs : system.CsString, rhs : Int32) : system.CsString return lhs.ToHaxeString() + Std.string(rhs);
	@:op(A + B) public static inline function add4(lhs : Int32, rhs : system.CsString) : system.CsString  return Std.string(lhs) + rhs.ToHaxeString();
	@:op(A + B) public static inline function add5(lhs : system.CsString, rhs : Single) : system.CsString  return lhs.ToHaxeString() + Std.string(rhs);
	@:op(A + B) public static inline function add6(lhs : Single, rhs : system.CsString) : system.CsString  return Std.string(lhs) + rhs.ToHaxeString();
	@:op(A + B) public static inline function add7(lhs : system.CsString, rhs : system.Byte) : system.CsString  return lhs.ToHaxeString() + Std.string(rhs.ToHaxeInt());
	@:op(A + B) public static inline function add8(lhs : system.Byte, rhs : system.CsString) : system.CsString  return Std.string(lhs.ToHaxeInt()) + rhs.ToHaxeString();

}