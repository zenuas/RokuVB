Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Manager

    Public Class RkNativeFunction
        Inherits RkFunction

        Public Overridable Property [Operator] As RkOperator = RkOperator.Nop

        Public Overrides Function CreateCall(self As RkValue, ParamArray args() As RkValue) As RkCode0()

            Throw New NotSupportedException
        End Function

        Public Overrides Function CreateCallReturn(self As RkValue, return_ As RkValue, ParamArray args() As RkValue) As RkCode0()

            If args.Length <> Me.Arguments.Count Then Throw New ArgumentException("argument count")
            Dim x As New RkCode With {.Operator = Me.Operator, .Left = If(args.Length > 0, args(0), Nothing), .Right = If(args.Length > 1, args(1), Nothing), .Return = return_}
            Return New RkCode0() {x}
        End Function

        Public Overrides Function CloneGeneric() As IType

            Dim x = New RkNativeFunction With {.Name = Me.Name, .Operator = Me.Operator, .Namespace = Me.Namespace}
            x.Namespace.AddFunction(x)
            Return x
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} '{Me.Operator}'"
        End Function

    End Class

End Namespace
