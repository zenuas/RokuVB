Imports Roku.Operator


Namespace IntermediateCode

    Public Class InIf
        Inherits InCode0

        Public Sub New()

            Me.Operator = InOperator.If
        End Sub

        Public Overridable Property Condition As OpValue
        'Public Overridable Property [Then] As InLabel
        Public Overridable Property [Else] As InLabel

        Public Overrides Function ToString() As String

            Return $"if {Me.Condition} else goto {Me.Else}"
        End Function
    End Class

End Namespace
