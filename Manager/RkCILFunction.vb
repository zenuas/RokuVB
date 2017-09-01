Imports System.Reflection


Namespace Manager

    Public Class RkCILFunction
        Inherits RkFunction

        Public Overridable Property MethodInfo As MethodInfo

        Public Overrides Function CloneGeneric() As IType

            Dim x = New RkCILFunction With {.Name = Me.Name, .Scope = Me.Scope, .MethodInfo = Me.MethodInfo, .GenericBase = Me}
            x.Scope.AddFunction(x)
            Return x
        End Function

    End Class

End Namespace

