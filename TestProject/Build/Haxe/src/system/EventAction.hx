package system;

abstract EventAction(Array<Void->Void>) 
{
	public inline function new(v:Void->Void) this = v == null ? null : [v];
	
	@:to public inline function ToLambda() : Void->Void return Invoke;
	
	public function Invoke() : Void
	{
		for (x in this)
		{
			x();
		}
	}
}