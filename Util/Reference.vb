Namespace Util

    Public Class Reference(Of T)

        Protected Overridable Property Value_ As T

        Public Sub New()

        End Sub

        Public Sub New(v As T)

            Me.Replace(v)
        End Sub

        Public Overridable ReadOnly Property Value As T
            Get
                Return Me.Value_
            End Get
        End Property

        Public Overridable Sub Replace(v As T)

            Me.Value_ = v
        End Sub
    End Class

End Namespace

