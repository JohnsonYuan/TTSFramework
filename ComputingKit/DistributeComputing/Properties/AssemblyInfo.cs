//----------------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines assembly data
// </summary>
//----------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyDescription("Library of computing kit")]
[assembly: AssemblyCulture("")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("fcdbc01e-c8bc-404c-bc86-ea3b55966fda")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
    "CA1059:MembersShouldNotExposeCertainConcreteTypes", Scope = "member",
    Target = "DistributeComputing.NodeInfoEventArgs..ctor("
    + "DistributeComputing.NodeInfo,System.Xml.XmlDocument)",
    MessageId = "System.Xml.XmlNode")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
    "CA2210:AssembliesShouldHaveValidStrongNames")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
    "CA1016:MarkAssembliesWithAssemblyVersion")]
