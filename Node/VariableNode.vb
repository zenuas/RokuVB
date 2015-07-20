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
        Public Overridable Property Type As InType Implements IEvaluableNode.Type

        Public Overridable ReadOnly Property Receiver() As InType Implements IEvaluableNode.Receiver
            Get
                Return Me.Type
            End Get
        End Property

    End Class

End Namespace
