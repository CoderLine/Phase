
package phase.externs.templates;
import haxe.rtti.CType.Enumdef;

class EnumTemplate extends TemplateBase
{
	public var data:Enumdef;
	
	public function new(generator:Generator, c:Enumdef)
	{
		super(generator, c.path);
		data = c;
	}
}