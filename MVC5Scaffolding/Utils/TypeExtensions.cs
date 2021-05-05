using System;
using System.CodeDom;
using Microsoft.CSharp;

namespace Happy.Scaffolding.MVC.Utils
{
    public static class TypeExtensions
    {
        public static string ToAliases(this Type type)
        {

            var compiler = new CSharpCodeProvider();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return compiler.GetTypeOutput(new CodeTypeReference(type.GetGenericArguments()[0])).Replace("System.", "") + "?";
            }
            else
            {
                return compiler.GetTypeOutput(new CodeTypeReference(type)).Replace("System.", "");
            }
        }

    }
}