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
[assembly: AssemblyDescription("General library of Microsoft TTS offline tools")]
[assembly: AssemblyCulture("")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b26cb973-be6d-4b68-8faa-5759fde2904e")]

[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
    "CA1016:MarkAssembliesWithAssemblyVersion")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
    "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Tts.Offline.Viterbi")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
    "CA2210:AssembliesShouldHaveValidStrongNames")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
    "CA1059:MembersShouldNotExposeCertainConcreteTypes", Scope = "member",
    Target = "DistributeComputing.NodeInfoEventArgs..ctor(" +
    "DistributeComputing.NodeInfo,System.Xml.XmlDocument)",
    MessageId = "System.Xml.XmlNode")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
    "CA1801:ReviewUnusedParameters", Scope = "member",
    Target = "Microsoft.Tts.Offline.Waveform.Fft.Transfer(" +
    "System.bool,System.Int16[],System.Single[]&):System.Void", MessageId = "invert")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
    "CA1016:MarkAssembliesWithAssemblyVersion")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
    "CA2201:DoNotRaiseReservedExceptionTypes", Scope = "member",
    Target = "Microsoft.Tts.Offline.Utility.CommandLine.RunCommand(" +
    "System.string,System.string,System.string,System.IO.TextWriter," +
    "System.IO.TextWriter,System.Threading.WaitHandle):System.int")]