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
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance
        Public Overridable Property ClosureEnvironment As Boolean = False

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} ""{Me.Name}"": {Me.Type?.ToString}"
        End Function
    End Class

End Namespace
