Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Manager

    Public Class RkNativeFunction
        Inherits RkFunction

        Public Overridable Property [Operator] As RkOperator = RkOperator.Nop

        Public Overrides Function CreateCall(ParamArray args() As RkValue) As RkCode0()

            Throw New NotSupportedException
        End Function

        Public Overrides Function CreateCallReturn(return_ As RkValue, ParamArray args() As RkValue) As RkCode0()

            If args.Length <> Me.Arguments.Count Then Throw New ArgumentException("argument count")
            Dim x As New RkCode With {.Operator = Me.Operator, .Left = args(0), .Right = args(1), .Return = return_}
            Return New RkCode0() {x}
        End Function

        Public Overrides Function CloneGeneric() As IType

            Return New RkNativeFunction With {.Name = Me.Name, .Operator = Me.Operator}
        End Function

    End Class

End Namespace
