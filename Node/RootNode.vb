Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class RootNode
        Inherits BaseNode


        Public Overridable ReadOnly Property Namespaces As New Dictionary(Of String, ProgramNode)
    End Class

End Namespace
