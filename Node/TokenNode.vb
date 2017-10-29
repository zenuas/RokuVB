Imports Roku.Manager
Imports Roku.Parser


Namespace Node

    Public Class TokenNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(t As Token)

            Me.Token = t
            Me.AppendLineNumber(t)
        End Sub

        Public Overridable ReadOnly Property Token As Token
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

    End Class

End Namespace
