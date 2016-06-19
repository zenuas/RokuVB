Imports System.Collections.Generic
Imports Roku.Manager
Imports Roku.Operator


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

    End Class

End Namespace
