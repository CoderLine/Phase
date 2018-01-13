package system;

abstract EventAction2<T1, T2>(Array<T1->T2->Void>) 
{
	public inline function new(v:T1->T2->Void) this = v == null ? null : [v];
	
	@:to public inline function ToLambda() : T1->T2->Void return Invoke;
	
	public function Invoke(p1:T1, p2:T2) : Void
	{
		for (x in this)
		{
			x(p1, p2);
		}
	}
}