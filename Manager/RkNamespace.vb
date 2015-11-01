Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkNamespace
        Implements IEntry

        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IEntry)

    End Class

End Namespace
