//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a the Phase Extern Generator
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Phase;
using Phase.Attributes;
namespace Haxe.Remoting
{
    [External]
    [Name("haxe.remoting.AsyncProxy")]
    public partial class AsyncProxy<T>
    {
        [Name("__cnx")]
        protected virtual extern Haxe.Remoting.AsyncConnection __cnx { get; set; }
        [Name("new")]
        protected virtual extern void New(Haxe.Remoting.AsyncConnection c);
    }
}
