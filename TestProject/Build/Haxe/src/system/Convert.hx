package system;

import js.html.ArrayBuffer;
import js.html.Int16Array;
import js.html.Int8Array;
import js.html.Int32Array;
import js.html.Uint16Array;
import js.html.Uint32Array;
import js.html.Uint8Array;

/**
 * ...
 * @author Danielku15
 */
class Convert 
{
	#if js
	
	private static var _conversionBuffer:ArrayBuffer = new ArrayBuffer(8);
	private static var _int8Buffer = new Int8Array(_conversionBuffer);
	private static var _uint8Buffer = new Uint8Array(_conversionBuffer);
	private static var _int16Buffer = new Int16Array(_conversionBuffer);
	private static var _uint16Buffer = new Uint16Array(_conversionBuffer);
	private static var _int32Buffer = new Int32Array(_conversionBuffer);
	private static var _uint32Buffer = new Uint32Array(_conversionBuffer);
	
	public static inline function toInt8(v:Int) : Int
	{
		_int32Buffer[0] = v;
		return _int8Buffer[0];
	}
	public static inline function toUInt8(v:Int) : Int 
	{
		_int32Buffer[0] = v;
		return _uint8Buffer[0];
	}
	public static inline function toInt16(v:Int) : Int
	{
		_int32Buffer[0] = v;
		return _int16Buffer[0];
	}
	public static inline function toUInt16(v:Int) : Int 
	{
		_int32Buffer[0] = v;
		return _uint16Buffer[0];
	}
	
	#end
}