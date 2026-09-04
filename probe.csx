using System;
using System.Reflection;
using System.Linq;

var asm = Assembly.LoadFrom(@""C:\Users\osama\.nuget\packages\microsoft.openapi\2.7.5\lib\net8.0\Microsoft.OpenApi.dll"");
var types = asm.GetTypes().Where(t => t.IsPublic && (
    t.Name.Contains(""OpenApiInfo"") ||
    t.Name.Contains(""SecurityScheme"") ||
    t.Name.Contains(""SecurityRequirement"") ||
    t.Name.Contains(""SecuritySchemeType"") ||
    t.Name.Contains(""ParameterLocation"") ||
    t.Name.Contains(""ReferenceType"")
));
foreach (var t in types.OrderBy(t => t.FullName))
    Console.WriteLine(t.FullName);
