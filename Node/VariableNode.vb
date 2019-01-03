Imports Roku.Manager


Namespace Node

    Public Class VariableNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(s As String)

            Me.Name = s
            Me.UniqueName = s
        End Sub

        Public Overridable Property Name As String
        Public Overridable Property UniqueName As String
        Public Overridable Property Scope As IScopeNode
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance
        Public Overridable Property ClosureEnvironment As Boolean = False
        Public Overridable Property LocalVariable As Boolean = False

        Public Overrides Function ToString() As String

            Return Me.Name
        End Function
    End Class

End Namespace
