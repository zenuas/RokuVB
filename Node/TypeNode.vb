Imports System.Collections.Generic
Imports Roku.Util


Namespace Node

    Public Class TypeNode
        Inherits TypeBaseNode
        Implements IEvaluableNode


        Public Sub New()

            Me.Name = ""
        End Sub

        Public Sub New(name As VariableNode)
            MyBase.New(name)

        End Sub

        Public Sub New(ns As TypeNode, name As VariableNode)
            Me.New(name)

            ns.IsNamespace = True
            Me.Namespace = ns
        End Sub

        Public Overridable Property [Namespace] As TypeNode = Nothing
        Public Overridable Property Arguments As New List(Of TypeBaseNode)
        Public Overridable Property IsNamespace As Boolean = False
        Public Overridable Property IsTypeClass As Boolean = False
        Public Overridable Property UseStatement As Boolean = False

        Public Overrides Function HasGeneric() As Boolean

            Return MyBase.HasGeneric OrElse Me.Arguments.Or(Function(x) x.HasGeneric)
        End Function

        Public Overrides Function ToString() As String

            If Me.Type IsNot Nothing Then Return Me.Type.ToString
            Return $"{If(Me.Namespace Is Nothing, "", $"{Me.Namespace}.")}{Me.Name}{If(Me.Arguments.Count > 0, $"<{String.Join(", ", Me.Arguments.Map(Function(x) x.ToString))}>", "")}"
        End Function
    End Class

End Namespace
