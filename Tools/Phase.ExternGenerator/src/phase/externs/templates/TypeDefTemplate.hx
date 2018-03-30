package phase.externs.templates;
import haxe.rtti.CType.Typedef;

class TypeDefTemplate extends TemplateBase
{
	public var data:Typedef;
	
	public function new(generator:Generator, c:Typedef)
	{
		super(generator, c.path);
		data = c;
	}
}