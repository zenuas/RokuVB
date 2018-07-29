Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class ClassNode
        Inherits BaseNode
        Implements IEvaluableNode, IAddFunction, IStatementNode


        Public Sub New(linenum As Integer)

            Me.LineNumber = linenum
            Me.LineColumn = 0
        End Sub

        Public Overridable Sub AddFunction(func As FunctionNode) Implements IAddFunction.AddFunction

            Me.Functions.Add(func)
        End Sub

        Public Overridable Property Name As String
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = False Implements IEvaluableNode.IsInstance
        Public Overridable ReadOnly Property Functions As New List(Of FunctionNode) Implements IAddFunction.Functions
        Public Overridable ReadOnly Property Generics As New List(Of TypeBaseNode)
    End Class

End Namespace
