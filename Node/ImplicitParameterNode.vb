Namespace Node

    Public Class ImplicitParameterNode
        Inherits VariableNode


        Public Sub New(s As String, index As UInteger)
            MyBase.New(s)

            Me.Index = index
        End Sub

        Public Overridable Property Index As UInteger
    End Class

End Namespace
