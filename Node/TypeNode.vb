Imports Roku.Manager


Namespace Node

    Public Class TypeNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New()

            Me.Name = ""
        End Sub

        Public Sub New(name As VariableNode)

            Me.Name = name.Name
            Me.AppendLineNumber(name)
        End Sub

        Public Overridable Property Name As String
        Public Overridable Property IsGeneric As Boolean = False
        Public Overridable Property Nullable As Boolean = False
        Public Overridable Property NullAdded As Boolean = False
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = False Implements IEvaluableNode.IsInstance

        Public Overridable Function HasGeneric() As Boolean

            Return Me.IsGeneric
        End Function

    End Class

End Namespace
