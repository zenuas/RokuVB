Imports System.Text
Imports Roku.Manager
Imports Roku.Compiler


Namespace Node

    Public Class StringNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Sub New(s As Token)

            Me.String = s.Name
            Me.AppendLineNumber(s)
        End Sub

        Private string_ As New StringBuilder
        Public Overridable Property [String]() As String
            Get
                Return Me.string_.ToString
            End Get
            Set(ByVal value As String)

                Me.string_ = New StringBuilder(value)
            End Set
        End Property

        Public Overridable Sub Append(s As String)

            Me.string_.Append(s)
        End Sub

        Public Overridable Property Type As InType Implements IEvaluableNode.Type

        Public Overridable ReadOnly Property Receiver As InType Implements IEvaluableNode.Receiver
            Get
                Return Me.Type
            End Get
        End Property

    End Class

End Namespace
