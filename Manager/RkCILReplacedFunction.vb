Imports System
Imports Roku.Manager.SystemLibrary


Namespace Manager

    Public Class RkCILReplacedFunction
        Inherits RkCILFunction

        Public Overridable Property ReplacedFunction As Func(Of IType(), IFunction)

        Public Overrides Function CloneGeneric() As IType

            Dim x = New RkCILReplacedFunction With {.Name = Me.Name, .Scope = Me.Scope, .MethodInfo = Me.MethodInfo, .GenericBase = Me, .ReplacedFunction = Me.ReplacedFunction}
            Me.CopyGeneric(x)
            x.Scope.AddFunction(x)
            Return x
        End Function

        Public Overrides Function ApplyFunction(target As IScope, ParamArray args() As IType) As IFunction

            Dim t = MyBase.ApplyFunction(target, args)
            If t.HasGeneric Then Return t

            Dim self = FixedByName(args(0))
            If TypeOf self Is RkCILStruct AndAlso CType(self, RkCILStruct).TypeInfo.IsArray Then

                Return Me.ReplacedFunction(args)
            End If

            Return t
        End Function

    End Class

End Namespace

