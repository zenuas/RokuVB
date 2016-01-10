Imports System


Namespace Architecture

    Public Class ArchitectureNameAttribute
        Inherits Attribute

        Public Overridable Property Name As String = ""

        Public Sub New(name As String)

            Me.Name = name
        End Sub
    End Class

End Namespace
