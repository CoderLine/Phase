package system;

abstract EventAction1<T1>(Array<T1->Void>) 
{
	public inline function new(v:T1->Void) this = v == null ? null : [v];
	
	@:to public inline function ToLambda() : T1->Void return Invoke;
	
	public function Invoke(p:T1) : Void
	{
		for (x in this)
		{
			x(p);
		}
	}
}