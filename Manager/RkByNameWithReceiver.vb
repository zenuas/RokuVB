Imports Roku.Node


Namespace Manager

    Public Class RkByNameWithReceiver
        Inherits RkByName

        Public Overridable Property Receiver As IEvaluableNode

        Public Overrides Function ToString() As String

            Return $"{Me.Receiver}.{Me.Name}"
        End Function
    End Class

End Namespace
