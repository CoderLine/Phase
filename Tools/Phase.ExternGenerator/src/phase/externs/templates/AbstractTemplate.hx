package phase.externs.templates;
import haxe.rtti.CType.Abstractdef;
import haxe.rtti.CType.Classdef;
import haxe.rtti.CType.ClassField;

class AbstractTemplate extends TemplateBase
{
	public var data:Abstractdef;
	
	public function new(generator:Generator, c:Abstractdef)
	{
		super(generator, c.path);
		data = c;
	}
	
	public override function generateType() 
	{
		writeLine("[External]");
		writeLine("[Abstract]");
		writeLine('[Name("$haxePath")]');
		
		write("public ");
		if (data.isInterface)
		{
			write("interface ");
		}
		else
		{
			write("partial class ");
		}
		write(name);
		
		if (data.params.length > 0)
		{
			write("<");
			writeIterable(data.params, function(item) {
				write(item);
			});
			write(">");
		}
		
		writeNewLine();
		indent();
		
		if (data.superClass != null)
		{
			write(": ");
			writePathParams(data.superClass);
			writeNewLine();
		}
		
		if(data.interfaces.length > 0)
		{
			if (data.superClass == null)
			{
				write(": ");
			}
			else 
			{
				write(", ");
			}
			writeIterable(data.interfaces, function(i) {
				writePathParams(i);
				writeNewLine();
			});
		}
		outdent();
		
		beginBlock();
		
		_currentFieldNames = new Array<String>();
		for (field in data.fields)
		{
			_currentFieldNames.push(field.name);
		}
		for (field in data.statics)
		{
			_currentFieldNames.push(field.name);
		}
		
		for (field in data.fields)
		{
			generateField(field, false);
		}
		
		for (field in data.statics)
		{
			generateField(field, true);
		}
		
		_currentFieldNames = null;
		
		endBlock();
	}
	
	private function generateField(f:ClassField, isStatic:Bool)
	{
		writeLine('[Name("${f.name}")]');
		
		if(f.isPublic) 
		{
			write("public ");
		}
		else if (isStatic)
		{
			write("private ");
		}
		else 
		{
			write("protected ");
		}
		if(isStatic) 
		{
			write("static ");
		}	
		else
		{
			if(f.isOverride) 
			{
				write("override ");
			}
			else
			{
				write("virtual ");
			}
		}
		write("extern ");
		
		_dynamicAsObject = false;		
		writeType(getFieldType(f));
		_dynamicAsObject = true;
		write(" ");
		writeName(toCamelCase(f.name), false);
		
		if (isMethod(f))
		{
			if (f.params.length > 0)
			{
				write("<");
				writeIterable(f.params, function(item) {
					write(item);
				});
				write(">");
			}
			
			writeMethod(f);
		}
		else
		{
			write(" {");
			
			switch(f.get)
			{
				case RNo:
					write(" private get;");
				case RNormal,RMethod,RDynamic,RInline:
					write(" get;");
				case RCall( m ):
					write(" get;");
			}
			
			switch(f.set)
			{
				case RNo:
					write(" private set;");
				case RNormal,RMethod,RDynamic,RInline:
					write(" set;");
				case RCall( m ):
					write(" set;");
			}
			
			write(" }");
			writeNewLine();
		}
		
		if (f.overloads != null)
		{
			for (overload in f.overloads)
			{
				overload.isPublic = f.isPublic;
				overload.isOverride = f.isOverride;
				overload.name = f.name;
				generateField(overload, isStatic);
			}
		}
	}
	
	private function writeMethod(f:ClassField)
	{
		write("(");
		
		var parameters = getMethodParameters(f);
		writeIterable(parameters, function(param) {
			
			_useStandardTypes = true;
			writeType(param.t);
			write(" ");
			writeName(param.name, false);
			
			if (param.opt)
			{
				if (param.value != null)
				{
					write(" = ");
					write(param.value);
				}
				else
				{
					write(" = default(");
					writeType(param.t);
					write(")");
				}
			}
			_useStandardTypes = false;
		});
		
		write(");");
		writeNewLine();
	}
}