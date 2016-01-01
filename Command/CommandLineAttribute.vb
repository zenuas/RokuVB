Imports System


Namespace Command

    Public Class CommandLineAttribute
        Inherits Attribute

        Public Sub New()

        End Sub

        Public Sub New(short_option As String)

            Me.ShortOption = short_option
        End Sub

        Public Sub New(short_option As String, long_option_ As String)

            Me.ShortOption = short_option
            Me.LongOption = long_option_
        End Sub

        Public Sub New(short_option As String, long_option_ As String, help_message As String)

            Me.ShortOption = short_option
            Me.LongOption = long_option_
            Me.HelpMessage = help_message
        End Sub

        Public Overridable Property ShortOption As String
        Public Overridable Property LongOption As String
        Public Overridable Property HelpMessage As String

    End Class

End Namespace
