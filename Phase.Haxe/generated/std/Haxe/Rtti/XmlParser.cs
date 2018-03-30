//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Rtti
{
    [External]
    [Name("haxe.rtti.XmlParser")]
    public partial class XmlParser
    {
        [Name("root")]
        public virtual extern Haxe.Rtti.TypeRoot Root { get; set; }
        [Name("curplatform")]
        protected virtual extern Haxe.HaxeString Curplatform { get; set; }
        [Name("sort")]
        public virtual extern void Sort(Haxe.Rtti.TypeRoot l = default(Haxe.Rtti.TypeRoot));
        [Name("sortFields")]
        protected virtual extern Haxe.root.List<Haxe.Rtti.ClassField> SortFields(Haxe.root.Iterable<Haxe.Rtti.ClassField> fl);
        [Name("process")]
        public virtual extern void Process(Haxe.root.Xml x, string platform);
        [Name("mergeRights")]
        protected virtual extern Haxe.HaxeBool MergeRights(Haxe.Rtti.ClassField f1, Haxe.Rtti.ClassField f2);
        [Name("mergeDoc")]
        protected virtual extern Haxe.HaxeBool MergeDoc(Haxe.Rtti.ClassField f1, Haxe.Rtti.ClassField f2);
        [Name("mergeFields")]
        protected virtual extern Haxe.HaxeBool MergeFields(Haxe.Rtti.ClassField f, Haxe.Rtti.ClassField f2);
        [Name("newField")]
        public virtual extern void NewField(Haxe.Rtti.Classdef c, Haxe.Rtti.ClassField f);
        [Name("mergeClasses")]
        protected virtual extern Haxe.HaxeBool MergeClasses(Haxe.Rtti.Classdef c, Haxe.Rtti.Classdef c2);
        [Name("mergeEnums")]
        protected virtual extern Haxe.HaxeBool MergeEnums(Haxe.Rtti.Enumdef e, Haxe.Rtti.Enumdef e2);
        [Name("mergeTypedefs")]
        protected virtual extern Haxe.HaxeBool MergeTypedefs(Haxe.Rtti.Typedef t, Haxe.Rtti.Typedef t2);
        [Name("mergeAbstracts")]
        protected virtual extern Haxe.HaxeBool MergeAbstracts(Haxe.Rtti.Abstractdef a, Haxe.Rtti.Abstractdef a2);
        [Name("merge")]
        protected virtual extern void Merge(Haxe.Rtti.TypeTree t);
        [Name("mkPath")]
        protected virtual extern Haxe.Rtti.Path MkPath(string p);
        [Name("mkTypeParams")]
        protected virtual extern Haxe.Rtti.TypeParams MkTypeParams(string p);
        [Name("mkRights")]
        protected virtual extern Haxe.Rtti.Rights MkRights(string r);
        [Name("xerror")]
        protected virtual extern dynamic Xerror(Haxe.Xml.Fast c);
        [Name("xroot")]
        protected virtual extern void Xroot(Haxe.Xml.Fast x);
        [Name("processElement")]
        public virtual extern Haxe.Rtti.TypeTree ProcessElement(Haxe.root.Xml x);
        [Name("xmeta")]
        protected virtual extern Haxe.Rtti.MetaData Xmeta(Haxe.Xml.Fast x);
        [Name("xoverloads")]
        protected virtual extern Haxe.root.List<Haxe.Rtti.ClassField> Xoverloads(Haxe.Xml.Fast x);
        [Name("xpath")]
        protected virtual extern Haxe.Rtti.PathParams Xpath(Haxe.Xml.Fast x);
        [Name("xclass")]
        protected virtual extern Haxe.Rtti.Classdef Xclass(Haxe.Xml.Fast x);
        [Name("xclassfield")]
        protected virtual extern Haxe.Rtti.ClassField Xclassfield(Haxe.Xml.Fast x, bool defPublic = false);
        [Name("xenum")]
        protected virtual extern Haxe.Rtti.Enumdef Xenum(Haxe.Xml.Fast x);
        [Name("xenumfield")]
        protected virtual extern Haxe.Rtti.EnumField Xenumfield(Haxe.Xml.Fast x);
        [Name("xabstract")]
        protected virtual extern Haxe.Rtti.Abstractdef Xabstract(Haxe.Xml.Fast x);
        [Name("xtypedef")]
        protected virtual extern Haxe.Rtti.Typedef Xtypedef(Haxe.Xml.Fast x);
        [Name("xtype")]
        protected virtual extern Haxe.Rtti.CType Xtype(Haxe.Xml.Fast x);
        [Name("xtypeparams")]
        protected virtual extern Haxe.root.List<Haxe.Rtti.CType> Xtypeparams(Haxe.Xml.Fast x);
        [Name("defplat")]
        protected virtual extern Haxe.root.List<Haxe.HaxeString> Defplat();
        [Name("new")]
        public virtual extern void New();
    }
}
