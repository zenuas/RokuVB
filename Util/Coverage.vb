Imports System.Diagnostics


Namespace Util

    Public Class Coverage

        <Conditional("TRACE")>
        Public Shared Sub [Case](Optional s As String = "")

            Dim frame As New StackFrame(1, True)
            Trace.WriteLine($"Coverage.Case:{frame.GetFileName}:{frame.GetType.FullName}:{frame.GetMethod}:{frame.GetFileLineNumber}:{s}:{String.Join(" ", System.Environment.GetCommandLineArgs)}")
        End Sub

    End Class

End Namespace
