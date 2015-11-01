Imports Roku.Manager


Namespace Node

    Public Class VariableNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(ByVal s As String)

            Me.Name = s
        End Sub

        Public Overridable Property Name As String
        Public Overridable Property Scope As IScopeNode
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property ClosureEnvironment As Boolean = False

    End Class

End Namespace
