Imports System.Collections.Generic
Imports Roku.Manager
Imports Roku.Operator
Imports Roku.Util.Extensions


Namespace IntermediateCode

    Public Class InCall
        Inherits InCode0
        Implements IReturnBind

        Public Sub New()

            Me.Operator = InOperator.Call
        End Sub

        Public Overridable ReadOnly Property Arguments As New List(Of OpValue)
        Public Overridable Property [Return] As OpValue Implements IReturnBind.Return
        Public Overridable Property [Function] As RkFunction

        Public Overrides Function ToString() As String

            If Me.Return IsNot Nothing Then

                Return $"{Me.Return} = {Me.Function.Name}({String.Join(", ", Me.Arguments.Map(Function(x) x.ToString))})"
            Else
                Return $"{Me.Function.Name}({String.Join(", ", Me.Arguments.Map(Function(x) x.ToString))})"
            End If
        End Function
    End Class

End Namespace
