Imports System
Imports Roku.Manager


Namespace Node

    Public Class NumericNode
        Inherits BaseNode
        Implements IEvaluableNode, IFeedback


        Public Sub New(ByVal n As UInt32)

            Me.Numeric = n
        End Sub

        Public Overridable Property Numeric As UInt32
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property IsInstance As Boolean = True Implements IEvaluableNode.IsInstance

        Public Overridable Function Feedback(t As IType) As Boolean Implements IFeedback.Feedback

            If TypeOf Me.Type Is RkSomeType Then

                Dim some = CType(Me.Type, RkSomeType)
                Return some.Merge(t)
            Else

                Me.Type = t
                Return True
            End If
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} ""{Me.Numeric}"": {Me.Type?.ToString}"
        End Function
    End Class

End Namespace
