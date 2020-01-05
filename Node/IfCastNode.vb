Namespace Node

    Public Class IfCastNode
        Inherits IfNode

        Public Overridable Property [Declare] As TypeBaseNode
        Public Overridable Property Var As VariableNode

        Public Overrides Function ToString() As String

            Return $"if {Me.Var}: {Me.Declare} = {Me.Condition}"
        End Function
    End Class

End Namespace
